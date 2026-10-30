# Unity Setup: Matching, Score/Lives/Level, Score Dashboard

How to add `ThinkNSync_Allen_Gameplay.zip` to the project and get it running in Unity 6 (6000.6.3f1).

## 1. Get the right base branch (Git)

The code builds on Patrick's board (`MemoryBoard`, the card prefab and the symbol art), so merge his branch into `integration` first.

```bash
git fetch origin
git checkout -b allen/matching-score-dashboard origin/integration
git merge "origin/Patrick's-Branch"
```

## 2. Unzip into the project

Close Unity first. Then unzip at the **project root**, the folder that contains `Assets/`, `Packages/` and `ProjectSettings/`. Let it replace existing files.

```bash
unzip -o ~/Downloads/ThinkNSync_Allen_Gameplay.zip -d .
```

On Windows: right-click the zip, choose **Extract All**, select the project root folder, and replace files when asked.

> Keep the `.meta` files. Unity uses them to link scripts to scenes and prefabs.

## 3. Folder structure after unzipping

```text
<project root>/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameSettings.cs          (team: Difficulty enum, selected difficulty)
│   │   │   └── SceneLoader.cs           (team; + GameLevel/ScoreDashboard/Options names)
│   │   ├── Logic/                        NEW: rules only, own assembly, unit tested
│   │   │   ├── ThinkNSync.Logic.asmdef
│   │   │   ├── PairMatchRound.cs        pick two cards → Match / Mismatch
│   │   │   ├── GameRules.cs             Easy/Hard numbers
│   │   │   ├── GameState.cs             score, lives, level, streak
│   │   │   └── ScoreHistory.cs          saved results, best/recent/top
│   │   ├── Gameplay/                     NEW
│   │   │   ├── GameManager.cs           runs the game on the MemoryBoard
│   │   │   ├── HUDController.cs         score/lives/level/timer display
│   │   │   └── GameOverPanel.cs         end-of-game panel
│   │   ├── Scores/                       NEW
│   │   │   ├── ScoreRepository.cs       save/load scores
│   │   │   ├── ScoreDashboardController.cs
│   │   │   └── ScoreRowView.cs
│   │   ├── Editor/
│   │   │   ├── MenuScreensBuilder.cs    (team)
│   │   │   ├── UIArtGenerator.cs        (team)
│   │   │   └── GameplaySetupMenu.cs     NEW: "Think N Sync" menu items
│   │   ├── UI/                           (team)
│   │   ├── MemoryBoard.cs               (Patrick; + level support)
│   │   ├── MemoryCard.cs                (Patrick; + click and matched state)
│   │   └── MainMenuController.cs        (team; + OpenScoreDashboard)
│   ├── Tests/
│   │   └── EditMode/                     NEW
│   │       ├── ThinkNSync.Tests.EditMode.asmdef
│   │       └── GameLogicTests.cs
│   ├── Scenes/
│   │   ├── MainMenu.unity, DifficultySelection.unity, Instructions.unity, Options.unity
│   │   ├── GameLevel.unity              (Patrick's board scene)
│   │   └── ScoreDashboard.unity         created in step 6
│   ├── Prefabs/MemoryCard.prefab        (Patrick)
│   └── Sprites/Game Symbols/            (Patrick: card symbols)
└── Docs/
    ├── Gameplay-Systems.md              rules, scripting reference
    └── Unity-Setup.md                   this file
```

## 4. Open the project and check it compiles

1. Open the project in **Unity Hub** with Unity **6000.6.3f1**, and wait for scripts to import.
2. Open **Window > General > Console**. There should be **no red errors**. If there are, stop and fix them first; the menus below only appear once everything compiles.
3. Optional: open **Window > General > Test Runner**, choose the **EditMode** tab, and click **Run All**. All 15 tests should pass.

## 5. Add the gameplay to the board scene

1. Open `Assets/Scenes/GameLevel.unity`.
2. In the top menu bar, choose **Think N Sync > Add Gameplay To Open Scene**. This adds:
   - a **GameManager** linked to Patrick's **BoardManager** (MemoryBoard)
   - **Gameplay UI > HUD**: score, level, difficulty, lives, a message, the Hard timer and pairs found
   - **Gameplay UI > Game Over**: a hidden panel with Play Again, Scores and Main Menu buttons
   - it also unticks **Build On Start** on the MemoryBoard, because GameManager deals each level itself
3. Save the scene (**Ctrl/Cmd + S**).
4. Optional: select **GameManager** and adjust the **Easy Rules** / **Hard Rules** numbers in the Inspector.

## 6. Create the Score Dashboard scene

1. Choose **Think N Sync > Create Score Dashboard Scene**.
2. This creates and saves `Assets/Scenes/ScoreDashboard.unity`.

## 7. Add every scene to the build list

1. Choose **Think N Sync > Add All Scenes To Build Settings**.
2. Check it under **File > Build Profiles > Scene List**. `MainMenu` should be first, followed by `DifficultySelection`, `Instructions`, `GameLevel`, `ScoreDashboard` and `Options`.

Loading a scene that isn't in this list only logs a warning, and nothing happens.

## 8. Hook up the menu buttons

These are one-time Inspector edits.

1. **Main Menu → Score Dashboard:** in `MainMenu.unity`, select **ScoreDashboardButton**. In **Button > On Click ()**, click **+**, drag in the **MainMenuController** object, and pick **MainMenuController > OpenScoreDashboard**.
2. **Difficulty → game:** the Easy/Hard buttons currently try to load a `StudyPhase` scene that doesn't exist. Agree with the team on one of these:
   - rename `GameLevel.unity` to `StudyPhase.unity`, or
   - in `Assets/Scripts/UI/DifficultySelectionScreen.cs`, change `SceneNames.StudyPhase` to `SceneNames.GameLevel`.

   Also make sure the `DifficultySelection` scene's Easy/Hard/Back buttons are actually wired up. Kavya's version has no Easy/Hard OnClick yet; `DifficultySelectionScreen` on the Canvas does it.

## 9. Play test

1. Open `GameLevel` and press **Play**. It uses the last difficulty chosen, Easy by default.
2. **Easy:** the cards stay face up until you press **Start**. Then click two cards at a time.
3. **Hard:** pick Hard on the Difficulty screen, or temporarily set `GameSettings.SelectedDifficulty`. A 10-second timer runs, then the cards flip.
4. Lose all your lives to see the Game Over panel. Press **Scores** to open the dashboard and check that your game is listed.

To wipe saved scores while testing, use **Clear History** on the dashboard (tap it twice).

## 10. Commit

```bash
git add Assets Docs README.md ProjectSettings/EditorBuildSettings.asset
git commit -m "Add matching, score/lives/level and score dashboard"
git push -u origin allen/matching-score-dashboard
```

Then open a pull request on GitHub **into `integration`**.

## Troubleshooting

| Problem | Fix |
|---|---|
| No **Think N Sync** menu | There are compile errors. Check the Console. |
| Cards don't respond to clicks | The scene needs an **EventSystem** with **InputSystemUIInputModule**. GameLevel already has one. |
| "Not enough symbols" | Add more sprites to **MemoryBoard > Symbols**, or lower **Max Pairs** in GameManager's rules. |
| Board shows 20 cards, then changes | **Build On Start** is still ticked on MemoryBoard. Untick it (step 5 does this). |
| "Scene can't be loaded" warning | Run step 7 again. |
