# CS134 Project - Hood
Hood is a 2.5D platform game where you fight fungus creatures and maneuver through various levels.
- Recommended to use a controller, but can use keyboard

## Controls:
| Keyboard | Gamepad | Action |
|---|---|---|
| WASD | Left Stick | Move |
| Spacebar | Button South | Jump |
| S | Stick Down | Drop Though Platforms |
| Shift | Right Trigger | Dash |
| E | Start | View Health |
| Z | Button North | Interact |
| Q | Button West | Attack |

## Implemented:
- Run/Dash/Air Dash (Multi-directional)
- Jump/Double Jump
- Attack
- Enemy AI
- Level 1 (Forest)
- Health: Damage
- Interaction Script Setup (Doors, and Signs)

# Setup
- Import both TMP Essentials and Extras
Window > TextMeshPro > [Import TMP Essential Resources] and [Import TMP Examples & Extras]


## Packages Used: (Make sure these are installed)
- Input System
- Cinemachine
- Shader Graph
- URP
- 2D
- Post Processing
- TextMeshPro

## Tags: 
- Tag 0: Ground
- Tag 1: Platform
- Tag 2: Collectible
- Tag 3: Interactable
- Tag 4: Transition
- Tag 5: Cave
- Tag 6: Water
- Tag 7: Grass

## Layers:
- Builtin Layer 0: Default
- Builtin Layer 1: TransparentFX
- Builtin Layer 2: Ignore Raycast
- User Layer 3: Ground
- Builtin Layer 4: Water
- Builtin Layer 5: UI
- User Layer 6: Platform
- User Layer 7: oneWayNoDrop
- User Layer 8: MapIcon
- User Layer 9: PostProcessing
- User Layer 10: Enemy
