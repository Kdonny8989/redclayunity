// Checks the fan buzz, the fan upkeep and the engine-claim rule against the web game (Tests/Data/fans-claims-expected.json).
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedFanCase { public double fans; public double n; public double after; public double upkeep; }
[Serializable] class ExpectedClaimCase { public int ti; public int pos; public double roll; public bool claimed; }
[Serializable] class ExpectedFansFile { public ExpectedFanCase[] fans; public ExpectedClaimCase[] claims; }

public class RaceFansTests
{
    [Test]
    public void EveryFanAndClaimRuleMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/fans-claims-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedFansFile>(File.ReadAllText(path));

        var failures = new List<string>();
        foreach (var f in file.fans)
        {
            double after = RaceFans.FansAfter(f.fans, f.n);
            if (after != f.after) failures.Add($"fans {f.fans} + {f.n}: {after} vs {f.after}");
            if (f.fans > 50 && RaceFans.FansUpkeep(f.fans) != f.upkeep) failures.Add($"upkeep at {f.fans}: {RaceFans.FansUpkeep(f.fans)} vs {f.upkeep}");
        }
        foreach (var c in file.claims)
            if (RaceFans.EngineClaimed(c.ti, c.pos, c.roll) != c.claimed) failures.Add($"claim ti {c.ti} pos {c.pos} roll {c.roll}: {RaceFans.EngineClaimed(c.ti, c.pos, c.roll)} vs {c.claimed}");

        Assert.AreEqual(0, failures.Count, failures.Count + " fan or claim results differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
