# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Documentation Policy

`README.md` is a **living document** holding the current game vision, story, and gameplay/systems design. Whenever a change in this repo alters scope, story, mechanics, or systems described there — new/changed worlds, enemies, progression systems, controls, etc. — update `README.md` in the same session/commit as the change. Don't let it drift out of date; treat it as required upkeep, not optional polish.

## Project Overview

SurveHive is a Unity project targeting mobile platforms, built on Unity 6000.5.2f1 with the Universal Render Pipeline (2D). The project is in an early/greenfield state — currently just the default 2D URP template (sample scene, input actions, renderer/volume assets) with no gameplay scripts yet.

Key packages in use: URP 2D (`com.unity.render-pipelines.universal`), the new Input System (`com.unity.inputsystem`), 2D Animation/Aseprite/PSD/Tilemap/SpriteShape packages, Timeline, Visual Scripting, and `com.unity.test-framework` (Unity Test Framework / NUnit) for tests.

## Commands

There is no CLI build/lint/test pipeline yet (no build scripts, no CI config). Code lives in three assemblies: `SurveHive.Runtime` (Assets/Scripts), `SurveHive.Editor` (Assets/Editor), and `SurveHive.BuildTools` (Assets/Editor/BuildTools). Development happens through the Unity Editor:

- **Open project**: open the repo root in Unity Hub / Unity Editor 6000.5.2f1 (must match `ProjectSettings/ProjectVersion.txt`).
- **Run tests**: Unity Test Framework is installed but no test assemblies/tests exist yet. Once added, run via `Window > General > Test Runner` in-editor, or headlessly with:
  `Unity -runTests -batchmode -projectPath . -testResults results.xml -testPlatform <EditMode|PlayMode>`
- **Build**: no build scripts exist yet; builds are done via `File > Build Profiles` in-editor, or `Unity -batchmode -quit -projectPath . -executeMethod <BuildScript.Method>` once a build script is added.
- IDE support is configured for both Rider (`com.unity.ide.rider`) and Visual Studio (`com.unity.ide.visualstudio`); `SurveHive.slnx` is the generated solution file — regenerate via the Editor rather than hand-editing.

## Repository Structure

- `Assets/` — all game content: `Scripts/` (single `SurveHive.Runtime` assembly), `Scenes/`, `Prefabs/`, `Data/` (ScriptableObject assets), `Sprites/`, `Audio/`, `Settings/` (URP renderer, volume profile, input actions), and `Editor/BuildTools/` (scene builder/validator tooling).
- `Assets/ThirdParty/` — Unity Store packs only (PixelFantasy monsters, `SpriteEffects/` VFX sheets, `PixelUI/` button kit, `Fonts/BoldPixels/`, `IconsTemp/` placeholder glyphs). New store packs go here; our own content stays in the sibling `Assets/` folders and references pack assets rather than moving/editing them.
- `Packages/manifest.json` — package dependencies; edit through the Package Manager rather than by hand when possible.
- `ProjectSettings/` — serialized Unity project configuration (physics, quality, tags, input, render pipeline, etc.). These are YAML assets checked into git and change often as project settings are tuned in-editor — review diffs carefully, they are not hand-authored.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` — Unity-generated caches/local state, not source of truth, and not meaningful to edit directly.

## C# Coding Standards (mobile-optimized Unity code)

This project targets mobile, so all gameplay C# must be written for zero-allocation hot paths, strict encapsulation, and safe Unity object handling. Apply these rules to all new/edited scripts:

**MonoBehaviour lifecycle & architecture**
- Never instantiate a `MonoBehaviour` with `new`.
- Cache component references in `Awake()`, not `Start()`.
- Never call `GetComponent<T>()`, `GameObject.Find()`, `FindObjectOfType<T>()`, or `Camera.main` inside `Update()`/`FixedUpdate()`/`LateUpdate()` — cache them once instead.
- Always unsubscribe from C# events/`Action`s/`UnityEvent`s in `OnDisable()` or `OnDestroy()`.
- Use `[RequireComponent(typeof(...))]` when a script strictly depends on a sibling component.

**Mobile performance / zero-GC**
- No GC allocations inside `Update()`, loops, or other frequently-called methods.
- No `System.Linq` in runtime game logic.
- No string concatenation (`+`) inside `Update()` — use `StringBuilder` or cached/precomputed strings.
- Use `CompareTag("X")` instead of `gameObject.tag == "X"`.
- Use `sqrMagnitude` instead of `Vector3.Distance()` for distance checks in loops.

**Encapsulation & serialization**
- Fields are `private` by default; expose to the Inspector via `[SerializeField] private`, never by making fields `public` just for visibility.
- Prefix private instance fields with `_` (e.g. `_moveSpeed`, `_rigidBody`).

**Safety & syntax**
- Prefer `TryGetComponent<T>(out var component)` over `GetComponent<T>() != null`.
- Never use `??` or `?.` on `UnityEngine.Object`-derived references (GameObjects, Components, Transforms) — Unity's fake-null (`==` override tied to native destruction) breaks with C#'s null-coalescing/conditional operators. Use explicit `== null` / `!= null` checks instead.

**SOLID & general design**
- Single Responsibility: keep each `MonoBehaviour`/class focused on one job (e.g. separate input handling, movement, health, and UI into distinct scripts rather than one monolithic controller).
- Open/Closed: favor composition and interfaces over large switch/if-chains on type; extend behavior by adding new components/classes, not by editing unrelated logic in existing ones.
- Liskov Substitution: subclasses and interface implementations must be usable wherever their base/interface is expected, without surprising behavior changes.
- Interface Segregation: define small, focused interfaces (e.g. `IDamageable`, `IInteractable`) rather than large multi-purpose ones.
- Dependency Inversion: depend on interfaces/abstractions for cross-system references (e.g. inject an `IInputService` or `IAudioService`) rather than hard-coding concrete types, so systems stay testable and swappable.
- Favor composition over inheritance for gameplay behaviors; keep inheritance hierarchies shallow.
- Keep methods small and named for what they do; avoid duplicated logic (DRY) by extracting shared behavior into helpers/services rather than copy-pasting across scripts.
