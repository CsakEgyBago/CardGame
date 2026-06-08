# Catalyst Architecture

A turn-based roguelike deckbuilder prototype built with C# and Raylib.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

That's it. Raylib is pulled in automatically as a NuGet package.

## How to Run

```
cd cardgameTest
dotnet run --project CardGamePrototype.Client
```

The window will open at 1600×900 and is resizable.

## Controls

| Input | Action |
|---|---|
| Mouse drag | Drag a card from your hand onto the battlefield grid to deploy it |
| Mouse hover | Hover a hand card to see an enlarged preview |
| Enter | End your turn |
| Space | Execute the selected board unit (double damage + special effect, then consumed) |
| F3 | Toggle skill tree debug mode |
| F5 | Toggle between Sci-Fi and Fantasy visual themes |

## Themes

Press **F5** at any time to switch themes. Both are fully playable.

- **Sci-Fi** — three-column layout with a perspective grid always visible in the center
- **Fantasy** — full-screen layout inspired by Slay the Spire: enemy panel at top, perspective grid in the middle, hand collapses to a small pile and fans out when you hover the bottom of the screen

## Project Structure

```
cardgameTest/
  CardGamePrototype.Core/    # Game logic (cards, battle state, turn manager)
  CardGamePrototype.Client/  # Rendering and input (Raylib)
  CardGamePrototype.Tests/   # Unit tests
```

To run the tests:

```
cd cardgameTest
dotnet test
```