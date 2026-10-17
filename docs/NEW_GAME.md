# Starting a new game

Every game gets its own repository, made from
[renegade-game-template](https://github.com/darren-rawlings/renegade-game-template).
Game folders live side by side in `C:\GameDev\Godot and VS Code\` (no `&` anywhere in the path).

```
cd "C:\GameDev\Godot and VS Code"
gh repo create darren-rawlings/<Repo> --public --template darren-rawlings/renegade-game-template --clone
cd <Repo>
git submodule update --init
powershell -File tools/New-Game.ps1 -Title "<Shown Name>" -CodeName <PascalName> -PackageId <lowercase>
git add -A && git commit -m "Name the game" && git push
```

- **Title** can change any time (it's just text). **PackageId** becomes permanent at the first
  Play upload, so settle the name first (see the release playbook, step 1).
- Check the title for trademarks: use the generic name of a classic game (Reversi, not Othello;
  Sea Battle, not Battleship; Code Breaker, not Mastermind).

## Then

1. Write `docs/DESIGN.md`: rules, the feel, the one signature effect, modes, ads, v1 scope.
2. Rules in `src/Core` (plain C#, unit tested), the 3D game in `src/Game`.
   Every game has, from the template: the top row (Style, Demo, Start over, Settings), Settings
   with Sound and Freeze the board (add the game's own rows with `AddToggle`/`AddChoice`, kept
   in `GameSettings.Extra`), Start over (against the computer: a resignation unless an ad is
   watched, `AskRestart`), the winner's fire (`Flames.SetAlight`), and titles in normal case.
3. Tutorial: pages in `src/Game/Tutorial.cs`; a scripted scene per clip in `Main.PlayClip`
   (`--clip=name` hides the HUD, plays it, quits); record them all with
   `powershell -File addons/renegade_core/tools/Record-Clips.ps1 name1 name2`, then open the
   project once (or `Godot --headless --import`) and commit `tutorial/*.ogv` with their `.uid`
   files. Check pages with `--tutorial=N` and Settings with `--settings`.
4. Icon: edit `assets/icon/*.svg`, run `tools/Render-Icons.ps1`.
5. Test on the phone: `mkdir build/android`, export, `adb install -r`.
6. Release: [RELEASE_PLAYBOOK.md](RELEASE_PLAYBOOK.md). Website card: [WEBSITE.md](WEBSITE.md).

## Studio repositories

| Repository | Role |
|---|---|
| renegade-core | Shared code and these docs (submodule in every game) |
| renegade-game-template | Starting point for each game |
| renegade-games-site | renegade-games.com |
| One per game | ConnectFour (Counter Fire), Mancala, Reversi, ... |
