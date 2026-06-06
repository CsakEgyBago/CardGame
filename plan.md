# CardGamePrototype — Context Snapshot

This document captures the current design and technical direction so we can keep moving without rediscovering the same ideas from scratch.

## 1. Project Overview

- **Working title:** CardGamePrototype
- **Genre:** Turn-based roguelike deckbuilder
- **Perspective:** 2.5D layered battlefield with hand-drawn art
- **Modes:** Single-player and 2-player co-op
- **Primary goal:** Deep tactical play with strong replayability and clear long-term progression

## 2. Core Design Goals

The game should feel like a strategy puzzle with momentum. Every turn should matter, every run should feel different, and co-op should amplify the same core systems instead of becoming a separate game with extra steps.

### Design pillars

Short list. Big consequences.

- Turn-based combat with meaningful sequencing
- Solo-first gameplay that still scales well to co-op
- Roguelike run structure with branching choices
- RNG-based unlocks that expand the collection
- Visually striking 2.5D presentation with depth and shadows

## 3. Core Combat Loop

### Catalyst and Executioner

Setup now, boom later.

Each card has two conceptual parts. This is the engine room.

1. **Catalyst** — establishes a setup state.
2. **Executioner** — consumes that state for a payoff.

This creates a combo language that works in both solo and co-op:

- In solo play, the player sequences cards within one hand.
- In co-op, one player can create the setup while the other triggers the payoff.
- Co-op should be powerful, but never mandatory.

### Example states and interactions

- Apply an element such as Fire, Frost, Void, Lightning, or Bio.
- Push or pull enemy positions on a spatial board.
- Create constructs or hazard zones that persist across turns.
- Trigger stronger effects when the board state matches a condition.

## 4. Card Taxonomy

Cards should be deep enough to support many builds without becoming unreadable.

### Card types

Four buckets. Resist the urge to make twenty.

- **Strike** — direct physical attacks and movement-based offense
- **Incantation** — elemental spells and battlefield setup
- **Construct** — persistent props, hazards, or stage objects
- **Reaction** — out-of-turn counters or conditional effects

### Elements

- Fire
- Frost
- Void
- Lightning
- Bio

Elements should do more than add damage. They should modify positioning, apply status tags, or enable follow-up effects.

### Classes

Suggested archetypes:

- **Elementalist** — battlefield control and elemental setup
- **Vanguard** — frontline pressure, defense, and displacement
- **Chrono-Thief** — timing, draw manipulation, and turn-order tricks

## 5. Abilities

When the draw order chooses violence.

Abilities sit outside the main deck and give each class a distinct support layer.

They should:

- help smooth bad draws
- reinforce class identity
- interact with board state or resources
- offer tactical decisions outside normal card play

## 6. Progression Model

### In-run progression

During a run, players can:

- draft new cards
- upgrade cards at forges
- socket glyphs or modifiers
- fuse duplicate cards
- gain temporary relics or buffs

### Meta progression

Long-term unlock loop. Variety first.

Between runs, players can:

- unlock new cards
- unlock new abilities
- unlock new classes or variants
- expand reward pools
- collect cosmetic or flavor unlocks

The unlock system should use chance, but mostly to increase variety rather than create large power gaps.

## 7. Run Structure

Risk, reward, adaptation, occasional self-inflicted disaster.

A standard run should include a branching path with:

- normal encounters
- elite fights
- special events
- upgrade nodes
- reward nodes
- bosses

This gives the familiar roguelike rhythm of risk, reward, and adaptation.

## 8. Visual Direction

The game should not be a full 3D experience. It should use 2.5D layering to create depth and readability.

### Visual goals

Pretty is optional. Readable is mandatory.

- hand-drawn card art
- foreground and background layers
- soft shadows and parallax
- clean card readability
- dramatic but readable combat feedback

The intended feel is a living tabletop diorama or pop-up book.

## 9. Technical Architecture

The technical stack should support deterministic rules, authoritative networking, and reusable shared logic.

### Recommended stack

- **Engine:** Unity
- **Language:** C#
- **Shared logic:** .NET Standard class library
- **Server:** headless authoritative C# service
- **Client:** thin rendering and input layer

### Why this approach works

- game rules stay testable and reusable
- the server can validate every action
- the client stays focused on visuals and input
- solo, co-op, and future platforms can share the same ruleset

### Networking model

Client asks. Server decides. Reality stays synced.

- client sends action intents
- server validates the action
- server runs deterministic simulation
- server broadcasts state deltas
- client replays those deltas visually

## 10. Shared Core Data Model

The shared simulation layer should contain the core combat rules and entity state so behavior stays consistent across client and server.

```csharp
namespace CardGamePrototype.Shared.Core.Engine
{
    using System.Collections.Generic;

    public enum ElementType : byte { None, Physical, Fire, Frost, Void }
    public enum CardType : byte { Strike, Incantation, Construct, Reaction }

    public struct Vector2DDepth
    {
        public int X;
        public int Z;
    }

    public class BattleEntity
    {
        public uint Id { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public Vector2DDepth Position { get; set; }
        public HashSet<ElementType> ActiveStatusTags { get; set; } = new HashSet<ElementType>();
    }

    public struct ActionInputContext
    {
        public uint CasterId;
        public uint? TargetEntityId;
        public Vector2DDepth TargetCoordinates;
        public bool IsCoOpCrossLink;
    }

    public interface ICardModule
    {
        uint CardId { get; }
        int ManaCost { get; }
        void ApplyCatalyst(BattleEntity caster, ActionInputContext ctx, List<EntityMutation> outDeltas);
        void ApplyExecutioner(BattleEntity caster, ActionInputContext ctx, List<EntityMutation> outDeltas);
    }

    public struct EntityMutation
    {
        public uint EntityId;
        public int HealthDelta;
        public Vector2DDepth NewPosition;
        public ElementType AppliedElement;
        public bool TriggeredResonance;
    }
}
```

## 11. Prototype Priorities

Build this first. Debate later.

The first implementation should focus on:

1. A deterministic turn and action pipeline.
2. Catalyst / Executioner resolution.
3. A minimal set of card types and elements.
4. A simple authoritative server loop.
5. A visual playback layer on the client.

## 12. Next Development Targets

After this brief, the next useful documents are:

- database schema for progression and RNG loot
- gRPC contract definitions for the network boundary
- class and ability trees
- example card lists for each archetype
- exact turn structure and resolution order

## 13. Name

AI gen so they all suck (arent great)

| Name       | Meaning                        | Why it fits                                  |
| ---------- | ------------------------------ | -------------------------------------------- |
| Kairos     | The perfect moment to act      | Catalyst → Executioner                       |
| Conflux    | Forces converging              | Elements, co-op, board states                |
| Kasane     | Layering/stacking              | Card interactions                            |
| Hibiki     | Echo/resonance                 | Setup and payoff                             |
| Nexus      | Connection                     | Co-op and combos                             |
| Syzygy     | Alignment                      | Perfect combo conditions                     |
| Sangam     | Convergence                    | Multiplayer and synergy                      |
| Kapocs     | Link                           | Entire game is links and triggers            |
| Praxis     | Action/execution               | Tactical gameplay                            |
| Vinculum   | Bond/chain                     | Combo architecture                           |
| Sutra      | Connecting thread              | Interwoven card interactions                 |
| Kinesis    | Movement                       | Positioning and battlefield control          |
| Harmonia   | Harmony through interaction    | Teamplay and synergies                       |
| En         | Connection, fate, relationship | Cooperative and emergent gameplay            |
| Resonantia | Resonance                      | Setup states and amplified effects           |
| Concursus  | Collision of forces            | Multiple systems meeting in one payoff       |
| Kasumi     | Mist, layered obscurity        | Layered tactics and battlefield manipulation |
| Aion       | Age, eternal cycle             | Roguelike progression and repeated runs      |
| Telos      | Purpose, end state             | Building toward a decisive payoff            |
| Logos      | Order, underlying structure    | Deterministic tactical systems               |
