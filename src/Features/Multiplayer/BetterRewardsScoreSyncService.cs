using BetterRewards.Features.Event;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace BetterRewards.Features.Multiplayer;

internal struct BetterRewardsPreviousRunScoreMessage : INetMessage, IPacketSerializable
{
    public string PlayerSignature;
    public int Score;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Info;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(PlayerSignature ?? string.Empty);
        writer.WriteInt(Score);
    }

    public void Deserialize(PacketReader reader)
    {
        PlayerSignature = reader.ReadString();
        Score = reader.ReadInt();
    }
}

internal static class BetterRewardsScoreSyncService
{
    private static readonly object Sync = new();
    private static readonly Dictionary<INetGameService, MessageHandlerDelegate<BetterRewardsPreviousRunScoreMessage>> ScoreHandlers = [];
    private static readonly Dictionary<INetGameService, Action<NetErrorInfo>> DisconnectHandlers = [];
    private static string? _cachedPlayerSignature;
    private static int _cachedScore;

    public static void EnsureAttached(INetGameService netService, string source)
    {
        lock (Sync)
        {
            if (ScoreHandlers.ContainsKey(netService))
            {
                return;
            }

            MessageHandlerDelegate<BetterRewardsPreviousRunScoreMessage> scoreHandler = (message, senderId) =>
                HandleScoreMessage(netService, message, senderId);
            Action<NetErrorInfo> disconnectHandler = _ => OnDisconnected(netService);

            ScoreHandlers[netService] = scoreHandler;
            DisconnectHandlers[netService] = disconnectHandler;

            netService.RegisterMessageHandler(scoreHandler);
            netService.Disconnected += disconnectHandler;
        }
    }

    public static void RefreshForCurrentRun(IRunState? runState, string source)
    {
        if (runState == null)
        {
            return;
        }

        RefreshForPlayerIds(runState.Players.Select(player => player.NetId), source);
    }

    public static void RefreshForPlayerIds(IEnumerable<ulong> playerIds, string source)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        var playerIdList = playerIds.Distinct().OrderBy(id => id).ToArray();
        if (playerIdList.Length <= 1)
        {
            return;
        }

        var netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected || netService.Type == NetGameType.Singleplayer)
        {
            return;
        }

        EnsureAttached(netService, source);
        if (netService.Type != NetGameType.Host)
        {
            return;
        }

        var signature = PreviousRunScoreService.BuildPlayerSignature(playerIdList);
        var score = PreviousRunScoreService.ResolveLatestMatchingScoreForPlayers(playerIdList);
        CacheAndBroadcast(netService, signature, score, source);
    }

    public static bool TryGetCachedScore(IRunState runState, out int score)
    {
        ArgumentNullException.ThrowIfNull(runState);

        var expectedSignature = PreviousRunScoreService.BuildPlayerSignature(runState.Players.Select(player => player.NetId));
        lock (Sync)
        {
            if (string.Equals(_cachedPlayerSignature, expectedSignature, StringComparison.Ordinal))
            {
                score = _cachedScore;
                return true;
            }
        }

        score = 0;
        return false;
    }

    public static void CacheAndBroadcast(INetGameService netService, string playerSignature, int score, string source)
    {
        if (string.IsNullOrWhiteSpace(playerSignature))
        {
            return;
        }

        lock (Sync)
        {
            _cachedPlayerSignature = playerSignature;
            _cachedScore = score;
        }

        if (netService.Type != NetGameType.Host || !netService.IsConnected)
        {
            return;
        }

        EnsureAttached(netService, source);
        netService.SendMessage(new BetterRewardsPreviousRunScoreMessage
        {
            PlayerSignature = playerSignature,
            Score = score
        });
    }

    public static void SendSnapshotTo(INetGameService netService, ulong targetPlayerId, IEnumerable<ulong> playerIds, string source)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        if (netService.Type != NetGameType.Host || !netService.IsConnected)
        {
            return;
        }

        var playerIdList = playerIds.Distinct().OrderBy(id => id).ToArray();
        if (playerIdList.Length <= 1)
        {
            return;
        }

        var playerSignature = PreviousRunScoreService.BuildPlayerSignature(playerIdList);
        if (string.IsNullOrWhiteSpace(playerSignature))
        {
            return;
        }

        var score = PreviousRunScoreService.ResolveLatestMatchingScoreForPlayers(playerIdList);
        lock (Sync)
        {
            _cachedPlayerSignature = playerSignature;
            _cachedScore = score;
        }

        EnsureAttached(netService, source);
        netService.SendMessage(new BetterRewardsPreviousRunScoreMessage
        {
            PlayerSignature = playerSignature,
            Score = score
        }, targetPlayerId);
    }

    private static void HandleScoreMessage(
        INetGameService netService,
        BetterRewardsPreviousRunScoreMessage message,
        ulong senderId)
    {
        if (senderId == netService.NetId || string.IsNullOrWhiteSpace(message.PlayerSignature))
        {
            return;
        }

        lock (Sync)
        {
            _cachedPlayerSignature = message.PlayerSignature;
            _cachedScore = message.Score;
        }
    }

    private static void OnDisconnected(INetGameService netService)
    {
        lock (Sync)
        {
            if (ScoreHandlers.TryGetValue(netService, out var scoreHandler))
            {
                netService.UnregisterMessageHandler(scoreHandler);
                ScoreHandlers.Remove(netService);
            }

            if (DisconnectHandlers.TryGetValue(netService, out var disconnectHandler))
            {
                netService.Disconnected -= disconnectHandler;
                DisconnectHandlers.Remove(netService);
            }

            _cachedPlayerSignature = null;
            _cachedScore = 0;
        }
    }
}

[HarmonyPatch(typeof(NetClientGameService))]
internal static class NetClientGameServiceAttachBetterRewardsScorePatch
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    public static void Postfix(NetClientGameService __instance)
    {
        BetterRewardsScoreSyncService.EnsureAttached(__instance, "NetClientGameService::.ctor");
    }
}

[HarmonyPatch(typeof(NetHostGameService))]
internal static class NetHostGameServiceAttachBetterRewardsScorePatch
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    public static void Postfix(NetHostGameService __instance)
    {
        BetterRewardsScoreSyncService.EnsureAttached(__instance, "NetHostGameService::.ctor");
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer))]
internal static class RunManagerSetUpNewMultiPlayerBetterRewardsPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState state)
    {
        BetterRewardsScoreSyncService.RefreshForCurrentRun(state, "RunManager.SetUpNewMultiPlayer");
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer))]
internal static class RunManagerSetUpSavedMultiPlayerBetterRewardsPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState state)
    {
        BetterRewardsScoreSyncService.RefreshForCurrentRun(state, "RunManager.SetUpSavedMultiPlayer");
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandleClientLobbyJoinRequestMessage")]
internal static class StartRunLobbyJoinBetterRewardsScoreSnapshotPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance, ulong senderId)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.Players.Exists(player => player.id == senderId))
        {
            return;
        }

        BetterRewardsScoreSyncService.SendSnapshotTo(
            __instance.NetService,
            senderId,
            __instance.Players.Select(player => player.id),
            "StartRunLobby join");
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandlePlayerReadyMessage")]
internal static class StartRunLobbyReadyBetterRewardsScoreSnapshotPatch
{
    [HarmonyPostfix]
    public static void Postfix(StartRunLobby __instance, ulong senderId)
    {
        if (__instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        if (!__instance.Players.Exists(player => player.id == senderId))
        {
            return;
        }

        BetterRewardsScoreSyncService.SendSnapshotTo(
            __instance.NetService,
            senderId,
            __instance.Players.Select(player => player.id),
            "StartRunLobby ready");
    }
}
