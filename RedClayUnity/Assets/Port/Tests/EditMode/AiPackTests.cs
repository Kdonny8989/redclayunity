// Checks the C# crossing caution and pack logic against the web game (Tests/Data/ai-pack-expected.json): 75 scenarios on
// the figure-eight (one crossing) and two downforce tracks, with the random source fixed at 0.3 in both.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedPackTrack { public string id; public string layout; public double ar; public double miles; public double W; public string phys; public double bank; public double grip; public bool fig8; }
[Serializable] class ExpectedPackInput
{
    public double s; public double lat; public double vF; public double prog; public bool @out; public int pit; public double recover; public double spin;
    public double checkT; public bool checkArmed; public double rippleAt; public int rippleFrom; public double skill; public double maxSpeed; public double target;
    public double x; public double y; public double h; public double vx; public double vy; public bool isPlayer; public bool hired; public double accel; public double gripMul; public double tw;
}
[Serializable] class ExpectedPackOutput
{
    public double crossingCap; public double packTarget; public int packBrake; public double checkT; public bool checkArmed; public double rippleAt; public int rippleFrom; public bool desperate; public double laneNudge;
}
[Serializable] class ExpectedPackScenario { public string track; public int tier; public double t; public string stage; public ExpectedPackInput[] inputs; public ExpectedPackOutput[] outputs; }
[Serializable] class ExpectedPackFile { public ExpectedPackTrack[] tracks; public ExpectedPackScenario[] scenarios; }

public class AiPackTests
{
    const double Tol = 1e-9;
    const double WebInfinity = 1e30;

    [Test]
    public void EveryPackScenarioMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/ai-pack-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedPackFile>(File.ReadAllText(path));
        var tracks = new Dictionary<string, ExpectedPackTrack>();
        foreach (var t in file.tracks) tracks[t.id] = t;

        var failures = new List<string>();
        for (int sc = 0; sc < file.scenarios.Length; sc++)
        {
            var s = file.scenarios[sc];
            var t = tracks[s.track];
            var env = new PhysicsEnv
            {
                Track = new TrackSpace(TrackGeometry.Build(t.layout, t.ar, t.miles), t.W), Surface = t.phys,
                Road = false, Bank = t.bank, SurfGrip = t.grip, Tier = s.tier, Kart = false, Fig8 = t.fig8,
            };
            CarPhysics.SetGroove(env, 0.5);
            var ctx = new RaceContext { Time = s.t, Stage = s.stage, Laps = 8, InRace = true, Rnd = () => 0.3 };

            var cars = new List<CarState>();
            foreach (var i in s.inputs)
            {
                cars.Add(new CarState
                {
                    S = i.s, Lat = i.lat, VF = i.vF, Prog = i.prog, Out = i.@out, Pit = i.pit, Recover = i.recover, Spin = i.spin,
                    CheckT = i.checkT, CheckArmed = i.checkArmed, RippleAt = i.rippleAt, Skill = i.skill, MaxSpeed = i.maxSpeed,
                    X = i.x, Y = i.y, H = i.h, Vx = i.vx, Vy = i.vy, IsPlayer = i.isPlayer, Hired = i.hired, Accel = i.accel,
                    GripMul = i.gripMul, Tw = i.tw,
                });
            }
            for (int k = 0; k < cars.Count; k++)
            {
                if (s.inputs[k].rippleFrom >= 0) cars[k].RippleFrom = cars[s.inputs[k].rippleFrom];
            }
            for (int k = 0; k < cars.Count; k++)
            {
                var c = cars[k];
                var o = s.outputs[k];
                double cap = AiPack.CrossingCaution(c, cars, env);
                if (double.IsPositiveInfinity(cap)) cap = WebInfinity;
                double tgt = AiPack.PackTarget(c, cars, s.inputs[k].target, 1.0 / 120.0, env, ctx);
                AiPack.PackPost(c, cars, env);
                int rf = c.RippleFrom == null ? -1 : cars.IndexOf(c.RippleFrom);
                bool same = Math.Abs(cap - o.crossingCap) <= Tol * Math.Max(1, Math.Abs(o.crossingCap))
                    && Math.Abs(tgt - o.packTarget) <= Tol && c.PackBrake == o.packBrake && Math.Abs(c.CheckT - o.checkT) <= Tol
                    && c.CheckArmed == o.checkArmed && Math.Abs(c.RippleAt - o.rippleAt) <= Tol && rf == o.rippleFrom
                    && c.Desperate == o.desperate && Math.Abs(c.LaneNudge - o.laneNudge) <= Tol;
                if (!same)
                    failures.Add($"scenario {sc} car {k} ({s.track}, {s.stage}): cap {cap} vs {o.crossingCap}, target {tgt} vs {o.packTarget}, brake {c.PackBrake} vs {o.packBrake}, checkT {c.CheckT} vs {o.checkT}, nudge {c.LaneNudge} vs {o.laneNudge}");
            }
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " pack results differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
