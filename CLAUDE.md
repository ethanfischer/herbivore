# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Herbivore is a Godot 4.6 C# game where players manage a pack of creatures, exploring the world and identifying other packs as friend or foe. Correct identification recruits members; wrong guesses lose pack members.

## Development Commands

```bash
godot --editor project.godot   # Open in editor
godot project.godot            # Run the game
dotnet build                   # Build C# solution
```

## Technical Configuration

- **Engine**: Godot 4.6, Mobile renderer, Jolt Physics
- **Language**: C# (.NET 8.0), nullable enabled
- **Root Namespace**: `Herbivore`
- **Main Scene**: `res://Scenes/launch.tscn`

## Architecture

### Game States (GameState enum)
`Traversal` → `Testing` → back to `Traversal` (or `GameOver`/`GameWon`)

Win condition: Pack size reaches 10. Lose condition: Pack size drops to 0.

### Key Systems

**GameManager (Autoload Singleton)**: Central state manager with signals `StateChanged`, `PackSizeChanged`, `ScoreChanged`. Tracks player pack, calculates test difficulty based on pack size ratio.

**Main.cs**: Scene orchestrator. Spawns NPC packs within world radius, handles recruitment, manages UI, triggers state transitions when player approaches an NPC pack.

**Scene Structure**:
- `launch.tscn`: Main scene containing traversal mode, UI, and the intro screen (`UI/Start` node)
- `encounter.tscn`: Invoked when player approaches an NPC pack
- `TestModeOverlay.tscn`: Used during encounters for the mask/reveal mechanic

**Traversal System** (`Scripts/Traversal/`):
- `PlayerDot.cs`: Player-controlled CharacterBody2D (WASD/arrows)
- `NPCPack.cs`: Spawns 2-6 members, randomly herbivore or carnivore majority
- `PackMember.cs`: Chain-following behavior, sprite changes on recruitment

**Test Mode System** (`Scripts/TestMode/`):
- `TestModeController.cs`: 8x8 mask grid revealing NPC face
- Click count limited by pack size ratio formula: `5 + (PlayerPackSize/NpcPackSize * 3)`, clamped 3-25
- Player guesses Friend/Foe after clicks exhausted

**SoundGenerator (Static Class)**: Procedural 16-bit WAV generation for game sounds. Not an autoload; called directly via static methods.

### Namespace Structure
```
Herbivore.Autoloads   # GameManager singleton, SoundGenerator static utility
Herbivore.Data        # Enums (GameState, DotType)
Herbivore.TestMode    # Test mode UI/logic
Herbivore.Traversal   # Movement, packs, camera
```

### Signal-Based Communication
The codebase uses C# events and Godot signals extensively. GameManager emits state changes; NPCPack emits `PlayerApproached`; Main.cs subscribes and orchestrates responses.
