# renegade-core

Shared code and know-how for Renegade Games' Godot C# games. Each game links this repository
in as a git submodule at `addons/renegade_core`, so a fix made here reaches every game.

| Folder | What's in it | Needs Godot? |
|---|---|---|
| `pure/` | `AdSchedule` (every-Nth-game ads), `Scoreboard`, `GameMode`/`Difficulty`, `Synth/ImpactSynth` (synthesised impact sounds) | No: unit tested |
| `godot/Looks` | `Styles` (the five materials: looks, piece sets, sounds tuned for phone speakers) and `Materials` (shader cache), over `shaders/` | Yes |
| `godot/Play` | `Stage` (lights, reflection sky, table, shader warm-up), `SpinControl` (tap vs drag-to-spin), `GameSettings`, `ComputerTurn` (background search), `Sparkle` effects | Yes |
| `godot/Ui` | `GameHud`: title, score, status, hint, New Game, Undo (ad) or a custom ad button, Sound, mode buttons or a Level button, Style menu | Yes |
| `godot/Ads` | `IAdService`, `AdMobAds` (real ads, consent, child-directed), `PlaceholderAds` (desktop stand-in), `AdServices.Create` | Yes |
| `godot/Audio` | `SoundPool` (round-robin positional players), `ImpactBank<T>` (cached synthesised variations) | Yes |
| `godot/Dev` | `DevLog` (`--log`), `DevArgs` (flags from the command line or `user://dev_args.txt`), `BlackBox` + `AudioWatch` (an always-on flight recorder in `user://blackbox.log`) | Yes |
| `docs/` | [Release playbook](docs/RELEASE_PLAYBOOK.md), [starting a new game](docs/NEW_GAME.md), [website](docs/WEBSITE.md) | |

`AdMobAds` uses the [Poing Studios AdMob plugin](https://github.com/poingstudios/godot-admob-plugin),
which each game includes in its own `addons/admob`.

## Using it in a game

```
git submodule add https://github.com/darren-rawlings/renegade-core addons/renegade_core
```

In the game's `.csproj`, keep the library's tests and build output out of the game assembly:

```xml
<DefaultItemExcludes>$(DefaultItemExcludes);addons/renegade_core/tests/**;addons/renegade_core/**/bin/**;addons/renegade_core/**/obj/**</DefaultItemExcludes>
```

The game's CI checkout needs `submodules: true`. New games get all of this from
[renegade-game-template](https://github.com/darren-rawlings/renegade-game-template).

## Updating a game to the latest core

```
cd addons/renegade_core && git pull && cd ../..
git add addons/renegade_core && git commit -m "Update renegade-core"
```

## Tests

```
dotnet test tests/Renegade.Core.Tests
```
