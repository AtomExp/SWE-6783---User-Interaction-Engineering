# Gameplay Systems: Matching, Score/Lives/Level, Score Dashboard

**Owner:** Allen Kim

This covers three items from the progress report: **Matching mechanics**, **Score/Lives/Level** and the **Score Dashboard**. It builds on Patrick's `MemoryBoard`/`MemoryCard` and on the team's `GameSettings`/`SceneLoader` from the `integration` branch. It doesn't add a second difficulty or scene system.

## How a game plays

The rules follow the How to Play screen:

1. **Observe:** the cards are dealt face up.
   - **Easy:** no time limit. The player presses **Start** (Patrick's start button) when ready.
   - **Hard:** a 10-second countdown, then the cards flip automatically.
2. **Select:** the player turns over two cards.
   - **Match:** points, plus a streak bonus for consecutive matches. Both cards stay face up, tinted green.
   - **No match:** the player loses a life. Both cards show for 0.8 seconds, then flip back.
3. **Level clear:** once every pair is found, the player gets a bonus (bonus × level) and a bigger board deals.
4. **Game over:** at 0 lives, the remaining cards are revealed, the score is saved, and the Game Over panel appears with **Play Again**, **Scores** and **Main Menu**.

| | Easy | Hard |
|---|---|---|
| Pairs at level 1 / per level / max | 3 / +1 / 8 | 6 / +1 / 10 |
| Observation | Until Start is pressed | 10 seconds |
| Lives | 5 | 3 |
| Points per match / streak bonus | 100 / +25 | 150 / +50 |
| Wrong pair penalty | 0 | −50 |
| Level clear bonus | 250 × level | 400 × level |

All numbers can be changed in the Inspector on **GameManager → Easy Rules / Hard Rules** without touching code. The pair count is also capped by the number of symbol sprites on `MemoryBoard` (currently 10).

## Setting it up in Unity

1. Open `GameLevel` (Patrick's board scene) and run **Think N Sync > Add Gameplay To Open Scene**, then save. This adds a `GameManager` linked to the `MemoryBoard`, a HUD (score, level, lives, timer, pairs found, message) and a Game Over panel, and turns off **Build On Start** on the board.
2. Run **Think N Sync > Create Score Dashboard Scene**.
3. Run **Think N Sync > Add All Scenes To Build Settings**. This puts every scene in `Assets/Scenes` into the build list in flow order and drops entries for deleted scenes.
4. Press Play in `GameLevel`. The difficulty comes from `GameSettings.SelectedDifficulty`, which is saved in PlayerPrefs, so the last choice from the Difficulty screen is used.

## Files

```text
Assets/Scripts/
├── Logic/      Pure C# rules, no Unity types (own assembly: ThinkNSync.Logic) — unit tested
│   ├── PairMatchRound.cs   pick two cards → Match / Mismatch
│   ├── GameRules.cs        Easy/Hard numbers and board growth
│   ├── GameState.cs        score, lives, level, streak (+ events)
│   └── ScoreHistory.cs     ScoreRecord + history (best / recent / top)
├── Gameplay/   GameManager, HUDController, GameOverPanel
├── Scores/     ScoreRepository (save/load), ScoreDashboardController, ScoreRowView
└── Editor/     GameplaySetupMenu (the Think N Sync menu items above)
Assets/Tests/EditMode/      Tests for Logic (Window > General > Test Runner > EditMode)
```

**Changes to teammates' files.** All of these are additions; existing behavior is unchanged.

- `MemoryCard.cs` (Patrick):
  - Cards are now clickable through a `Clicked` event, using the UI EventSystem.
  - Added `ShowMatched()` and `IsMatched`.
- `MemoryBoard.cs` (Patrick):
  - Added `BuildBoard(pairCount)`, which picks random symbols each time.
  - Added `Cards`, `SymbolCount`, a `Started` event and `SetStartButtonVisible()`.
  - Added a **Build On Start** toggle.
  - Columns are chosen automatically so rows come out even.
  - `InitializeBoard()` still builds the columns × rows board as before.
- `SceneLoader.cs` (`SceneNames`): added the `GameLevel`, `ScoreDashboard` and `Options` constants.
- `MainMenuController.cs`: added `OpenScoreDashboard()`.

## Connecting other screens

- **Main Menu:** point the **ScoreDashboardButton**'s OnClick at `MainMenuController.OpenScoreDashboard`.
- **Difficulty Selection:** `DifficultySelectionScreen` sets `GameSettings.SelectedDifficulty` and loads `SceneNames.StudyPhase`. **This needs a team decision:** there's no `StudyPhase` scene. The observe/study step happens on the board in `GameLevel`. Either rename `GameLevel` to `StudyPhase`, or point the difficulty buttons at `SceneNames.GameLevel`.
- **Study Phase (Kavya):** the built-in observe step uses the board's Start button and the Hard timer. A custom study UI can:
  - subscribe to `GameManager.Instance.PhaseChanged` and `ObservationTick`, and call `GameManager.Instance.EndObservation()`;
  - or change how long the cards stay up with `observationSeconds` in the rules.
- **Audio (Team):** `GameManager` has UnityEvents you can drag an `AudioSource.Play` onto: `onObservePhaseStarted`, `onSelectionPhaseStarted`, `onCardFlipped`, `onMatch`, `onMismatch`, `onLevelComplete`, `onGameOver`.
- **Options (volume sliders):** these aren't connected to audio yet. Not part of this work.

## Scripting reference

| Need | Use |
|---|---|
| Current game | `GameManager.Instance` (`State`, `Phase`, `Difficulty`, `CurrentRound`) |
| Score / lives / level changes | `GameManager.Instance.GameStarted` → `state.ScoreChanged`, `LivesChanged`, `LevelChanged` |
| Phase changes | `PhaseChanged` (`Observe`, `Selecting`, `Checking`, `LevelComplete`, `GameOver`) |
| Each pair attempt | `PairChecked` (`Match` / `Mismatch`) |
| Game end | `GameEnded` (`ScoreRecord`, `isNewBest`) |
| Saved scores | `ScoreRepository.GetBest(Difficulty.Easy)`, `GetTop(d, n)`, `GetRecent(d or null, n)` |

## Notes

- **Input:** the project only allows the Input System package, so legacy `Input.*` calls throw errors. Card clicks use `IPointerClickHandler` through the scene's EventSystem (`InputSystemUIInputModule`).
- **Saving:** scores are stored in PlayerPrefs under `ThinkNSync.ScoreHistory.v1`. Each difficulty keeps its last 50 games, and the best score is never removed.
- **Logic assembly:** `Assets/Scripts/Logic` compiles separately and can't use Unity or game types, so the tests can run without the game code. Don't put MonoBehaviours there.
