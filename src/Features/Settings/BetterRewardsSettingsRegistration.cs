using MegaCrit.Sts2.Core.Logging;
using ModManagerSettings.Api;

namespace BetterRewards.Features.Settings;

internal static class BetterRewardsSettingsRegistration
{
    private const string ModKey = "BetterRewards";

    public static void Register()
    {
        BetterRewardsSettings.ResetToDefaults();

        ModSettingsRegistry.UpsertRegistration(new ModSettingsRegistration
        {
            ModPckName = ModKey,
            DisplayName = "BetterRewards",
            Description = "Post-Neow HP-sacrifice event and special shop.",
            ShowSettingsButtonInModdingMenu = true,
            OnRestoreDefaults = BetterRewardsSettings.ResetToDefaults,

            NumberSettings = new[]
            {
                // Event settings
                new ModSettingNumberDefinition
                {
                    Key = "hp_sacrifice_percent",
                    Label = "HP Sacrifice %",
                    Description = "Fraction of your current HP lost per sacrifice (0.0–1.0). Capped so a single choice cannot exceed 1/N of your max HP.",
                    Path = "Settings/Event",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 0.20,
                    MinValue = 0.0,
                    MaxValue = 1.0,
                    Step = 0.01,
                    GetCurrentValue = () => BetterRewardsSettings.HpSacrificePercent,
                    OnApply = BetterRewardsSettings.SetHpSacrificePercent
                },
                new ModSettingNumberDefinition
                {
                    Key = "sacrifice_repeat_count",
                    Label = "Sacrifice Repeats",
                    Description = "How many times you may sacrifice HP for gold.",
                    Path = "Settings/Event",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 4,
                    MinValue = 1,
                    MaxValue = 999,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.SacrificeRepeatCount,
                    OnApply = BetterRewardsSettings.SetSacrificeRepeatCount
                },
                new ModSettingNumberDefinition
                {
                    Key = "score_to_gold_percent",
                    Label = "Score → Gold %",
                    Description = "Fraction of your previous run's score converted to gold per sacrifice (e.g. 0.25 = 25% of score per sacrifice).",
                    Path = "Settings/Event",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 0.25,
                    MinValue = 0.0,
                    MaxValue = 100.0,
                    Step = 0.05,
                    GetCurrentValue = () => BetterRewardsSettings.ScoreToGoldPercent,
                    OnApply = BetterRewardsSettings.SetScoreToGoldPercent
                },
                new ModSettingNumberDefinition
                {
                    Key = "min_previous_score",
                    Label = "Minimum Previous Score",
                    Description = "The effective previous-run score is at least this value, guaranteeing a minimum gold opportunity even on first run.",
                    Path = "Settings/Event",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 1000,
                    MinValue = 0,
                    MaxValue = 100000,
                    Step = 50,
                    GetCurrentValue = () => BetterRewardsSettings.MinPreviousScore,
                    OnApply = BetterRewardsSettings.SetMinPreviousScore
                },

                // Shop: reroll
                new ModSettingNumberDefinition
                {
                    Key = "shop_reroll_base_cost",
                    Label = "Reroll Base Cost",
                    Description = "Gold cost for the first shop reroll.",
                    Path = "Settings/Shop/Reroll",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 150,
                    MinValue = 0,
                    MaxValue = 9999,
                    Step = 5,
                    GetCurrentValue = () => BetterRewardsSettings.ShopRerollBaseCost,
                    OnApply = BetterRewardsSettings.SetShopRerollBaseCost
                },
                new ModSettingNumberDefinition
                {
                    Key = "shop_reroll_cost_scaling",
                    Label = "Reroll Cost Scaling",
                    Description = "Multiplier applied to reroll cost after each use (e.g. 1.10 = +10% each time).",
                    Path = "Settings/Shop/Reroll",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 1.10,
                    MinValue = 1.0,
                    MaxValue = 5.0,
                    Step = 0.05,
                    GetCurrentValue = () => BetterRewardsSettings.ShopRerollCostScaling,
                    OnApply = BetterRewardsSettings.SetShopRerollCostScaling
                },

                // Shop: random relic
                new ModSettingNumberDefinition
                {
                    Key = "relic_base_cost",
                    Label = "Random Relic Base Cost",
                    Description = "Gold cost for the first random relic purchase.",
                    Path = "Settings/Shop/RandomRelic",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 300,
                    MinValue = 0,
                    MaxValue = 9999,
                    Step = 5,
                    GetCurrentValue = () => BetterRewardsSettings.RelicBaseCost,
                    OnApply = BetterRewardsSettings.SetRelicBaseCost
                },
                new ModSettingNumberDefinition
                {
                    Key = "relic_cost_scaling",
                    Label = "Random Relic Cost Scaling",
                    Description = "Multiplier applied to relic cost after each purchase.",
                    Path = "Settings/Shop/RandomRelic",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 1.10,
                    MinValue = 1.0,
                    MaxValue = 5.0,
                    Step = 0.05,
                    GetCurrentValue = () => BetterRewardsSettings.RelicCostScaling,
                    OnApply = BetterRewardsSettings.SetRelicCostScaling
                },
                new ModSettingNumberDefinition
                {
                    Key = "relic_weight_common",
                    Label = "Relic Weight: Common",
                    Description = "Relative weight for rolling a Common relic.",
                    Path = "Settings/Shop/RandomRelic/Weights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 10,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.RelicWeightCommon,
                    OnApply = BetterRewardsSettings.SetRelicWeightCommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "relic_weight_uncommon",
                    Label = "Relic Weight: Uncommon",
                    Description = "Relative weight for rolling an Uncommon relic.",
                    Path = "Settings/Shop/RandomRelic/Weights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 20,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.RelicWeightUncommon,
                    OnApply = BetterRewardsSettings.SetRelicWeightUncommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "relic_weight_rare",
                    Label = "Relic Weight: Rare",
                    Description = "Relative weight for rolling a Rare relic.",
                    Path = "Settings/Shop/RandomRelic/Weights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 35,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.RelicWeightRare,
                    OnApply = BetterRewardsSettings.SetRelicWeightRare
                },
                new ModSettingNumberDefinition
                {
                    Key = "relic_weight_shop",
                    Label = "Relic Weight: Shop",
                    Description = "Relative weight for rolling a Shop relic.",
                    Path = "Settings/Shop/RandomRelic/Weights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 10,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.RelicWeightShop,
                    OnApply = BetterRewardsSettings.SetRelicWeightShop
                },
                new ModSettingNumberDefinition
                {
                    Key = "relic_weight_ancient",
                    Label = "Relic Weight: Ancient",
                    Description = "Relative weight for rolling an Ancient relic.",
                    Path = "Settings/Shop/RandomRelic/Weights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 25,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.RelicWeightAncient,
                    OnApply = BetterRewardsSettings.SetRelicWeightAncient
                },

                // Shop: random item
                new ModSettingNumberDefinition
                {
                    Key = "random_item_base_cost",
                    Label = "Random Item Base Cost",
                    Description = "Gold cost for the first random item purchase.",
                    Path = "Settings/Shop/RandomItem",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 75,
                    MinValue = 0,
                    MaxValue = 9999,
                    Step = 5,
                    GetCurrentValue = () => BetterRewardsSettings.RandomItemBaseCost,
                    OnApply = BetterRewardsSettings.SetRandomItemBaseCost
                },
                new ModSettingNumberDefinition
                {
                    Key = "random_item_cost_scaling",
                    Label = "Random Item Cost Scaling",
                    Description = "Multiplier applied to random item cost after each purchase.",
                    Path = "Settings/Shop/RandomItem",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 1.10,
                    MinValue = 1.0,
                    MaxValue = 5.0,
                    Step = 0.05,
                    GetCurrentValue = () => BetterRewardsSettings.RandomItemCostScaling,
                    OnApply = BetterRewardsSettings.SetRandomItemCostScaling
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_weight_relic",
                    Label = "Item Type Weight: Relic",
                    Description = "Relative weight for random item to be a relic.",
                    Path = "Settings/Shop/RandomItem/TypeWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 15,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemWeightRelic,
                    OnApply = BetterRewardsSettings.SetItemWeightRelic
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_weight_card",
                    Label = "Item Type Weight: Card",
                    Description = "Relative weight for random item to be a card.",
                    Path = "Settings/Shop/RandomItem/TypeWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 50,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemWeightCard,
                    OnApply = BetterRewardsSettings.SetItemWeightCard
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_weight_potion",
                    Label = "Item Type Weight: Potion",
                    Description = "Relative weight for random item to be a potion.",
                    Path = "Settings/Shop/RandomItem/TypeWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 35,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemWeightPotion,
                    OnApply = BetterRewardsSettings.SetItemWeightPotion
                },
                new ModSettingNumberDefinition
                {
                    Key = "card_weight_common",
                    Label = "Card Weight: Common (character pool)",
                    Description = "Relative weight for a character-specific Common card.",
                    Path = "Settings/Shop/RandomItem/CardWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 55,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.CardWeightCommon,
                    OnApply = BetterRewardsSettings.SetCardWeightCommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "card_weight_uncommon",
                    Label = "Card Weight: Uncommon (character pool)",
                    Description = "Relative weight for a character-specific Uncommon card.",
                    Path = "Settings/Shop/RandomItem/CardWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 25,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.CardWeightUncommon,
                    OnApply = BetterRewardsSettings.SetCardWeightUncommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "card_weight_rare",
                    Label = "Card Weight: Rare (character pool)",
                    Description = "Relative weight for a character-specific Rare card.",
                    Path = "Settings/Shop/RandomItem/CardWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 10,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.CardWeightRare,
                    OnApply = BetterRewardsSettings.SetCardWeightRare
                },
                new ModSettingNumberDefinition
                {
                    Key = "card_weight_colorless",
                    Label = "Card Weight: Colorless",
                    Description = "Relative weight for a random Colorless card.",
                    Path = "Settings/Shop/RandomItem/CardWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 5,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.CardWeightColorless,
                    OnApply = BetterRewardsSettings.SetCardWeightColorless
                },
                new ModSettingNumberDefinition
                {
                    Key = "card_weight_curse",
                    Label = "Card Weight: Curse",
                    Description = "Relative weight for a random Curse card.",
                    Path = "Settings/Shop/RandomItem/CardWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 5,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.CardWeightCurse,
                    OnApply = BetterRewardsSettings.SetCardWeightCurse
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_relic_weight_common",
                    Label = "Random Item Relic Weight: Common",
                    Description = "Relative weight for the random-item relic to be Common.",
                    Path = "Settings/Shop/RandomItem/RelicWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 50,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemRelicWeightCommon,
                    OnApply = BetterRewardsSettings.SetItemRelicWeightCommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_relic_weight_uncommon",
                    Label = "Random Item Relic Weight: Uncommon",
                    Path = "Settings/Shop/RandomItem/RelicWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 33,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemRelicWeightUncommon,
                    OnApply = BetterRewardsSettings.SetItemRelicWeightUncommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_relic_weight_rare",
                    Label = "Random Item Relic Weight: Rare",
                    Path = "Settings/Shop/RandomItem/RelicWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 17,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemRelicWeightRare,
                    OnApply = BetterRewardsSettings.SetItemRelicWeightRare
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_relic_weight_shop",
                    Label = "Random Item Relic Weight: Shop",
                    Path = "Settings/Shop/RandomItem/RelicWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 5,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemRelicWeightShop,
                    OnApply = BetterRewardsSettings.SetItemRelicWeightShop
                },
                new ModSettingNumberDefinition
                {
                    Key = "item_relic_weight_ancient",
                    Label = "Random Item Relic Weight: Ancient",
                    Path = "Settings/Shop/RandomItem/RelicWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 2,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.ItemRelicWeightAncient,
                    OnApply = BetterRewardsSettings.SetItemRelicWeightAncient
                },
                new ModSettingNumberDefinition
                {
                    Key = "potion_weight_common",
                    Label = "Potion Rarity Weight: Common",
                    Path = "Settings/Shop/RandomItem/PotionWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 65,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.PotionWeightCommon,
                    OnApply = BetterRewardsSettings.SetPotionWeightCommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "potion_weight_uncommon",
                    Label = "Potion Rarity Weight: Uncommon",
                    Path = "Settings/Shop/RandomItem/PotionWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 25,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.PotionWeightUncommon,
                    OnApply = BetterRewardsSettings.SetPotionWeightUncommon
                },
                new ModSettingNumberDefinition
                {
                    Key = "potion_weight_rare",
                    Label = "Potion Rarity Weight: Rare",
                    Path = "Settings/Shop/RandomItem/PotionWeights",
                    AllowMultiplayerOverwrite = true,
                    DefaultValue = 10,
                    MinValue = 0,
                    MaxValue = 1000,
                    Step = 1,
                    GetCurrentValue = () => BetterRewardsSettings.PotionWeightRare,
                    OnApply = BetterRewardsSettings.SetPotionWeightRare
                }
            }
        });

        TryHydrateFromPersistedValues();
    }

    public static void PersistIfReady()
    {
        if (ModSettingsRegistry.IsPersistenceReady())
        {
            ModSettingsRegistry.PersistCurrentRegistrationValues(ModKey);
        }
    }

    private static bool _hydratedFromPersistedValues;

    public static void TryHydrateFromPersistedValues()
    {
        if (_hydratedFromPersistedValues)
        {
            Log.Info("[BetterRewards] TryHydrateFromPersistedValues: already hydrated, skipping.");
            return;
        }

        var ready = ModSettingsRegistry.IsPersistenceReady();
        Log.Info($"[BetterRewards] TryHydrateFromPersistedValues: IsPersistenceReady={ready}");

        if (!ready)
        {
            return;
        }

        var persisted = ModSettingsRegistry.GetPersistedSettingValues(ModKey);
        Log.Info($"[BetterRewards] TryHydrateFromPersistedValues: persisted count={persisted.Count}");
        foreach (var pair in persisted)
        {
            Log.Info($"[BetterRewards]   persisted key={pair.Key} value={pair.Value}");
        }

        ModSettingsRegistry.RestorePersistedValues(ModKey);
        Log.Info($"[BetterRewards] TryHydrateFromPersistedValues: RestorePersistedValues done. min_previous_score is now {BetterRewardsSettings.MinPreviousScore}");
        _hydratedFromPersistedValues = true;
    }
}
