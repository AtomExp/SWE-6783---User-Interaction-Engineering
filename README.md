# Think 'N' Sync

Think 'N' Sync is a 2D single-player puzzle game developed for SWE 783 - User Interaction Engineering Project 

## Game Overview

Players observe colors, shapes and patterns and then select the matching objects. 

The game includes two difficulty levels: 

- **Easy:** Fewer objects, no time limit for obervation and easy matches.
- **Hard:** More objects, similar-looking distractors, and a 10-second observation period.

## How to Play

- Launch the game and select Start from the main menu.
- Choose either Easy or Hard mode.
- Observe the cards during the Study Phase and remember their positions.
- Select Ready when you are prepared to begin.
- Click on two cards to reveal them.
- Find matching pairs to earn points.
- Incorrect matches reduce your remaining lives.
- Match all pairs to complete the level and progress to the next level.
- The game ends when you run out of lives.

## Controls

- **Navigate menus:** Mouse click
- **Select difficulty:** Mouse click
- **Flip cards:** Mouse click
- **Confirm readiness:** Click the Ready button.
- **Interact with buttons:** Mouse click

## Troubleshooting
- **Project does not open:** Check that Unity 6 (6000.6.3f1) is installed.
- **Scenes are not loading:** Check that all required scenes are included in Build Profiles.
- **Cards are not displaying:** Check card prefabs and their Inspector references.
- **Audio is not playing:** Check AudioSource settings and volume levels.
- **Controls are not responding:** Make sure the Game window is active.

## Team - Pixel Avengers

- Kavya Kondeti
- Allen Kim
- Patrick Li
- Yasoda Kuchampudi

## Development Environment

- **Unity:** 6 (6000.6.3f1)
- **Template:** Universal 2D
- **Programming Language:** C#

## Project Structure

```text
Assets/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
├── Sprites/
└── UI/
```

## Gameplay Systems

Matching, score/lives/level and the Score Dashboard are documented in [Docs/Gameplay-Systems.md](Docs/Gameplay-Systems.md), including how other screens connect to them.
