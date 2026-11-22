// Checks the C# economy against the web game (Tests/Data/economy-expected.json): 120 random team setups across the six
// divisions, with the tech levels, facility levels, repairs and hospitality values.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedFx { public double speed; public double accel; public double grip; public double repair; public double slick; public double pitgain; public double dmgresist; public double tirelife; public double travel; }
[Serializable] class ExpectedEconCase
{
    public int tier; public int[] levels; public int shop; public int hospLevel; public int eng; public ExpectedFx fx;
    public double dmg; public double repairCost; public double[] techCosts; public double hospValue;
}
[Serializable] class ExpectedEconFile { public ExpectedEconCase[] cases; }

public class EconomyTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryTeamSetupMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/economy-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedEconFile>(File.ReadAllText(path));

        var failures = new List<string>();
        foreach (var e in file.cases)
        {
            var f = Economy.TeamEffects(e.tier, e.levels, e.eng);
            var w = e.fx;
            bool fxSame = Math.Abs(f.Speed - w.speed) <= Tol && Math.Abs(f.Accel - w.accel) <= Tol && Math.Abs(f.Grip - w.grip) <= Tol
                && Math.Abs(f.Repair - w.repair) <= Tol && Math.Abs(f.Slick - w.slick) <= Tol && Math.Abs(f.PitGain - w.pitgain) <= Tol
                && Math.Abs(f.DmgResist - w.dmgresist) <= Tol && Math.Abs(f.TireLife - w.tirelife) <= Tol && Math.Abs(f.Travel - w.travel) <= Tol;
            double rc = Economy.RepairCost(e.dmg, e.tier, f.Repair);
            bool costsSame = true;
            var tierItems = EconomyTables.TierTech[e.tier];
            for (int i = 0; i < tierItems.Length; i++)
                for (int l = 0; l < 3; l++)
                    if (Math.Abs(Economy.TechCost(tierItems[i].Costs[l], e.shop) - e.techCosts[i * 3 + l]) > Tol) costsSame = false;
            if (!fxSame || Math.Abs(rc - e.repairCost) > Tol || !costsSame)
                failures.Add($"tier {e.tier}: fx {f.Speed} vs {w.speed}, repair {rc} vs {e.repairCost}, tech costs match: {costsSame}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " economy results differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }

    [Test]
    public void HospitalityValueMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/economy-expected.json");
        var file = JsonUtility.FromJson<ExpectedEconFile>(File.ReadAllText(path));
        for (int k = 0; k < file.cases.Length; k++)
        {
            var e = file.cases[k];
            double v = 1234 + 77 * k;
            Assert.AreEqual(e.hospValue, Economy.Hosp(v, e.hospLevel), Tol, $"hospitality case {k}");
        }
    }
}
