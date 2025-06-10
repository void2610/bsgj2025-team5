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

# Build from command line (Unity 6000.0.42f1)
/Applications/Unity/Hub/Editor/6000.0.42f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "$(pwd)" \
  -buildTarget [StandaloneOSX|StandaloneWindows64|WebGL] \
  -buildOSXUniversalPlayer "Build/MyGame.app"
```

### CI/CD Pipeline
- **GitHub Actions**: Automatic WebGL builds on push/PR to main branch
- **Build Target**: WebGL with decompression fallback enabled
- **Deployment**: Auto-deploys to GitHub Pages with branch-specific URLs
- **Discord Integration**: Notifies on successful deploys

### Common Unity Shortcuts
- Play Mode: Cmd+P (Mac) / Ctrl+P (Windows)
- Pause: Cmd+Shift+P / Ctrl+Shift+P
- Console: Cmd+Shift+C / Ctrl+Shift+C

## Code Patterns

### Singleton Pattern
All manager classes inherit from `SingletonMonoBehaviour<T>` with auto-instantiation:
```csharp
public class MyManager : SingletonMonoBehaviour<MyManager>
{
    protected override void Awake()
    {
        base.Awake(); // Critical: Always call base.Awake()
        // Custom initialization here
    }
}
```

### Reactive Properties (R3)
State management with reactive streams and automatic cleanup:
```csharp
private readonly ReactiveProperty<int> _value = new(0);
public ReadOnlyReactiveProperty<int> Value => _value;

// Subscription with automatic cleanup
private void Start()
{
    GameManager.Instance.ItemCount.Subscribe(OnItemChanged).AddTo(this);
}
```

### Manager Architecture
- **GameManager**: Central state controller with `ItemCount` and `ClosestEnemyDistance` reactive properties
- **SeManager**: Audio pool system (20 AudioSources) with `PlaySe()` API and PlayerPrefs volume
- **BGMManager**: Dynamic tempo scaling based on player speed using LitMotion
- **ParticleManager**: GameObject pool for effects with `CreateParticle()` API
- **UIManager**: Pause/UI control with time scale management
- **VolumeManager**: Post-processing effects driven by speed/distance

### Physics System
Player uses torque-based ball physics:
```csharp
// Mouse input → torque application → Rigidbody physics
rigidbody.AddTorque(mouseInput * torqueMultiplier);
// Speed levels calculated from maxLinearVelocity (0-4 int levels)
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

- **Physics Configuration**: Player Rigidbody has custom max velocities (linear/angular) with tuned friction/bounciness
- **Game Rules**: Win at 5 items collected, lose on enemy contact
- **Input System**: Mouse movement drives ball torque, 'P' key toggles pause
- **Localization**: All UI text uses Unity Localization system (EN/JA) via string tables
- **Audio Architecture**: SeManager (pooled AudioSources) + BGMManager (speed-reactive) with AudioMixer groups
- **R3 Lifecycle**: Always use `.AddTo(this)` for subscriptions to prevent memory leaks
- **Addressables**: Used for localization assets with proper group configuration