using BetterRewards.Features.Settings;
using BetterRewards.Features.Shop;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Reflection;

namespace BetterRewards.Features.Event;

/// <summary>
/// Patches Neow.GenerateInitialOptions to append the BetterRewards sacrifice/shop option.
/// </summary>
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class NeowGenerateInitialOptionsPatch
{
    // Cached reflection accessor for EventModel.SetEventState (protected)
    private static readonly MethodInfo? _setEventState = typeof(MegaCrit.Sts2.Core.Models.EventModel)
        .GetMethod("SetEventState", BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPostfix]
    public static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        try
        {
            var owner = __instance.Owner;
            if (owner == null)
            {
                return;
            }

            BetterRewardsNeowShopTransitionState.ResetForNeowStart();

            if (__result.Any(option => option.TextKey == "BETTERREWARDS.NEOW"))
            {
                return;
            }

            var options = new List<EventOption>(__result)
            {
                BuildBargainOption(__instance)
            };
            __result = options;
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterRewards] Failed to inject Neow option: {ex.Message}");
        }
    }

    private static EventOption BuildBargainOption(Neow neow)
    {
        var previousScore    = PreviousRunScoreService.GetPreviousRunScore(neow.Owner?.RunState);
        var hPct             = BetterRewardsSettings.HpSacrificePercent;
        var n                = BetterRewardsSettings.SacrificeRepeatCount;
        var sPct             = BetterRewardsSettings.ScoreToGoldPercent;
        var effectiveHPct    = Math.Min(hPct, 1.0 / n);
        var goldPerSacrifice = (int)Math.Floor(previousScore * sPct);

        EnsureLocStrings(previousScore, effectiveHPct, n, goldPerSacrifice);

        return new EventOption(
            neow,
            () => OnBargainChosen(neow, effectiveHPct, n, goldPerSacrifice, sacrificesDone: 0),
            new LocString("events", "BETTERREWARDS.NEOW.title"),
            new LocString("events", "BETTERREWARDS.NEOW.description"),
            "BETTERREWARDS.NEOW",
            Array.Empty<IHoverTip>());
    }

    private static void EnsureLocStrings(int previousScore, double hPct, int n, int goldPerSacrifice)
    {
        var locTable = LocManager.Instance.GetTable("events");
        locTable.MergeWith(new Dictionary<string, string>
        {
            // NEOW option — uses LocString constructor directly so no .title/.description needed here,
            // but also register for safety.
            ["BETTERREWARDS.NEOW.title"]                   = "The Merchant's Bargain",
            ["BETTERREWARDS.NEOW.description"]             = previousScore > 0
                ? $"Visit a special shop. Sacrifice HP for gold (prev score: {previousScore}). Up to {n} sacrifices, {(int)(hPct * 100)}% HP each for {goldPerSacrifice}g."
                : "Visit a special shop. No previous run — gold rewards will be 0.",
            // EventOption string-key ctor appends .title / .description
            ["BETTERREWARDS.ENTER_SHOP.title"]             = "Enter the shop",
            ["BETTERREWARDS.ENTER_SHOP.description"]       = "Leave Neow and enter the merchant's special shop.",
            ["BETTERREWARDS.SACRIFICE.locked.title"]       = $"Cannot Sacrifice {(int)(hPct * 100)}% HP",
            ["BETTERREWARDS.SACRIFICE.locked.description"] = "You would die from this sacrifice.",
            ["BETTERREWARDS.SHOP_PENDING.title"]           = "Waiting for the merchant",
            ["BETTERREWARDS.SHOP_PENDING.desc"]            = "The merchant waits. You will enter the special shop once everyone is finished with Neow.",
        });
    }

    private static Task OnBargainChosen(Neow neow, double hPct, int maxRepeats, int goldPerSacrifice, int sacrificesDone)
    {
        var owner = neow.Owner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        BetterRewardsShopState.Reset();
        ShowSacrificeState(neow, owner, hPct, maxRepeats, goldPerSacrifice, sacrificesDone);
        return Task.CompletedTask;
    }

    private static void ShowSacrificeState(Neow neow, MegaCrit.Sts2.Core.Entities.Players.Player owner, double hPct, int maxRepeats, int goldPerSacrifice, int sacrificesDone)
    {
        var hpCost   = (int)Math.Floor(owner.Creature.MaxHp * hPct);
        var wouldDie = hpCost >= owner.Creature.CurrentHp;
        var canMore  = sacrificesDone < maxRepeats;

        var locTable = LocManager.Instance.GetTable("events");

        // Inject all display strings directly into the loc table so they resolve at display time.
        locTable.MergeWith(new Dictionary<string, string>
        {
            ["BETTERREWARDS.SACRIFICE.state.desc"]         = $"The merchant offers a bargain. Sacrifice HP in exchange for gold.\n\nSacrifices: {sacrificesDone}/{maxRepeats}  |  HP: {owner.Creature.CurrentHp}/{owner.Creature.MaxHp}  |  Gold per sacrifice: {goldPerSacrifice}",
            ["BETTERREWARDS.SACRIFICE.do.title"]           = $"Sacrifice {hpCost} HP for {goldPerSacrifice} gold",
            ["BETTERREWARDS.SACRIFICE.do.desc"]            = $"Lose {hpCost} HP and gain {goldPerSacrifice} gold. ({sacrificesDone + 1}/{maxRepeats})",
            ["BETTERREWARDS.SACRIFICE.locked.title"]       = $"Cannot Sacrifice {(int)(hPct * 100)}% HP",
            ["BETTERREWARDS.SACRIFICE.locked.desc"]        = "You would die from this sacrifice.",
            ["BETTERREWARDS.ENTER_SHOP.title"]             = "Enter the shop",
            ["BETTERREWARDS.ENTER_SHOP.desc"]              = "Leave Neow and enter the merchant's special shop.",
        });

        // Use the LocString-based EventOption constructor directly to avoid the string-key
        // overload's call to GetOptionTitle/GetOptionDescription, which returns null for keys
        // not in the original event data file and then crashes in AddDetailsTo(null).
        var options = new List<EventOption>();

        if (canMore && !wouldDie)
        {
            options.Add(new EventOption(
                neow,
                async () =>
                {
                    var cost = (int)Math.Floor(owner.Creature.MaxHp * hPct);
                    await CreatureCmd.SetCurrentHp(owner.Creature, owner.Creature.CurrentHp - cost);
                    await PlayerCmd.GainGold(goldPerSacrifice, owner);
                    ShowSacrificeState(neow, owner, hPct, maxRepeats, goldPerSacrifice, sacrificesDone + 1);
                },
                new LocString("events", "BETTERREWARDS.SACRIFICE.do.title"),
                new LocString("events", "BETTERREWARDS.SACRIFICE.do.desc"),
                "BETTERREWARDS.SACRIFICE.do",
                Array.Empty<IHoverTip>()));
        }
        else if (canMore)
        {
            // Grayed out — null OnChosen makes IsLocked = true
            options.Add(new EventOption(
                neow,
                null,
                new LocString("events", "BETTERREWARDS.SACRIFICE.locked.title"),
                new LocString("events", "BETTERREWARDS.SACRIFICE.locked.desc"),
                "BETTERREWARDS.SACRIFICE.locked",
                Array.Empty<IHoverTip>()));
        }

        options.Add(new EventOption(
            neow,
            async () =>
            {
                BetterRewardsNeowShopTransitionState.MarkMerchantPending(owner.NetId);
                ShowMerchantPendingState(neow);
                await TryEnterPendingMerchantRoom("Enter shop option");
            },
            new LocString("events", "BETTERREWARDS.ENTER_SHOP.title"),
            new LocString("events", "BETTERREWARDS.ENTER_SHOP.desc"),
            "BETTERREWARDS.ENTER_SHOP",
            Array.Empty<IHoverTip>()));

        CallSetEventState(neow, new LocString("events", "BETTERREWARDS.SACRIFICE.state.desc"), options);
    }

    private static void ShowMerchantPendingState(Neow neow)
    {
        NMapScreen.Instance?.SetTravelEnabled(enabled: false);

        CallSetEventState(
            neow,
            new LocString("events", "BETTERREWARDS.SHOP_PENDING.desc"),
            new[]
            {
                new EventOption(
                    neow,
                    null,
                    new LocString("events", "BETTERREWARDS.SHOP_PENDING.title"),
                    new LocString("events", "BETTERREWARDS.SHOP_PENDING.desc"),
                    "BETTERREWARDS.SHOP_PENDING",
                    Array.Empty<IHoverTip>())
            });
    }

    private static void CallSetEventState(Neow neow, LocString description, IEnumerable<EventOption> options)
    {
        if (_setEventState == null)
        {
            Log.Error("[BetterRewards] Could not find EventModel.SetEventState via reflection.");
            return;
        }

        _setEventState.Invoke(neow, new object[] { description, options });
    }

    internal static async Task TryEnterPendingMerchantRoom(string source)
    {
        var runManager = RunManager.Instance;
        var runState = runManager.DebugOnlyGetState();
        if (runState?.CurrentRoom is not EventRoom { CanonicalEvent: Neow })
        {
            return;
        }

        var synchronizer = runManager.EventSynchronizer;
        if (synchronizer == null || synchronizer.Events.Count == 0 || synchronizer.Events.Any(@event => !BetterRewardsNeowShopTransitionState.IsEventReadyForMerchantTransition(@event)))
        {
            return;
        }

        if (!BetterRewardsNeowShopTransitionState.TryBeginMerchantTransition())
        {
            return;
        }

        try
        {
            BetterRewardsShopButtonsFeature.IsActiveShop = true;
            Log.Info($"[BetterRewards] Entering special merchant room after Neow from '{source}'.");
            await runManager.EnterRoom(new MerchantRoom());
        }
        catch (Exception ex)
        {
            BetterRewardsShopButtonsFeature.IsActiveShop = false;
            BetterRewardsNeowShopTransitionState.CancelMerchantTransition();
            Log.Warn($"[BetterRewards] Failed to enter special merchant room: {ex.Message}");
        }
    }
}

internal static class BetterRewardsNeowShopTransitionState
{
    private static readonly HashSet<ulong> PendingMerchantPlayers = [];
    private static bool _merchantTransitionStarted;

    public static void ResetForNeowStart()
    {
        PendingMerchantPlayers.Clear();
        _merchantTransitionStarted = false;
        BetterRewardsShopButtonsFeature.IsActiveShop = false;
    }

    public static void MarkMerchantPending(ulong playerId)
    {
        if (playerId == 0)
        {
            return;
        }

        PendingMerchantPlayers.Add(playerId);
        NMapScreen.Instance?.SetTravelEnabled(enabled: false);
        Log.Info($"[BetterRewards] Special merchant room queued after Neow for player {playerId}.");
    }

    public static bool TryBeginMerchantTransition()
    {
        if (PendingMerchantPlayers.Count == 0 || _merchantTransitionStarted)
        {
            return false;
        }

        _merchantTransitionStarted = true;
        NMapScreen.Instance?.SetTravelEnabled(enabled: false);
        return true;
    }

    public static void CancelMerchantTransition()
    {
        _merchantTransitionStarted = false;
    }

    public static void ClearAfterMerchantExit()
    {
        PendingMerchantPlayers.Clear();
        _merchantTransitionStarted = false;
        BetterRewardsShopButtonsFeature.IsActiveShop = false;
    }

    public static bool ShouldBlockProceed()
    {
        return PendingMerchantPlayers.Count > 0
            && RunManager.Instance?.DebugOnlyGetState()?.CurrentRoom is EventRoom { CanonicalEvent: Neow };
    }

    public static bool IsEventReadyForMerchantTransition(MegaCrit.Sts2.Core.Models.EventModel eventModel)
    {
        if (eventModel.IsFinished)
        {
            return true;
        }

        return eventModel.Owner != null && PendingMerchantPlayers.Contains(eventModel.Owner.NetId);
    }
}

/// <summary>
/// Patches NMerchantInventory._Ready to inject the BetterRewards special buttons.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory._Ready))]
internal static class NMerchantInventoryReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance)
    {
        try
        {
            BetterRewardsShopButtonsFeature.TryAttachButtons(__instance);
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterRewards] Failed to attach shop buttons: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(EventRoom), "OnEventStateChanged")]
internal static class EventRoomNeowMerchantTransitionPatch
{
    [HarmonyPostfix]
    public static void Postfix(EventRoom __instance)
    {
        if (__instance.CanonicalEvent is not Neow)
        {
            return;
        }

        TaskHelper.RunSafely(NeowGenerateInitialOptionsPatch.TryEnterPendingMerchantRoom("EventRoom.OnEventStateChanged"));
    }
}

[HarmonyPatch(typeof(NEventRoom), nameof(NEventRoom.OptionButtonClicked))]
internal static class BetterRewardsBlockNeowProceedWhileMerchantPendingPatch
{
    [HarmonyPrefix]
    public static bool Prefix(EventOption option)
    {
        if (!option.IsProceed || !BetterRewardsNeowShopTransitionState.ShouldBlockProceed())
        {
            return true;
        }

        NMapScreen.Instance?.SetTravelEnabled(enabled: false);
        return false;
    }
}

[HarmonyPatch(typeof(MerchantRoom), nameof(MerchantRoom.Exit))]
internal static class BetterRewardsMerchantRoomExitPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BetterRewardsNeowShopTransitionState.ClearAfterMerchantExit();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer))]
internal static class BetterRewardsResetSinglePlayerRunPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BetterRewardsNeowShopTransitionState.ResetForNeowStart();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer))]
internal static class BetterRewardsResetSavedSinglePlayerRunPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BetterRewardsNeowShopTransitionState.ResetForNeowStart();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer))]
internal static class BetterRewardsResetNewMultiplayerRunPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BetterRewardsNeowShopTransitionState.ResetForNeowStart();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer))]
internal static class BetterRewardsResetSavedMultiplayerRunPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BetterRewardsNeowShopTransitionState.ResetForNeowStart();
    }
}
