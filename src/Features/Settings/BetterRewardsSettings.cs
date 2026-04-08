namespace BetterRewards.Features.Settings;

/// <summary>
/// Runtime-accessible settings values for BetterRewards.
/// All values are set by the registration/hydration layer.
/// </summary>
internal static class BetterRewardsSettings
{
    // --- HP sacrifice event ---

    /// <summary>Fraction of current HP lost per sacrifice choice (0.0–1.0).</summary>
    public static double HpSacrificePercent { get; private set; } = 0.20;

    /// <summary>How many times the sacrifice option may be chosen.</summary>
    public static int SacrificeRepeatCount { get; private set; } = 4;

    /// <summary>Fraction of previous run score converted to gold (0.0–100.0+).</summary>
    public static double ScoreToGoldPercent { get; private set; } = 0.25;

    /// <summary>Minimum effective previous-run score (guarantees at least this much gold opportunity).</summary>
    public static int MinPreviousScore { get; private set; } = 500;

    // --- Shop: reroll ---

    public static double ShopRerollBaseCost { get; private set; } = 150.0;
    public static double ShopRerollCostScaling { get; private set; } = 1.10;

    // --- Shop: random relic ---

    public static double RelicBaseCost { get; private set; } = 250.0;
    public static double RelicCostScaling { get; private set; } = 1.10;

    // Relic rarity weights (higher = more likely)
    public static double RelicWeightCommon   { get; private set; } = 50.0;
    public static double RelicWeightUncommon { get; private set; } = 33.0;
    public static double RelicWeightRare     { get; private set; } = 17.0;
    public static double RelicWeightShop     { get; private set; } = 5.0;
    public static double RelicWeightAncient  { get; private set; } = 2.0;

    // --- Shop: random item ---

    public static double RandomItemBaseCost { get; private set; } = 75.0;
    public static double RandomItemCostScaling { get; private set; } = 1.10;

    // Item type weights (Curse is now a sub-type of Card)
    public static double ItemWeightRelic  { get; private set; } = 30.0;
    public static double ItemWeightCard   { get; private set; } = 40.0;
    public static double ItemWeightPotion { get; private set; } = 25.0;

    // Card sub-type weights for random item
    // Common/Uncommon/Rare = player's character card pool
    // Colorless = colorless pool
    // Curse = curse cards
    public static double CardWeightCommon    { get; private set; } = 55.0;
    public static double CardWeightUncommon  { get; private set; } = 25.0;
    public static double CardWeightRare      { get; private set; } = 10.0;
    public static double CardWeightColorless { get; private set; } = 5.0;
    public static double CardWeightCurse     { get; private set; } = 5.0;

    // Relic rarity weights for random item (separate from dedicated relic button)
    public static double ItemRelicWeightCommon   { get; private set; } = 50.0;
    public static double ItemRelicWeightUncommon { get; private set; } = 33.0;
    public static double ItemRelicWeightRare     { get; private set; } = 17.0;
    public static double ItemRelicWeightShop     { get; private set; } = 5.0;
    public static double ItemRelicWeightAncient  { get; private set; } = 2.0;

    // Potion rarity weights for random item
    public static double PotionWeightCommon   { get; private set; } = 65.0;
    public static double PotionWeightUncommon { get; private set; } = 25.0;
    public static double PotionWeightRare     { get; private set; } = 10.0;

    // -------------------------------------------------------------------------
    // Setters (called from registration/hydration)
    // -------------------------------------------------------------------------

    public static void SetHpSacrificePercent(double v)      => HpSacrificePercent      = Math.Clamp(v, 0.01, 1.0);
    public static void SetSacrificeRepeatCount(double v)    => SacrificeRepeatCount    = Math.Max(1, (int)Math.Round(v));
    public static void SetScoreToGoldPercent(double v)      => ScoreToGoldPercent      = Math.Max(0.0, v);
    public static void SetMinPreviousScore(double v)        => MinPreviousScore        = Math.Max(0, (int)Math.Round(v));

    public static void SetShopRerollBaseCost(double v)      => ShopRerollBaseCost      = Math.Max(0.0, v);
    public static void SetShopRerollCostScaling(double v)   => ShopRerollCostScaling   = Math.Max(1.0, v);

    public static void SetRelicBaseCost(double v)           => RelicBaseCost           = Math.Max(0.0, v);
    public static void SetRelicCostScaling(double v)        => RelicCostScaling        = Math.Max(1.0, v);
    public static void SetRelicWeightCommon(double v)       => RelicWeightCommon       = Math.Max(0.0, v);
    public static void SetRelicWeightUncommon(double v)     => RelicWeightUncommon     = Math.Max(0.0, v);
    public static void SetRelicWeightRare(double v)         => RelicWeightRare         = Math.Max(0.0, v);
    public static void SetRelicWeightShop(double v)         => RelicWeightShop         = Math.Max(0.0, v);
    public static void SetRelicWeightAncient(double v)      => RelicWeightAncient      = Math.Max(0.0, v);

    public static void SetRandomItemBaseCost(double v)      => RandomItemBaseCost      = Math.Max(0.0, v);
    public static void SetRandomItemCostScaling(double v)   => RandomItemCostScaling   = Math.Max(1.0, v);
    public static void SetItemWeightRelic(double v)         => ItemWeightRelic         = Math.Max(0.0, v);
    public static void SetItemWeightCard(double v)          => ItemWeightCard          = Math.Max(0.0, v);
    public static void SetItemWeightPotion(double v)        => ItemWeightPotion        = Math.Max(0.0, v);
    public static void SetCardWeightCommon(double v)        => CardWeightCommon        = Math.Max(0.0, v);
    public static void SetCardWeightUncommon(double v)      => CardWeightUncommon      = Math.Max(0.0, v);
    public static void SetCardWeightRare(double v)          => CardWeightRare          = Math.Max(0.0, v);
    public static void SetCardWeightColorless(double v)     => CardWeightColorless     = Math.Max(0.0, v);
    public static void SetCardWeightCurse(double v)         => CardWeightCurse         = Math.Max(0.0, v);
    public static void SetItemRelicWeightCommon(double v)   => ItemRelicWeightCommon   = Math.Max(0.0, v);
    public static void SetItemRelicWeightUncommon(double v) => ItemRelicWeightUncommon = Math.Max(0.0, v);
    public static void SetItemRelicWeightRare(double v)     => ItemRelicWeightRare     = Math.Max(0.0, v);
    public static void SetItemRelicWeightShop(double v)     => ItemRelicWeightShop     = Math.Max(0.0, v);
    public static void SetItemRelicWeightAncient(double v)  => ItemRelicWeightAncient  = Math.Max(0.0, v);
    public static void SetPotionWeightCommon(double v)      => PotionWeightCommon      = Math.Max(0.0, v);
    public static void SetPotionWeightUncommon(double v)    => PotionWeightUncommon    = Math.Max(0.0, v);
    public static void SetPotionWeightRare(double v)        => PotionWeightRare        = Math.Max(0.0, v);

    public static void ResetToDefaults()
    {
        HpSacrificePercent      = 0.20;
        SacrificeRepeatCount    = 4;
        ScoreToGoldPercent      = 0.25;
        MinPreviousScore        = 500;
        ShopRerollBaseCost      = 150.0;
        ShopRerollCostScaling   = 1.10;
        RelicBaseCost           = 250.0;
        RelicCostScaling        = 1.10;
        RelicWeightCommon       = 50.0;
        RelicWeightUncommon     = 33.0;
        RelicWeightRare         = 17.0;
        RelicWeightShop         = 5.0;
        RelicWeightAncient      = 2.0;
        RandomItemBaseCost      = 75.0;
        RandomItemCostScaling   = 1.10;
        ItemWeightRelic         = 30.0;
        ItemWeightCard          = 40.0;
        ItemWeightPotion        = 25.0;
        CardWeightCommon        = 55.0;
        CardWeightUncommon      = 25.0;
        CardWeightRare          = 10.0;
        CardWeightColorless     = 5.0;
        CardWeightCurse         = 5.0;
        ItemRelicWeightCommon   = 50.0;
        ItemRelicWeightUncommon = 33.0;
        ItemRelicWeightRare     = 17.0;
        ItemRelicWeightShop     = 5.0;
        ItemRelicWeightAncient  = 2.0;
        PotionWeightCommon      = 65.0;
        PotionWeightUncommon    = 25.0;
        PotionWeightRare        = 10.0;
    }
}
