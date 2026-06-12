# Catalyst Architecture

Turn-based roguelike deckbuilder prototype built with C# and Raylib.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

Raylib is pulled in automatically as a NuGet package.

## How to Run

```
cd cardgameTest
dotnet run --project CardGamePrototype.Client
```

Window opens at 1600×900 and is resizable.

## Controls

| Input | Action |
|---|---|
| Drag card → lane | Deploy unit to that lane (costs card's energy) |
| Click unit on battlefield | Execute unit — deals double damage + triggers effect (costs 1 energy) |
| **ENTER** | End turn |
| **Q** | Activate ability |
| **P** | Pause / resume |
| **SPACE** | Execute selected unit |
| **ESC** | Back / close (context-sensitive) |
| F3 | Toggle skill tree debug mode |
| F5 | Toggle Sci-Fi / Fantasy theme |

## Gameplay

### Campaign
- 6-node campaign: 3 minion sectors → 1 elite → 1 boss
- HP persists between battles — don't let it hit zero
- Defeat resets the run; beating the boss ends it with a victory screen
- Gold rewards: 60 G (minion), 80 G (elite), 120 G (boss)
- After each battle (except the boss) pick a new card for your collection

### Energy
- 4 base energy per turn (skill tree can increase this)
- Deploying a card costs its printed energy cost
- Executing a deployed unit always costs 1 energy

### Cards & Hand
- Hand holds up to 5 cards, refilled at the start of each turn
- No duplicate cards in hand at the same time
- 2-cycle cooldown: must play 2 other cards before any card returns to your hand
- Click **DIS** to inspect your discard pile

### Units
- Deploying a card places a unit on the chosen lane
- Units deal damage automatically at end of turn
- Executing a unit deals double damage + triggers its special effect, then removes it

### Ability
- Select one ability in the Deck Builder before battle
- Charges from damage dealt and damage taken (at different rates per ability)
- Activate with **Q** once fully charged

### Enemies
- **Minion** — basic attacker, targets occupied lanes
- **Elite** — alternates between lunging the player and striking units; attack scales each turn
- **Boss** — every 3rd turn hits all lanes and the player; enters phase 2 below 40% HP

## Themes

Press **F5** to switch themes.

- **Sci-Fi** — three-column grid layout with animated enemy, energy pips, and element-coloured cards
- **Fantasy** — 5-layer atmospheric dungeon battlefield. Hover **HAND** (bottom-left) to reveal cards. Hover the battlefield to enter execute mode.

Both themes share animated HP bars (ghost trail drain effect) and all game logic.

## Screens

| Screen | How to reach |
|---|---|
| Title | Launch, or after a run ends |
| Campaign Map | Title → Story Mode; shows current HP, gold, and SP |
| Market & Skills | "MARKET / SKILLS" on map |
| Deck Builder | "DECK BUILDER" on map |
| Battle | Click a node on the campaign map |
| Reward | After each non-boss victory |
| Victory | After defeating the boss |

Progress is saved automatically when you quit to title or pick a reward.

## Project Structure

```
cardgameTest/
  CardGamePrototype.Core/    # Game logic (cards, turn manager, abilities, effects)
  CardGamePrototype.Client/  # Rendering and input (Raylib, Program.cs)
  CardGamePrototype.Tests/   # Unit tests
```

Run tests:

```
cd cardgameTest
dotnet test
```
