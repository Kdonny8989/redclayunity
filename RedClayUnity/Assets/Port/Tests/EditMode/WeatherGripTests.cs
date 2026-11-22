// Checks the C# grip against the web game for every combination of surface, time of day, weather, dirt state,
// shower and drying (Tests/Data/weather-grip-expected.json).
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedGripCase { public string surface; public string tod; public string wx; public string dirt; public bool showerOn; public double drying; public double frac; public double grip; }
[Serializable] class ExpectedGripFile { public ExpectedGripCase[] cases; }

public class WeatherGripTests
{
    const double Tol = 1e-12;

    [Test]
    public void EveryGripCaseMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/weather-grip-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedGripFile>(File.ReadAllText(path));
        Assert.Greater(file.cases.Length, 0);

        var failures = new List<string>();
        foreach (var e in file.cases)
        {
            var w = new WeatherState { Tod = e.tod, Wx = e.wx, Dirt = e.dirt, ShowerOn = e.showerOn, Drying = e.drying };
            double got = WeatherGrip.Grip(e.frac, w, e.surface);
            if (Math.Abs(got - e.grip) > Tol)
                failures.Add($"{e.surface}/{e.tod}/{e.wx}/{e.dirt}/shower={e.showerOn}/drying={e.drying}/frac={e.frac}: {got} vs {e.grip}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " grip cases differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
