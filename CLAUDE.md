# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 (6000.0.42f1) 3D physics-based ball-rolling puzzle game where players control a sphere to collect 5 items while avoiding enemies. The game has a Japanese shrine aesthetic with Torii gates and temple elements.

## Key Architecture

### Core Systems
- **GameManager**: Singleton managing game state, item collection (win at 5 items), and scene transitions
- **Player**: Physics-based ball controlled via mouse input (torque application), with particle effects on collision
- **Enemy/EnemyAI**: AI-driven enemies that chase the player
- **UIManager**: Handles pause menu and UI state
- **Scene Flow**: TitleScene → MainScene/TutorialScene → ClearScene/GameOverScene

### Important Libraries
- **R3**: Reactive programming (ReactiveProperty for state management)
- **UniTask**: Async/await support
- **LitMotion**: Animation/tweening
- **Unity Localization**: Multi-language support (EN/JA)
- **Unity Input System**: Modern input handling
- **URP**: Universal Render Pipeline for rendering

## Development Commands

### Unity Editor
```bash
# Open Unity Hub (macOS)
open -n -a "Unity Hub"

# Build from command line
/Applications/Unity/Hub/Editor/6000.0.42f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "$(pwd)" \
  -buildTarget [StandaloneOSX|StandaloneWindows64|WebGL] \
  -buildOSXUniversalPlayer "Build/MyGame.app"
```

### Common Unity Shortcuts
- Play Mode: Cmd+P (Mac) / Ctrl+P (Windows)
- Pause: Cmd+Shift+P / Ctrl+Shift+P
- Console: Cmd+Shift+C / Ctrl+Shift+C

## Code Patterns

### Singleton Pattern
All manager classes inherit from `SingletonMonoBehaviour<T>`:
```csharp
public class MyManager : SingletonMonoBehaviour<MyManager>
```

### Reactive Properties (R3)
State is managed using ReactiveProperty:
```csharp
private readonly ReactiveProperty<int> _value = new(0);
public ReadOnlyReactiveProperty<int> Value => _value;
```

### Scene Management
Always use scene names (not indices):
```csharp
SceneManager.LoadScene("SceneName");
```

## Project Structure

- **Assets/Scripts/**: Organized by feature (Enemy/, Player/, System/, UI/, etc.)
- **Assets/Prefabs/**: Reusable GameObjects, organized by scene (Main/, Menu/)
- **Assets/Scenes/**: 5 main scenes (Title, Main, Tutorial, Clear, GameOver)
- **Assets/Localization/**: Language files and string tables
- **ProjectSettings/**: Unity configuration (don't modify directly)

## Testing & Building

1. **Play Testing**: Use Unity Editor play mode
2. **Build Scenes**: Already configured in EditorBuildSettings
3. **Input**: Mouse movement controls ball torque
4. **Debug**: Press 'P' key to toggle pause menu

## Important Notes

- Physics settings: Player Rigidbody has custom max velocities
- Win condition: Collect 5 items
- Lose condition: Touch enemy
- Localization: All UI text should use localized strings
- Audio: Separate BGM and SE managers with mixer groups

## Security Warnings
- Gizmo機能は絶対に使うな。