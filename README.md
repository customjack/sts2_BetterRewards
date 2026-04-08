# BetterRewards

BetterRewards changes how you choose rewards at the start of a run.

Instead of taking one of the normal Neow rewards, you can sacrifice HP for gold, then spend it in a special shop with unique options not found in any normal merchant — rerolling the stock, buying a random relic, or buying a random item.

## Description

BetterRewards gives you another choice at Neow. Sacrifice some HP to load up on gold, then visit the special shop. The shop has three extra actions alongside the normal merchant inventory:

- **Reroll Shop** — swap out all items in the merchant for a fresh roll
- **Random Relic** — buy a random relic drawn from configurable rarity weights (Common / Uncommon / Rare)
- **Random Item** — buy a random relic, card, or potion

All costs, HP sacrifice limits, and rarity weights are tunable via the in-game settings menu.

## Installation

**Requirements:** [ModManagerSettings](../ModManagerSettings) must be installed first.

1. Install ModManagerSettings (see its README).
2. Download the latest release zip from the [Releases](../../releases) page.
3. Close Slay the Spire 2.
4. In Steam, right-click `Slay the Spire 2` → `Properties` → `Installed Files` → `Browse`.
5. Create a `mods` folder in the game directory if it does not exist.
6. Extract the zip and drag the `BetterRewards` folder into `mods`.
7. Confirm these files are present in `mods/BetterRewards`:
   - `BetterRewards.dll`
   - `BetterRewards.json`
8. Launch Slay the Spire 2. If prompted to enable mods, accept and relaunch.
9. Go to `Settings` → `General` → `Mods` and enable `BetterRewards`.

## Main Features

- **HP-for-gold sacrifice** — after Neow, choose how much HP to give up for starting gold
- **Special shop** — access a persistent shop through the in-run pause menu for the whole run
- **Reroll** — spend gold to completely refresh the merchant's stock
- **Random Relic** — spend gold to roll a relic at configurable rarities
- **Random Item** — spend gold for a surprise relic, card, or potion
- **Fully configurable** — tweak costs, sacrifice range, gold rate, and rarity weights from the settings panel

## Configuration

Open the settings panel via `Settings` → `General` → `Mods` → `BetterRewards` → `Settings`:

| Setting | Description |
|---|---|
| Enable Mod | Master toggle for all BetterRewards features |
| HP Sacrifice Enabled | Allow sacrificing HP for gold after Neow |
| Min / Max HP Sacrifice | Clamps how much HP the player can sacrifice |
| Gold Per HP | Gold earned per HP sacrificed |
| Reroll Cost | Gold cost for the Reroll Shop action |
| Random Relic Cost | Gold cost for the Random Relic action |
| Random Item Cost | Gold cost for the Random Item action |
| Rarity Weights | Relative weights for Common / Uncommon / Rare relics |

## Requirements

- **Slay the Spire 2** (Steam)
- **[ModManagerSettings](../ModManagerSettings)** — provides the in-game settings panel

## Shout Outs

Heavily inspired by [**BetterRewards** by Skrelpoid](https://steamcommunity.com/sharedfiles/filedetails/?id=1491613936) on the Slay the Spire 1 Workshop — the original idea of trading Neow rewards for a special starting shop. Go give that mod a subscribe if you're still playing StS1.

## Developer Notes

**Requirements:** .NET SDK, Godot 4 export templates, WSL or Linux shell.

**Setup:**
1. Copy `.env.example` to `.env`.
2. Set `STS2_INSTALL_DIR` to your game install path.

**Build and install:**
```bash
./scripts/bash/build_and_stage.sh
./scripts/bash/install_to_game.sh
```

## License

MIT — see [LICENSE](LICENSE).
