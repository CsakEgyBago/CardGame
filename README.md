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
| Hover **HAND** pile (bottom-left) | Reveals hand cards for dragging |
| Drag card → lane | Deploy unit to that lane (costs card's energy) |
| Hover battlefield | Activates execute mode — click a unit to execute (costs 1 energy) |
| **END TURN** button | End your turn |
| **ACTIVATE** button | Use charged ability |
| F3 | Toggle skill tree debug mode |
| F5 | Toggle Sci-Fi / Fantasy theme |

## Gameplay

### Energy
- 4 base energy per turn (skill tree can increase this)
- Deploying a card costs its printed energy cost
- Executing a deployed unit always costs 1 energy

### Cards & Hand
- Hand holds up to 5 cards, refilled at the start of each turn
- No duplicate cards in hand at the same time
- 2-cycle cooldown: must play 2 other cards before any card returns to your hand
- Deck size: 10 cards, one copy each

### Units
- Deploying a card places a unit on the chosen lane
- Units deal damage automatically at end of turn
- Executing a unit deals double damage + triggers its special effect, then removes it

### Ability
- Select one ability in the Deck Builder before battle
- Charges from damage dealt and damage taken (at different rates per ability)
- Activate once fully charged

## Themes

Press **F5** to switch themes.

- **Sci-Fi** — three-column grid layout, always visible
- **Fantasy** — 5-layer atmospheric dungeon battlefield. Field always visible. Hover **HAND** (bottom-left) to bring up your cards for deployment. Hovering the battlefield activates execute mode.

## Screens

| Screen | How to reach |
|---|---|
| Campaign Map | Start from title, pick Story Mode |
| Market & Skills | "MARKET / SKILLS" button on map |
| Deck Builder | "DECK BUILDER" button on map |
| Battle | Click a node on the campaign map |

## Project Structure

```
cardgameTest/
  CardGamePrototype.Core/    # Game logic (cards, turn manager, abilities)
  CardGamePrototype.Client/  # Rendering and input (Raylib)
  CardGamePrototype.Tests/   # Unit tests
```

Run tests:

```
cd cardgameTest
dotnet test
```
