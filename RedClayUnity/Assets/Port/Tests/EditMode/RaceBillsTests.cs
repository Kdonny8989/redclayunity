// Checks the C# race bills against the web game (Tests/Data/race-bills-expected.json): 200 random cases covering every
// division, the guest and team-car rules, tyre wear and stops, the wind tunnel, interest on debt and merchandise.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedBillCase
{
    public int ti; public bool mc; public bool guest; public double travelFx; public double wear; public int stops; public int splits;
    public double money; public double net; public double fans; public double lv;
    public double travel; public double tireWear; public double pitSets; public double windTunnel; public double interest; public double merch;
}
[Serializable] class ExpectedBillFile { public ExpectedBillCase[] cases; }

public class RaceBillsTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryRaceBillMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/race-bills-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedBillFile>(File.ReadAllText(path));

        var failures = new List<string>();
        foreach (var e in file.cases)
        {
            double travel = RaceBills.TravelBill(e.ti, e.mc, e.guest, e.travelFx);
            // the game only charges tyre wear in the first two divisions (src/logic.js: ti <= 1), so only those are compared
            double tw = e.ti <= 1 ? RaceBills.TireWearBill(e.ti, e.wear) : e.tireWear;
            double sets = RaceBills.PitTireSets(e.stops, e.splits);
            double tunnel = RaceBills.WindTunnelBill(e.lv);
            double interest = RaceBills.InterestOnDebt(e.money, e.net);
            double merch = RaceBills.MerchSales(e.ti, e.fans);
            bool same = Math.Abs(travel - e.travel) <= Tol && Math.Abs(tw - e.tireWear) <= Tol && Math.Abs(sets - e.pitSets) <= Tol
                && Math.Abs(tunnel - e.windTunnel) <= Tol && Math.Abs(interest - e.interest) <= Tol && Math.Abs(merch - e.merch) <= Tol;
            if (!same)
                failures.Add($"division {e.ti}: travel {travel} vs {e.travel}, tyre wear {tw} vs {e.tireWear}, interest {interest} vs {e.interest}, merch {merch} vs {e.merch}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " race bills differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
