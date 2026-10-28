using System;
using System.Linq;
using NUnit.Framework;

public class PairMatchRoundTests
{
    // Cards: 0 1 2 3 with keys A B A B.
    static PairMatchRound CreateRound() => new PairMatchRound(new[] { 7, 9, 7, 9 });

    [Test]
    public void MatchingPairIsFound()
    {
        var round = CreateRound();
        Assert.AreEqual(PickResult.FirstPick, round.Pick(0, out int other));
        Assert.AreEqual(-1, other);
        Assert.AreEqual(PickResult.Match, round.Pick(2, out other));
        Assert.AreEqual(0, other);
        Assert.IsTrue(round.IsMatched(0) && round.IsMatched(2));
        Assert.AreEqual(1, round.PairsFound);
    }

    [Test]
    public void MismatchCountsMistakeAndResetsPick()
    {
        var round = CreateRound();
        round.Pick(0, out _);
        Assert.AreEqual(PickResult.Mismatch, round.Pick(1, out int other));
        Assert.AreEqual(0, other);
        Assert.AreEqual(1, round.Mistakes);
        Assert.AreEqual(-1, round.PendingPick);
        Assert.IsFalse(round.IsMatched(0));
    }

    [Test]
    public void SameCardTwiceAndMatchedCardsAreIgnored()
    {
        var round = CreateRound();
        round.Pick(0, out _);
        Assert.AreEqual(PickResult.Ignored, round.Pick(0, out _));
        round.Pick(2, out _);
        Assert.AreEqual(PickResult.Ignored, round.Pick(2, out _));
        Assert.AreEqual(PickResult.Ignored, round.Pick(99, out _));
    }

    [Test]
    public void CompletesWhenAllPairsFound()
    {
        var round = CreateRound();
        round.Pick(0, out _); round.Pick(2, out _);
        round.Pick(1, out _); round.Pick(3, out _);
        Assert.IsTrue(round.IsComplete);
        Assert.AreEqual(PickResult.Ignored, round.Pick(1, out _));
    }

    [Test]
    public void RejectsKeysThatAreNotPairs()
    {
        Assert.Throws<ArgumentException>(() => new PairMatchRound(new[] { 1, 1, 2 }));
        Assert.Throws<ArgumentException>(() => new PairMatchRound(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public void ShuffledKeysContainEachKeyTwice()
    {
        var keys = PairMatchRound.CreateShuffledKeys(6, new Random(1));
        Assert.AreEqual(12, keys.Count);
        Assert.IsTrue(keys.GroupBy(k => k).All(g => g.Count() == 2));
    }
}

public class GameRulesTests
{
    [Test]
    public void BoardGrowsEachLevelUpToCap()
    {
        var easy = GameRules.CreateEasy();
        Assert.AreEqual(3, easy.PairsForLevel(1));
        Assert.AreEqual(4, easy.PairsForLevel(2));
        Assert.AreEqual(easy.maxPairs, easy.PairsForLevel(100));
    }

    [Test]
    public void HardHasTenSecondObservation()
    {
        Assert.AreEqual(10f, GameRules.CreateHard().observationSeconds);
        Assert.IsFalse(GameRules.CreateEasy().HasObservationLimit);
    }
}

public class GameStateTests
{
    [Test]
    public void MatchesAddPointsWithStreakBonus()
    {
        var rules = GameRules.CreateEasy();
        var state = new GameState(rules, 0);
        Assert.AreEqual(rules.pointsPerMatch, state.RegisterMatch());
        Assert.AreEqual(rules.pointsPerMatch + rules.streakBonus, state.RegisterMatch());
        Assert.AreEqual(2, state.BestStreak);
    }

    [Test]
    public void MistakesCostLivesAndEndTheGame()
    {
        var rules = GameRules.CreateHard();
        var state = new GameState(rules, 1);
        bool gameOver = false;
        state.GameOver += () => gameOver = true;

        state.RegisterMatch();
        state.RegisterMistake();
        Assert.AreEqual(rules.startingLives - 1, state.Lives);
        Assert.AreEqual(0, state.Streak);
        Assert.AreEqual(rules.pointsPerMatch - rules.wrongPenalty, state.Score);

        for (int i = 0; i < rules.startingLives; i++)
            state.RegisterMistake();
        Assert.IsTrue(state.IsGameOver);
        Assert.IsTrue(gameOver);
        Assert.GreaterOrEqual(state.Score, 0);
    }

    [Test]
    public void CompletingLevelAwardsBonusAndAddsPairs()
    {
        var state = new GameState(GameRules.CreateEasy(), 0);
        int pairsBefore = state.PairsThisLevel;
        Assert.AreEqual(state.Rules.levelClearBonus, state.CompleteLevel());
        Assert.AreEqual(2, state.Level);
        Assert.Greater(state.PairsThisLevel, pairsBefore);
    }

    [Test]
    public void RecordKeepsDifficultyAndStats()
    {
        var state = new GameState(GameRules.CreateHard(), 1);
        state.RegisterMatch();
        state.RegisterMistake();
        var record = state.ToRecord(new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(1, record.difficulty);
        Assert.AreEqual(1, record.matches);
        Assert.AreEqual(1, record.mistakes);
        Assert.AreEqual(0.5f, record.Accuracy, 0.0001f);
    }
}

public class ScoreHistoryTests
{
    static ScoreRecord Record(int difficulty, int score, long ticks) =>
        new ScoreRecord { difficulty = difficulty, score = score, timestampUtcTicks = ticks, levelReached = 1 };

    [Test]
    public void TracksBestPerDifficulty()
    {
        var history = new ScoreHistory();
        Assert.IsTrue(history.Add(Record(0, 500, 1)));
        Assert.IsFalse(history.Add(Record(0, 300, 2)));
        Assert.IsTrue(history.Add(Record(1, 100, 3)));
        Assert.IsTrue(history.Add(Record(0, 900, 4)));
        Assert.AreEqual(900, history.GetBest(0).score);
        Assert.AreEqual(100, history.GetBest(1).score);
        Assert.AreEqual(3, history.GamesPlayed(0));
    }

    [Test]
    public void RecentIsNewestFirstAndTopIsHighestFirst()
    {
        var history = new ScoreHistory();
        history.Add(Record(0, 1, 10));
        history.Add(Record(1, 2, 30));
        history.Add(Record(0, 3, 20));
        CollectionAssert.AreEqual(new[] { 2, 3, 1 }, history.GetRecent(null, 10).Select(r => r.score));
        CollectionAssert.AreEqual(new[] { 3, 1 }, history.GetTop(0, 10).Select(r => r.score));
    }

    [Test]
    public void TrimsOldRecordsButKeepsBest()
    {
        var history = new ScoreHistory();
        history.Add(Record(0, 10000, 0));
        for (int i = 1; i <= ScoreHistory.MaxRecordsPerDifficulty + 10; i++)
            history.Add(Record(0, i, i));
        Assert.AreEqual(ScoreHistory.MaxRecordsPerDifficulty, history.GamesPlayed(0));
        Assert.AreEqual(10000, history.GetBest(0).score);
    }
}
