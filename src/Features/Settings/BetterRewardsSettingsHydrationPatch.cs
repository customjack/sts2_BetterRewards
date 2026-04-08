using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace BetterRewards.Features.Settings;

/// <summary>
/// Late-hydrates BetterRewards settings from persisted values when the main menu becomes ready.
/// This is necessary because IsPersistenceReady() returns false during mod initialization,
/// so the first TryHydrateFromPersistedValues() call in Register() is a no-op.
/// </summary>
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class BetterRewardsSettingsHydrationPatch
{
    public static void Postfix()
    {
        Log.Info("[BetterRewards] BetterRewardsSettingsHydrationPatch: NMainMenu._Ready fired.");
        try
        {
            BetterRewardsSettingsRegistration.TryHydrateFromPersistedValues();
        }
        catch (Exception ex)
        {
            Log.Error($"[BetterRewards] Failed late settings hydration on NMainMenu._Ready. {ex}");
        }
    }
}
