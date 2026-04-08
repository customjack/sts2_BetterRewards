using BetterRewards.Features.Multiplayer;
using BetterRewards.Features.Settings;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace BetterRewards.Features.Event;

/// <summary>
/// Resolves the score from the most recent run whose player-id set exactly matches the current run.
/// In multiplayer, clients prefer the host-provided cached score and only fall back locally if it has
/// not arrived yet.
/// </summary>
internal static class PreviousRunScoreService
{
    public static int GetPreviousRunScore(IRunState? runState = null)
    {
        var score = GetRawScore(runState);
        return Math.Max(score, BetterRewardsSettings.MinPreviousScore);
    }

    public static string BuildPlayerSignature(IEnumerable<ulong> playerIds)
    {
        ArgumentNullException.ThrowIfNull(playerIds);
        return string.Join(",", playerIds.Distinct().OrderBy(id => id));
    }

    public static int ResolveLatestMatchingScoreForPlayers(IEnumerable<ulong> playerIds)
    {
        var signature = BuildPlayerSignature(playerIds);
        if (string.IsNullOrWhiteSpace(signature))
        {
            return 0;
        }

        var inMemoryHistory = RunManager.Instance?.History;
        if (inMemoryHistory != null && MatchesPlayerSignature(inMemoryHistory, signature))
        {
            return CalculateFromHistory(inMemoryHistory);
        }

        try
        {
            var names = SaveManager.Instance?.GetAllRunHistoryNames();
            if (names == null || names.Count == 0)
            {
                return 0;
            }

            foreach (var name in names
                         .Where(candidate => candidate.EndsWith(".run", StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(candidate => candidate, StringComparer.Ordinal))
            {
                var result = SaveManager.Instance!.LoadRunHistory(name);
                if (!result.Success || result.SaveData == null || !MatchesPlayerSignature(result.SaveData, signature))
                {
                    continue;
                }

                return CalculateFromHistory(result.SaveData);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterRewards] Could not load previous run score: {ex.Message}");
        }

        return 0;
    }

    private static int GetRawScore(IRunState? runState)
    {
        var resolvedRunState = runState ?? RunManager.Instance?.DebugOnlyGetState();
        if (resolvedRunState == null)
        {
            return ResolveLatestMatchingScoreForPlayers(Array.Empty<ulong>());
        }

        var signature = BuildPlayerSignature(resolvedRunState.Players.Select(player => player.NetId));
        if (resolvedRunState.Players.Count > 1)
        {
            if (BetterRewardsScoreSyncService.TryGetCachedScore(resolvedRunState, out var cachedScore))
            {
                return cachedScore;
            }

            var localScore = ResolveLatestMatchingScoreForPlayers(resolvedRunState.Players.Select(player => player.NetId));
            var netService = RunManager.Instance?.NetService;
            if (netService is { IsConnected: true, Type: NetGameType.Host })
            {
                BetterRewardsScoreSyncService.CacheAndBroadcast(netService, signature, localScore, "PreviousRunScoreService fallback");
            }

            return localScore;
        }

        return ResolveLatestMatchingScoreForPlayers(resolvedRunState.Players.Select(player => player.NetId));
    }

    private static bool MatchesPlayerSignature(RunHistory history, string signature)
    {
        if (history == null)
        {
            return false;
        }

        return string.Equals(
            BuildPlayerSignature(history.Players.Select(player => player.Id)),
            signature,
            StringComparison.Ordinal);
    }

    private static int CalculateFromHistory(RunHistory history)
    {
        var score = 0;
        var actCount = history.MapPointHistory.Count;
        for (var actIndex = 0; actIndex < actCount; actIndex++)
        {
            score += history.MapPointHistory[actIndex].Count * 10 * (actIndex + 1);
        }

        if (history.Win)
        {
            score += 300;
        }
        else if (actCount <= 2)
        {
            if (actCount > 1)
            {
                score += 100;
            }
        }
        else
        {
            score += 200;
        }

        return (int)(score * (1.0 + history.Ascension * 0.1));
    }
}
