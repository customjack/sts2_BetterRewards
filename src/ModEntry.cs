using BetterRewards.Core;
using MegaCrit.Sts2.Core.Modding;

namespace BetterRewards;

[ModInitializer(nameof(OnModLoaded))]
public static class ModEntry
{
    public static void OnModLoaded()
    {
        ModBootstrap.Initialize();
    }
}
