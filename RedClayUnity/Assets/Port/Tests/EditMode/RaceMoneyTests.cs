// Checks the purse table against the web game (Tests/Data/purse-expected.json) for every division and position, and
// checks the sponsor rules as endRace in src/logic.js states them. The sponsor rules cannot be run in the web game on
// their own, so they are checked by hand-worked examples, not by comparison.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedPurseTier { public int index; public double winPurse; public double[] purse; }
[Serializable] class ExpectedPurseFile { public ExpectedPurseTier[] tiers; }

public class RaceMoneyTests
{
    [Test]
    public void EveryPurseMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/purse-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedPurseFile>(File.ReadAllText(path));
        foreach (var t in file.tiers)
        {
            Assert.AreEqual(t.winPurse, Divisions.WinPurse[t.index], 1e-9, "win purse, division " + t.index);
            for (int p = 1; p <= t.purse.Length; p++)
                Assert.AreEqual(t.purse[p - 1], RaceMoney.PurseFor(t.index, p), 1e-9, $"purse, division {t.index}, position {p}");
        }
    }

    [Test]
    public void SponsorPaysAndBonusesFollowEndRace()
    {
        // goal: finish in the top 3, no rival. Met at 3rd: pay and bonus, misses reset.
        var met = RaceMoney.SponsorResult(100, 50, 2, 1, 3, 3, 0, false, 0);
        Assert.IsTrue(met.Met); Assert.AreEqual(100, met.Pay, 1e-9); Assert.AreEqual(50, met.Bonus, 1e-9); Assert.AreEqual(0, met.Misses);
        // 4th: not met. Pay still comes, no bonus, one more miss; patience 2 with one earlier miss drops them.
        var missed = RaceMoney.SponsorResult(100, 50, 2, 1, 4, 3, 0, false, 0);
        Assert.IsFalse(missed.Met); Assert.AreEqual(100, missed.Pay, 1e-9); Assert.AreEqual(0, missed.Bonus, 1e-9);
        Assert.AreEqual(2, missed.Misses); Assert.IsTrue(missed.Dropped);
        // beat a rival who finished 5th: finishing 4th beats them and is met.
        var beat = RaceMoney.SponsorResult(100, 50, 3, 0, 4, 0, 5, true, 0);
        Assert.IsTrue(beat.Met);
        // the rival is not in the race (rival position 0): counts as beaten
        var noRival = RaceMoney.SponsorResult(100, 50, 3, 0, 7, 0, 0, true, 0);
        Assert.IsTrue(noRival.Met);
        // hospitality suite level 2: pay and bonus up 30%
        var hosp = RaceMoney.SponsorResult(100, 50, 3, 0, 1, 1, 0, false, 2);
        Assert.AreEqual(130, hosp.Pay, 1e-9); Assert.AreEqual(70, hosp.Bonus, 1e-9);   // 65 rounds up to 70, as the game rounds it
    }
}
