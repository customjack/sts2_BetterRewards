# BetterRewards

Adds a post-Neow event where you can sacrifice HP for gold, then spend it in a special shop with extra options beyond the normal merchant.

**Features:**
- After the Neow event, choose how much HP to sacrifice for gold
- Special shop with configurable extra actions:
  - **Reroll Shop** — replace the merchant stock with a fresh roll
  - **Random Relic** — buy a random relic at configurable rarity weights
  - **Random Item** — buy a random relic, card, or potion
- All costs and weights are configurable via ModManagerSettings

**Requires:** [ModManagerSettings](../ModManagerSettings)

## Install

1. Install [ModManagerSettings](../ModManagerSettings) first.
2. Download the latest release zip from the [Releases](../../releases) page.
3. Close Slay the Spire 2.
4. In Steam, right-click `Slay the Spire 2` -> `Properties` -> `Installed Files` -> `Browse`.
5. Create a `mods` folder in the game directory if it does not exist.
6. Extract the zip and drag the `BetterRewards` folder into `mods`.
7. Confirm these files are present in `mods/BetterRewards`:
   - `BetterRewards.dll`
   - `BetterRewards.json`
8. Launch Slay the Spire 2. If prompted to enable mods, accept and relaunch.
9. In-game, go to `Settings` -> `General` -> `Mods` and enable `BetterRewards`.

## Usage

After defeating Neow, a prompt appears letting you sacrifice HP for gold. The amount you can sacrifice and the gold conversion rate are both configurable. Once you have gold, you can spend it in the special shop at any point during the run.

Open the shop from the in-run menu and use the extra action buttons to reroll, buy a random relic, or buy a random item.

## Configuration

Settings are available via ModManagerSettings (Settings -> General -> Mods -> BetterRewards -> Settings):

| Setting | Description |
|---|---|
| Enable Mod | Master toggle for all BetterRewards features |
| HP Sacrifice Enabled | Allow sacrificing HP for gold after Neow |
| Min / Max HP Sacrifice | Clamps how much HP the player can sacrifice |
| Gold Per HP | Conversion rate (gold earned per HP sacrificed) |
| Reroll Cost | Gold cost for the Reroll Shop action |
| Random Relic Cost | Gold cost for the Random Relic action |
| Random Item Cost | Gold cost for the Random Item action |
| Rarity Weights | Relative weights for Common / Uncommon / Rare relics |

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
