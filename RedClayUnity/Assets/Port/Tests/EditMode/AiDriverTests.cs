// Checks the C# AI steering and corner limit against the web game (Tests/Data/ai-steer-expected.json): 80 random car states
// on a dirt and an asphalt track.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedAiTrack { public string id; public string layout; public double ar; public double miles; public double W; public string phys; public double bank; public double grip; }
[Serializable] class ExpectedAiCase
{
    public string track; public int tier; public double s; public double lat; public double x; public double y; public double h;
    public double vx; public double vy; public double r; public double beta; public double tw; public double maxSpeed; public double accel;
    public double gripMul; public double skill; public double margin; public double lane; public double look;
    public double steerTarget; public double cornerLimit;
}
[Serializable] class ExpectedAiFile { public ExpectedAiTrack[] tracks; public ExpectedAiCase[] cases; }

public class AiDriverTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryAiCaseMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/ai-steer-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedAiFile>(File.ReadAllText(path));
        var tracks = new Dictionary<string, ExpectedAiTrack>();
        foreach (var t in file.tracks) tracks[t.id] = t;

        var failures = new List<string>();
        foreach (var e in file.cases)
        {
            var t = tracks[e.track];
            var env = new PhysicsEnv
            {
                Track = new TrackSpace(TrackGeometry.Build(t.layout, t.ar, t.miles), t.W), Surface = t.phys,
                Road = t.layout == "road" || t.layout == "street", Bank = t.bank, SurfGrip = t.grip, Tier = e.tier, Kart = false,
            };
            CarPhysics.SetGroove(env, 0.5);
            var c = new CarState
            {
                S = e.s, Lat = e.lat, X = e.x, Y = e.y, H = e.h, Vx = e.vx, Vy = e.vy, R = e.r, Beta = e.beta, Tw = e.tw,
                MaxSpeed = e.maxSpeed, Accel = e.accel, GripMul = e.gripMul, Skill = e.skill, Margin = e.margin,
                VF = Math.Sqrt(e.vx * e.vx + e.vy * e.vy),
            };
            AiDriver.Steer(c, env, e.lane, e.look);
            double limit = AiDriver.CornerLimit(c, env, e.lane);
            if (Math.Abs(c.SteerTarget - e.steerTarget) > Tol || Math.Abs(limit - e.cornerLimit) > Tol * Math.Max(1, e.cornerLimit))
                failures.Add($"{e.track} s={e.s:0.0}: steer {c.SteerTarget} vs {e.steerTarget}, corner limit {limit} vs {e.cornerLimit}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " AI results differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
