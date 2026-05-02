# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6000.2.6f2 project. Core gameplay code lives in `Assets/Scripts`, organized by feature: `Buildings`, `Grid`, `Missions`, `Network`, `Resources`, `Technology`, `UI`, and related managers. Unity scenes are in `Assets/Scenes`; project/package configuration is in `ProjectSettings` and `Packages`. Standalone NUnit tests currently live in `Tests`, with additional Unity-side test or debug scripts under `Assets/Scripts/Tests` and feature debug folders. Treat `Assets/FishNet` as vendored networking code unless a task explicitly targets FishNet integration.

## Build, Test, and Development Commands

Open the project with Unity Hub or Unity Editor matching `ProjectSettings/ProjectVersion.txt`.

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit
```

Runs Edit Mode tests through the Unity Test Framework. Use Play Mode tests when scene/runtime behavior is changed:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults PlayModeResults.xml -quit
```

Builds are normally produced through Unity Build Settings into `Builds/`. Do not commit generated `Library`, `Temp`, `Logs`, or IDE cache files.

## Coding Style & Naming Conventions

Use C# with Unity conventions: four-space indentation, `PascalCase` for types, methods, properties, and enum values; `camelCase` for locals and parameters. Keep `MonoBehaviour` scripts named after their main class, for example `BuildingManager.cs`. Prefer serialized private fields for Inspector wiring and keep public fields for intentional external API only. Place new code in the matching feature folder rather than expanding generic managers.

## Testing Guidelines

Tests use NUnit assertions via Unity Test Framework. Name tests as `UnitUnderTest_Condition_ExpectedResult`, matching examples such as `SaveData_SerializesToValidJSON`. Add Edit Mode tests for pure data, serialization, and manager logic; add Play Mode tests for scene, prefab, input, UI, or networking behavior. Run the relevant Unity test platform before submitting changes that touch gameplay or persistence.

## Commit & Pull Request Guidelines

Recent history uses concise imperative summaries, for example `Add coop networking and update sprites/prefabs` or `Rework top bar UI, fix tooltips, and update Chapter 1 narrative`. Keep commits focused and mention the affected feature first. Pull requests should include a short change summary, testing performed, linked task or issue, and screenshots or short clips for visible UI, scene, or asset changes. Call out migrations, save-data changes, package updates, and networking assumptions explicitly.

## Agent-Specific Instructions

UnityMCP is available for this project through `com.coplaydev.unity-mcp`. Prefer UnityMCP tools for scene, prefab, ScriptableObject, console, screenshot, and Unity Test Framework work. Start by checking editor state, wait for compilation after script edits, then inspect console errors before continuing. Preserve Unity `.meta` files with their assets. Avoid editing generated `.csproj` files unless Unity regeneration is the actual goal. When changing prefabs, scenes, ScriptableObjects, or package settings, state the Unity version used and verify the project still opens cleanly.
