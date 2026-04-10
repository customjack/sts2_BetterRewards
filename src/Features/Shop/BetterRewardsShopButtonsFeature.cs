using System.Reflection;
using BetterRewards.Features.Settings;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace BetterRewards.Features.Shop;

/// <summary>
/// Injects three special BetterRewards buttons into the top-left of the merchant screen
/// when a BetterRewards shop visit is active.
/// </summary>
internal static class BetterRewardsShopButtonsFeature
{
    private const string ContainerName = "BetterRewardsShopButtons";
    private const string RerollIconResourceName = "BetterRewards.RerollIconPng";
    private const string RandomRelicIconResourceName = "BetterRewards.RandomRelicIconPng";
    private const string RandomItemIconResourceName = "BetterRewards.RandomItemIconPng";
    private const string LocTableName = "better_rewards_shop";
    private const float ActionIconSize = 80f;
    private static readonly Vector2 ShopNormalScale = Vector2.One;
    private static readonly Vector2 ShopHoverScale = Vector2.One * 1.15f;

    private static Texture2D? _rerollIconTexture;
    private static Texture2D? _randomRelicIconTexture;
    private static Texture2D? _randomItemIconTexture;

    private static bool _isActiveShop;
    public static bool IsActiveShop
    {
        get => _isActiveShop;
        set
        {
            _isActiveShop = value;
            if (!value) _activeRefreshAction = null;
        }
    }

    public static void TryAttachButtons(NMerchantInventory merchantInventory)
    {
        if (!IsActiveShop)
        {
            return;
        }

        // Attach to %SlotsContainer so buttons are positioned relative to the merchant rug,
        // not the full-screen inventory control (which is offset off-screen until opened).
        var slotsContainer = merchantInventory.GetNodeOrNull<Control>("%SlotsContainer");
        if (slotsContainer == null)
        {
            Log.Warn("[BetterRewards] %SlotsContainer not found on NMerchantInventory — buttons not attached.");
            return;
        }

        if (slotsContainer.GetNodeOrNull<Control>(ContainerName) != null)
        {
            return;
        }

        var container = new VBoxContainer
        {
            Name = ContainerName,
        };
        container.AddThemeConstantOverride("separation", 12);

        ShopActionUi[]? buttons = null;

        void RefreshLabels()
        {
            if (buttons == null)
            {
                return;
            }

            foreach (var btn in buttons)
            {
                SetCost(btn);
            }
        }

        _activeRefreshAction = RefreshLabels;

        _rerollIconTexture ??= LoadEmbeddedPngTexture(RerollIconResourceName);
        _randomRelicIconTexture ??= LoadEmbeddedPngTexture(RandomRelicIconResourceName);
        _randomItemIconTexture ??= LoadEmbeddedPngTexture(RandomItemIconResourceName);

        EnsureTooltipLocalization();

        var rerollButton = MakeIconAction(
            "reroll",
            _rerollIconTexture,
            () => BetterRewardsShopState.CurrentRerollCost,
            merchantInventory);
        var relicButton = MakeIconAction(
            "random_relic",
            _randomRelicIconTexture,
            () => BetterRewardsShopState.CurrentRelicCost,
            merchantInventory);
        var randomButton = MakeIconAction(
            "random_item",
            _randomItemIconTexture,
            () => BetterRewardsShopState.CurrentRandomItemCost,
            merchantInventory);

        buttons = new[] { rerollButton, relicButton, randomButton };

        rerollButton.OnPressed = () =>
        {
            var player = GetLocalPlayer();
            if (player == null)
            {
                return;
            }

            var cost = BetterRewardsShopState.CurrentRerollCost;
            if (player.Gold < cost)
            {
                return;
            }

            TaskHelper.RunSafely(DoReroll(player, cost, RefreshLabels, merchantInventory));
        };

        relicButton.OnPressed = () =>
        {
            var player = GetLocalPlayer();
            if (player == null)
            {
                return;
            }

            var cost = BetterRewardsShopState.CurrentRelicCost;
            if (player.Gold < cost)
            {
                return;
            }

            TaskHelper.RunSafely(DoRandomRelic(player, cost, RefreshLabels));
        };

        randomButton.OnPressed = () =>
        {
            var player = GetLocalPlayer();
            if (player == null)
            {
                return;
            }

            var cost = BetterRewardsShopState.CurrentRandomItemCost;
            if (player.Gold < cost)
            {
                return;
            }

            TaskHelper.RunSafely(DoRandomItem(player, cost, RefreshLabels));
        };

        container.AddChild(rerollButton.Root);
        container.AddChild(relicButton.Root);
        container.AddChild(randomButton.Root);
        slotsContainer.AddChild(container);

        // Deferred so gold is readable after the node enters the tree.
        // Purchase event subscriptions happen in TrySubscribePurchaseEvents, called from
        // the Initialize patch (after Inventory is populated).
        Callable.From(RefreshLabels).CallDeferred();

        void PositionContainer()
        {
            if (!GodotObject.IsInstanceValid(container) || !GodotObject.IsInstanceValid(slotsContainer))
            {
                return;
            }

            var containerSize = container.GetCombinedMinimumSize();
            var availableSize = slotsContainer.Size;
            if (availableSize.X <= 0f || availableSize.Y <= 0f)
                availableSize = slotsContainer.GetRect().Size;

            // Bottom-left of the merchant window + 5% from the left, 10% up from the bottom.
            var x = availableSize.X * 0.075f;
            var y = availableSize.Y - containerSize.Y - availableSize.Y * 0.175f;

            container.Position = new Vector2(x, MathF.Max(0f, y));
        }

        slotsContainer.Resized += PositionContainer;
        Callable.From(PositionContainer).CallDeferred();
    }

    /// <summary>
    /// Called from the Initialize patch once Inventory is populated.
    /// Subscribes RefreshLabels to every entry's PurchaseCompleted so that
    /// buying anything in the shop (card, relic, potion, card removal, or our
    /// own buttons) immediately updates our price label colors.
    /// </summary>
    public static void TrySubscribePurchaseEvents(NMerchantInventory merchantInventory)
    {
        if (!IsActiveShop) return;

        var slotsContainer = merchantInventory.GetNodeOrNull<Control>("%SlotsContainer");
        if (slotsContainer == null) return;

        var container = slotsContainer.GetNodeOrNull<Control>(ContainerName);
        if (container == null) return;

        // Find the RefreshLabels closure captured in TryAttachButtons by looking for
        // the buttons node — we need to call SetCost on all ShopActionUi children.
        // Simpler: just keep a static registry of active refresh delegates.
        if (_activeRefreshAction == null) return;

        if (merchantInventory.Inventory == null) return;

        foreach (var entry in merchantInventory.Inventory.AllEntries)
        {
            entry.PurchaseCompleted += (_, _) => _activeRefreshAction?.Invoke();
        }
    }

    // Stores the most recent RefreshLabels delegate so the Initialize patch can subscribe it.
    private static Action? _activeRefreshAction;

    private static readonly MethodInfo? _restockAfterPurchase =
        typeof(MerchantEntry).GetMethod("RestockAfterPurchase", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo? _setCardRemovalUsed =
        typeof(MerchantCardRemovalEntry).GetProperty("Used", BindingFlags.Public | BindingFlags.Instance)
            ?.GetSetMethod(nonPublic: true);

    // NMerchantCardRemoval._isUnavailable is a private bool that gate-keeps UpdateVisual.
    // Once true it never resets, so we must reflect to clear it before re-enabling the slot.
    private static readonly FieldInfo? _isUnavailableField =
        typeof(NMerchantCardRemoval).GetField("_isUnavailable", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? _animatorField =
        typeof(NMerchantCardRemoval).GetField("_animator", BindingFlags.NonPublic | BindingFlags.Instance);

    private static async Task DoReroll(Player player, int cost, Action refreshLabels, NMerchantInventory nInventory)
    {
        await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
        BetterRewardsShopState.RecordRerollUse();

        var inventory = NMerchantRoom.Instance?.Inventory?.Inventory;
        if (inventory == null)
        {
            Log.Warn("[BetterRewards] DoReroll: MerchantInventory not found.");
            refreshLabels();
            return;
        }

        // Re-populate card entries, then notify UI for each one individually.
        // (CalcCost throws if CreationResult is null, so we must Populate before notifying.)
        foreach (var entry in inventory.CardEntries)
        {
            entry.Populate();
            entry.OnMerchantInventoryUpdated();
        }

        // Re-populate relic and potion entries (including purchased/cleared slots).
        if (_restockAfterPurchase != null)
        {
            foreach (var entry in inventory.RelicEntries)
            {
                _restockAfterPurchase.Invoke(entry, new object?[] { inventory });
                entry.OnMerchantInventoryUpdated();
            }
            foreach (var entry in inventory.PotionEntries)
            {
                _restockAfterPurchase.Invoke(entry, new object?[] { inventory });
                entry.OnMerchantInventoryUpdated();
            }
        }
        else
        {
            Log.Warn("[BetterRewards] Could not reflect RestockAfterPurchase — relics/potions not rerolled.");
        }

        // Reset card removal — re-enable the service for this rerolled shop visit.
        var cardRemoval = inventory.CardRemovalEntry;
        if (cardRemoval != null)
        {
            _setCardRemovalUsed?.Invoke(cardRemoval, new object[] { false });
            cardRemoval.CalcCost();

            var cardRemovalNode = nInventory.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
            if (cardRemovalNode != null)
            {
                _isUnavailableField?.SetValue(cardRemovalNode, false);

                if (_animatorField?.GetValue(cardRemovalNode) is AnimationPlayer animator)
                {
                    animator.CurrentAnimation = "Used";
                    animator.Seek(0.0, true);
                    animator.Stop();
                }

                var hitbox = cardRemovalNode.Hitbox;
                if (hitbox != null)
                {
                    hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
                    cardRemovalNode.FocusMode = Control.FocusModeEnum.All;
                }
            }

            cardRemoval.OnMerchantInventoryUpdated();
        }

        // NMerchantCard.UpdateVisual sets Visible=false on purchase but never re-shows it.
        // After re-populating all entries, force all slots visible again.
        foreach (var slot in nInventory.GetAllSlots())
        {
            if (slot.Entry.IsStocked)
            {
                slot.Visible = true;
            }
        }

        refreshLabels();
        Log.Info("[BetterRewards] Shop rerolled in-place.");
    }

    private static async Task DoRandomRelic(Player player, int cost, Action refreshLabels)
    {
        await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);

        var rarity = RollRelicRarity(player.PlayerRng.Rewards,
            BetterRewardsSettings.RelicWeightCommon,
            BetterRewardsSettings.RelicWeightUncommon,
            BetterRewardsSettings.RelicWeightRare,
            BetterRewardsSettings.RelicWeightShop,
            BetterRewardsSettings.RelicWeightAncient);
        // Ancient relics are excluded from the grab bag entirely (Populate filters them out).
        // Pull directly from ModelDb for Ancient, bypassing the grab bag.
        RelicModel relic;
        if (rarity == RelicRarity.Ancient)
        {
            relic = PullRandomAncientRelic(player) ?? RelicFactory.PullNextRelicFromFront(player, RelicRarity.Rare);
        }
        else
        {
            relic = RelicFactory.PullNextRelicFromFront(player, rarity);
        }

        var mutable = relic.IsMutable ? relic : relic.ToMutable();
        Log.Info($"[BetterRewards] Random Relic → {rarity} relic: {relic.Id}");
        await RelicCmd.Obtain(mutable, player);

        BetterRewardsShopState.RecordRelicUse();
        refreshLabels();
    }

    private static async Task DoRandomItem(Player player, int cost, Action refreshLabels)
    {
        await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);

        var itemType = RollItemType();
        Log.Info($"[BetterRewards] Random Item rolled type: {itemType}");
        try
        {
            switch (itemType)
            {
                case RandomItemType.Relic:
                {
                    var rarity = RollRelicRarity(player.PlayerRng.Rewards,
                        BetterRewardsSettings.ItemRelicWeightCommon,
                        BetterRewardsSettings.ItemRelicWeightUncommon,
                        BetterRewardsSettings.ItemRelicWeightRare,
                        BetterRewardsSettings.ItemRelicWeightShop,
                        BetterRewardsSettings.ItemRelicWeightAncient);
                    RelicModel relic;
                    if (rarity == RelicRarity.Ancient)
                    {
                        relic = PullRandomAncientRelic(player) ?? RelicFactory.PullNextRelicFromFront(player, RelicRarity.Rare);
                    }
                    else
                    {
                        relic = RelicFactory.PullNextRelicFromFront(player, rarity);
                    }
                    var mutable = relic.IsMutable ? relic : relic.ToMutable();
                    Log.Info($"[BetterRewards] Random Item (Relic) → {rarity}: {relic.Id}");
                    await RelicCmd.Obtain(mutable, player);
                    break;
                }
                case RandomItemType.Card:
                {
                    var cardSubType = RollCardSubType();
                    Log.Info($"[BetterRewards] Random Item (Card) → sub-type: {cardSubType}");
                    await GiveRandomCard(player, cardSubType);
                    break;
                }
                case RandomItemType.Potion:
                {
                    var potion = PotionFactory.CreateRandomPotionOutOfCombat(player, player.PlayerRng.Rewards);
                    if (potion != null)
                    {
                        Log.Info($"[BetterRewards] Random Item (Potion) → {potion.Rarity}: {potion.Id}");
                        await PotionCmd.TryToProcure(potion.ToMutable(), player);
                    }
                    else
                    {
                        Log.Warn("[BetterRewards] Random Item (Potion) → PotionFactory returned null.");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterRewards] Random item grant failed: {ex.Message}\n{ex.StackTrace}");
        }

        BetterRewardsShopState.RecordRandomItemUse();
        refreshLabels();
    }

    private static async Task GiveRandomCard(Player player, CardSubType subType)
    {
        var constraint = player.RunState.CardMultiplayerConstraint;

        CardModel? canonical = null;

        switch (subType)
        {
            case CardSubType.Common:
            case CardSubType.Uncommon:
            case CardSubType.Rare:
            {
                var targetRarity = subType switch
                {
                    CardSubType.Common   => CardRarity.Common,
                    CardSubType.Uncommon => CardRarity.Uncommon,
                    _                    => CardRarity.Rare
                };
                var candidates = player.Character.CardPool
                    .GetUnlockedCards(player.UnlockState, constraint)
                    .Where(c => c.Rarity == targetRarity)
                    .ToList();
                Log.Info($"[BetterRewards] Card ({targetRarity}) pool size after constraint filter: {candidates.Count}");
                if (candidates.Count == 0)
                {
                    // Fallback: factory picks
                    var options = CardCreationOptions.ForNonCombatWithDefaultOdds(new[] { player.Character.CardPool });
                    var results = CardFactory.CreateForReward(player, 1, options).ToList();
                    if (results.Count > 0)
                    {
                        canonical = results[0].Card;
                        Log.Info($"[BetterRewards] Card ({targetRarity}) fallback → {canonical.Id}");
                    }
                }
                else
                {
                    canonical = candidates[player.PlayerRng.Rewards.NextInt(candidates.Count)];
                    Log.Info($"[BetterRewards] Card ({targetRarity}) → {canonical.Id}");
                    canonical = player.RunState.CreateCard(canonical, player);
                }
                break;
            }
            case CardSubType.Colorless:
            {
                var candidates = ModelDb.CardPool<ColorlessCardPool>()
                    .GetUnlockedCards(player.UnlockState, constraint)
                    .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Curse
                                && c.Rarity != CardRarity.Status && c.Rarity != CardRarity.Token)
                    .ToList();
                Log.Info($"[BetterRewards] Colorless pool size after constraint filter: {candidates.Count}");
                if (candidates.Count > 0)
                {
                    canonical = candidates[player.PlayerRng.Rewards.NextInt(candidates.Count)];
                    Log.Info($"[BetterRewards] Card (Colorless) → {canonical.Id}");
                    canonical = player.RunState.CreateCard(canonical, player);
                }
                break;
            }
            case CardSubType.Curse:
            {
                var curses = player.Character.CardPool
                    .GetUnlockedCards(player.UnlockState, constraint)
                    .Where(c => c.Rarity == CardRarity.Curse)
                    .ToList();
                if (curses.Count == 0)
                {
                    curses = ModelDb.CardPool<ColorlessCardPool>()
                        .GetUnlockedCards(player.UnlockState, constraint)
                        .Where(c => c.Rarity == CardRarity.Curse)
                        .ToList();
                }
                Log.Info($"[BetterRewards] Curse pool size after constraint filter: {curses.Count}");
                if (curses.Count > 0)
                {
                    var curse = curses[player.PlayerRng.Rewards.NextInt(curses.Count)];
                    Log.Info($"[BetterRewards] Card (Curse) → {curse.Id}");
                    var mutableCurse = player.RunState.CreateCard(curse, player);
                    await CardPileCmd.AddCursesToDeck(new[] { mutableCurse }, player);
                    AnimateCardToDeck(mutableCurse, player);
                    return;
                }
                break;
            }
        }

        if (canonical != null)
        {
            // Ensure it's registered in RunState (CreateCard already does this; fallback card may not be)
            if (!player.RunState.ContainsCard(canonical))
            {
                canonical = player.RunState.CreateCard(canonical, player);
            }
            await CardPileCmd.Add(canonical, PileType.Deck);
            AnimateCardToDeck(canonical, player);
        }
    }

    private static void AnimateCardToDeck(CardModel card, Player player)
    {
        try
        {
            var globalUi = NRun.Instance?.GlobalUi;
            if (globalUi == null) return;

            var cardNode = NCard.Create(card);
            if (cardNode == null) return;
            cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            var trailContainer = globalUi.TopBar.TrailContainer;
            trailContainer.AddChildSafely(cardNode);

            // Start from the merchant inventory region (center of screen is a safe default).
            cardNode.GlobalPosition = new Vector2(globalUi.Size.X * 0.5f, globalUi.Size.Y * 0.5f);

            var targetPos = PileType.Deck.GetTargetPosition(cardNode);
            var vfx = NCardFlyVfx.Create(cardNode, targetPos, isAddingToPile: true, player.Character.TrailPath);
            if (vfx != null)
            {
                trailContainer.AddChildSafely(vfx);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterRewards] AnimateCardToDeck failed (non-fatal): {ex.Message}");
        }
    }

    private static Player? GetLocalPlayer()
    {
        // NMerchantRoom.Inventory is NMerchantInventory; its .Inventory is MerchantInventory which holds Player.
        var player = NMerchantRoom.Instance?.Inventory?.Inventory?.Player;
        if (player != null && LocalContext.IsMe(player))
        {
            return player;
        }

        return null;
    }

    private static RelicRarity RollRelicRarity(
        MegaCrit.Sts2.Core.Random.Rng rng,
        double wCommon, double wUncommon, double wRare, double wShop, double wAncient)
    {
        var weights = new (RelicRarity rarity, double weight)[]
        {
            (RelicRarity.Common,   wCommon),
            (RelicRarity.Uncommon, wUncommon),
            (RelicRarity.Rare,     wRare),
            (RelicRarity.Shop,     wShop),
            (RelicRarity.Ancient,  wAncient),
        };
        return RollWeighted(rng, weights);
    }

    private enum RandomItemType { Relic, Card, Potion }

    private static RandomItemType RollItemType()
    {
        var weights = new (RandomItemType type, double weight)[]
        {
            (RandomItemType.Relic,  BetterRewardsSettings.ItemWeightRelic),
            (RandomItemType.Card,   BetterRewardsSettings.ItemWeightCard),
            (RandomItemType.Potion, BetterRewardsSettings.ItemWeightPotion),
        };
        var total = weights.Sum(w => w.weight);
        var roll  = System.Random.Shared.NextDouble() * total;
        var acc   = 0.0;
        foreach (var (type, weight) in weights)
        {
            acc += weight;
            if (roll < acc)
            {
                return type;
            }
        }
        return RandomItemType.Card;
    }

    private enum CardSubType { Common, Uncommon, Rare, Colorless, Curse }

    private static CardSubType RollCardSubType()
    {
        var weights = new (CardSubType sub, double weight)[]
        {
            (CardSubType.Common,    BetterRewardsSettings.CardWeightCommon),
            (CardSubType.Uncommon,  BetterRewardsSettings.CardWeightUncommon),
            (CardSubType.Rare,      BetterRewardsSettings.CardWeightRare),
            (CardSubType.Colorless, BetterRewardsSettings.CardWeightColorless),
            (CardSubType.Curse,     BetterRewardsSettings.CardWeightCurse),
        };
        var total = weights.Sum(w => w.weight);
        var roll  = System.Random.Shared.NextDouble() * total;
        var acc   = 0.0;
        foreach (var (sub, weight) in weights)
        {
            acc += weight;
            if (roll < acc)
            {
                return sub;
            }
        }
        return CardSubType.Common;
    }

    private static T RollWeighted<T>(MegaCrit.Sts2.Core.Random.Rng rng, (T item, double weight)[] weights)
    {
        var total = weights.Sum(w => w.weight);
        if (total <= 0)
        {
            return weights[0].item;
        }

        var roll = rng.NextFloat() * (float)total;
        var acc  = 0.0;
        foreach (var (item, weight) in weights)
        {
            acc += weight;
            if (roll < acc)
            {
                return item;
            }
        }
        return weights[^1].item;
    }

    private static bool _locTableRegistered;

    private static void EnsureTooltipLocalization()
    {
        if (_locTableRegistered)
        {
            return;
        }

        _locTableRegistered = true;

        var entries = new Dictionary<string, string>
        {
            ["reroll.title"]             = "Reroll Shop",
            ["reroll.description"]       = "Replace the merchant stock with a fresh roll.",
            ["random_relic.title"]       = "Random Relic",
            ["random_relic.description"] = "Buy a random relic from the configured rarity weights.",
            ["random_item.title"]        = "Random Item",
            ["random_item.description"]  = "Buy a random relic, card, or potion.",
        };

        // LocManager throws if the table doesn't exist — create and register it.
        var manager = LocManager.Instance;
        var tablesField = typeof(LocManager).GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance);
        if (tablesField?.GetValue(manager) is Dictionary<string, MegaCrit.Sts2.Core.Localization.LocTable> tables)
        {
            if (!tables.ContainsKey(LocTableName))
            {
                tables[LocTableName] = new MegaCrit.Sts2.Core.Localization.LocTable(LocTableName, new Dictionary<string, string>());
            }

            tables[LocTableName].MergeWith(entries);
        }
        else
        {
            Log.Warn("[BetterRewards] Could not access LocManager._tables — hover tooltips will use fallback text.");
        }
    }

    private static ShopActionUi MakeIconAction(string id, Texture2D? iconTexture, Func<int> getCost, NMerchantInventory merchantInventory)
    {
        var root = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        root.AddThemeConstantOverride("separation", 4);

        var action = new TextureButton
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(ActionIconSize, ActionIconSize),
            PivotOffset = new Vector2(ActionIconSize * 0.5f, ActionIconSize * 0.5f),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            TextureNormal = iconTexture,
        };

        // Extract gold icon texture and font from a live slot's Cost node, then build
        // our own small HBoxContainer — bypasses all Godot container scaling issues.
        var (goldTexture, costFont) = ExtractCostAssets(merchantInventory);

        var costRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        costRow.AddThemeConstantOverride("separation", 3);

        if (goldTexture != null)
        {
            var goldIcon = new TextureRect
            {
                Texture = goldTexture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                CustomMinimumSize = new Vector2(20f, 20f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            };
            costRow.AddChild(goldIcon);
        }

        var priceLabel = new Label
        {
            Text = getCost().ToString(),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (costFont != null)
        {
            priceLabel.AddThemeFontOverride("font", costFont);
        }
        priceLabel.AddThemeFontSizeOverride("font_size", 18);
        costRow.AddChild(priceLabel);

        var ui = new ShopActionUi(root, action, priceLabel, getCost);

        Tween? activeTween = null;
        action.MouseEntered += () =>
        {
            activeTween?.Kill();
            activeTween = action.CreateTween();
            activeTween.TweenProperty(action, "scale", ShopHoverScale, 0.08)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            ShowHoverTip(action, id);
        };
        action.MouseExited += () =>
        {
            activeTween?.Kill();
            activeTween = action.CreateTween();
            activeTween.TweenProperty(action, "scale", ShopNormalScale, 0.15)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            HideHoverTip(action);
        };
        action.Pressed += () =>
        {
            ui.OnPressed?.Invoke();
        };

        root.AddChild(action);
        root.AddChild(costRow);

        return ui;
    }

    private static void ShowHoverTip(Control owner, string id)
    {
        NHoverTipSet.Remove(owner);
        var tip = new HoverTip(
            new LocString(LocTableName, $"{id}.title"),
            new LocString(LocTableName, $"{id}.description"));
        var tipSet = NHoverTipSet.CreateAndShow(owner, tip);
        tipSet.GlobalPosition = owner.GlobalPosition + new Vector2(ActionIconSize + 8f, -40f);
    }

    private static void HideHoverTip(Control owner)
    {
        NHoverTipSet.Remove(owner);
    }

    private static (Texture2D? goldTexture, Font? font) ExtractCostAssets(NMerchantInventory merchantInventory)
    {
        Texture2D? goldTexture = null;
        Font? font = null;

        // Look in the card removal node and all slots for the Cost subtree.
        var candidates = new List<Node?> { merchantInventory.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval") };
        candidates.AddRange(merchantInventory.GetAllSlots().Cast<Node?>());

        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;

            // Find first TextureRect anywhere in the subtree as the gold icon.
            if (goldTexture == null)
            {
                goldTexture = FindFirstTextureRect(candidate)?.Texture;
            }

            // Find the MegaLabel for the font.
            if (font == null)
            {
                var megaLabel = FindFirstMegaLabel(candidate);
                if (megaLabel != null)
                {
                    font = megaLabel.GetThemeFont("font");
                }
            }

            if (goldTexture != null && font != null) break;
        }

        return (goldTexture, font);
    }

    private static TextureRect? FindFirstTextureRect(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is TextureRect rect && rect.Texture != null)
            {
                return rect;
            }

            var found = FindFirstTextureRect(child);
            if (found != null) return found;
        }

        return null;
    }

    private static MegaLabel? FindFirstMegaLabel(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MegaLabel label)
            {
                return label;
            }

            var found = FindFirstMegaLabel(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetAllMouseFiltersIgnore(Node node)
    {
        if (node is Control c)
        {
            c.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        foreach (var child in node.GetChildren())
        {
            SetAllMouseFiltersIgnore(child);
        }
    }

    private static void SetCost(ShopActionUi ui)
    {
        var cost = ui.GetCost();
        if (ui.PriceLabel != null)
        {
            ui.PriceLabel.Text = cost.ToString();
            ui.PriceLabel.Modulate = HasEnoughGold(cost) ? StsColors.cream : StsColors.red;
        }
    }

    /// <summary>
    /// Picks a random unlocked Ancient relic the player doesn't already own.
    /// Ancient relics are excluded from the normal grab bag, so we query ModelDb directly.
    /// Returns null if no candidates exist (caller should fall back to Rare).
    /// </summary>
    private static RelicModel? PullRandomAncientRelic(Player player)
    {
        var owned = new HashSet<string>(player.Relics.Select(r => r.Id.Entry), StringComparer.OrdinalIgnoreCase);

        var candidates = ModelDb.AllRelicPools
            .SelectMany(pool => pool.GetUnlockedRelics(player.UnlockState))
            .Where(r => r.Rarity == RelicRarity.Ancient && !owned.Contains(r.Id.Entry))
            .Distinct()
            .ToList();

        if (candidates.Count == 0)
        {
            Log.Info("[BetterRewards] PullRandomAncientRelic: no candidates, falling back.");
            return null;
        }

        var index = player.PlayerRng.Rewards.NextInt(candidates.Count);
        var relic = candidates[index];
        // Remove from shared grab bag to avoid duplicates with future normal pulls.
        player.RunState.SharedRelicGrabBag.Remove(relic);
        Log.Info($"[BetterRewards] PullRandomAncientRelic → {relic.Id}");
        return relic;
    }

    private static bool HasEnoughGold(int cost)
    {
        var player = GetLocalPlayer();
        return player != null && player.Gold >= cost;
    }

    private static Texture2D? LoadEmbeddedPngTexture(string resourceName)
    {
        try
        {
            var assembly = typeof(BetterRewardsShopButtonsFeature).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Warn($"[BetterRewards] Missing embedded icon resource: {resourceName}");
                return null;
            }

            var bytes = new byte[stream.Length];
            var read = stream.Read(bytes, 0, bytes.Length);
            if (read != bytes.Length)
            {
                Log.Warn($"[BetterRewards] Could not read full icon resource: {resourceName}");
                return null;
            }

            var image = new Image();
            if (image.LoadPngFromBuffer(bytes) != Error.Ok)
            {
                Log.Warn($"[BetterRewards] Could not decode icon resource: {resourceName}");
                return null;
            }

            return ImageTexture.CreateFromImage(image);
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterRewards] Failed loading icon resource {resourceName}: {ex.Message}");
            return null;
        }
    }

    private sealed class ShopActionUi
    {
        public ShopActionUi(VBoxContainer root, TextureButton actionRoot, Label? priceLabel, Func<int> getCost)
        {
            Root = root;
            ActionRoot = actionRoot;
            PriceLabel = priceLabel;
            GetCost = getCost;
        }

        public VBoxContainer Root { get; }

        public TextureButton ActionRoot { get; }

        public Label? PriceLabel { get; }

        public Func<int> GetCost { get; }

        public Action? OnPressed { get; set; }
    }
}
