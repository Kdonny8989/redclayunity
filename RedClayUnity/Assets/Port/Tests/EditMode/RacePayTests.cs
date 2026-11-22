// Checks the C# pay rules against the web game (Tests/Data/race-pay-expected.json): 200 random cases of salary, associate
// pay, chief pay and the charter payment, at every division's pay scale.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedPayCase
{
    public double @base; public double scale; public int level; public double salary; public double debt; public double bonus;
    public double scaledSalary; public double hospAt; public double assocAmount; public double chiefBill; public double charterPayment;
}
[Serializable] class ExpectedPayFile { public ExpectedPayCase[] cases; }

public class RacePayTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryPayRuleMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/race-pay-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedPayFile>(File.ReadAllText(path));

        var failures = new List<string>();
        foreach (var e in file.cases)
        {
            bool same = Math.Abs(RacePay.ScaledSalary(e.@base, e.scale) - e.scaledSalary) <= Tol
                && Math.Abs(RacePay.HospAt(e.@base * 7.3, e.level) - e.hospAt) <= Tol
                && Math.Abs(RacePay.AssocAmount(e.@base, e.scale, e.level) - e.assocAmount) <= Tol
                && Math.Abs(RacePay.ChiefBill(e.salary, e.scale) - e.chiefBill) <= Tol
                && Math.Abs(RacePay.CharterPayment(e.debt) - e.charterPayment) <= Tol;
            if (!same)
                failures.Add($"base {e.@base} scale {e.scale} level {e.level}: salary {RacePay.ScaledSalary(e.@base, e.scale)} vs {e.scaledSalary}, assoc {RacePay.AssocAmount(e.@base, e.scale, e.level)} vs {e.assocAmount}, charter {RacePay.CharterPayment(e.debt)} vs {e.charterPayment}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " pay results differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
