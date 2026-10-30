using System;

// Tunable numbers for one difficulty. Shown in the GameManager Inspector (Easy Rules / Hard Rules).
[Serializable]
public class GameRules
{
    // Board size: pairs on level 1, extra pairs each level, and the cap
    // (also capped by how many symbol sprites MemoryBoard has).
    public int startingPairs = 3;
    public int pairsAddedPerLevel = 1;
    public int maxPairs = 8;

    // Seconds the cards stay face up before hiding. 0 = unlimited (the player presses Start).
    public float observationSeconds = 0f;

    public int startingLives = 5;
    public int pointsPerMatch = 100;
    // Extra points for each match in a row: 2nd match +streakBonus, 3rd +2x, ...
    public int streakBonus = 25;
    // Awarded when a board is cleared, multiplied by the level number.
    public int levelClearBonus = 250;
    public int wrongPenalty = 0;

    public bool HasObservationLimit => observationSeconds > 0f;

    public int PairsForLevel(int level)
    {
        int pairs = startingPairs + Math.Max(0, level - 1) * Math.Max(0, pairsAddedPerLevel);
        return Math.Max(1, Math.Min(pairs, Math.Max(startingPairs, maxPairs)));
    }

    public GameRules Clone() => (GameRules)MemberwiseClone();

    public static GameRules CreateEasy() => new GameRules
    {
        startingPairs = 3,
        pairsAddedPerLevel = 1,
        maxPairs = 8,
        observationSeconds = 0f,
        startingLives = 5,
        pointsPerMatch = 100,
        streakBonus = 25,
        levelClearBonus = 250,
        wrongPenalty = 0
    };

    // Matches GameSettings.HardObservationSeconds (10 s).
    public static GameRules CreateHard() => new GameRules
    {
        startingPairs = 6,
        pairsAddedPerLevel = 1,
        maxPairs = 10,
        observationSeconds = 10f,
        startingLives = 3,
        pointsPerMatch = 150,
        streakBonus = 50,
        levelClearBonus = 400,
        wrongPenalty = 50
    };
}
