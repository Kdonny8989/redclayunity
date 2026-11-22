// Checks the C# AI decision, with traffic, against the web game (Tests/Data/ai-traffic-expected.json): 60 scenarios of
// 2 to 6 cars on a dirt oval and a dirt track, with the random source fixed at 0.3 in both.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedTrafficTrack { public string id; public string layout; public double ar; public double miles; public double W; public string phys; public double bank; public double grip; }
[Serializable] class ExpectedTrafficInput
{
    public double s; public double lat; public double x; public double y; public double h; public double vx; public double vy; public double vF;
    public double beta; public double r; public bool @out; public int pit; public double recover; public double stuckT; public double dodgeT; public double dodgeLane;
    public bool team; public bool grudge; public double maxSpeed; public double accel; public double skill; public double follow; public double twitch; public double tw; public double gripMul;
}
[Serializable] class ExpectedTrafficOutput { public double steerTarget; public double throttle; public double brake; public double dodgeT; public double dodgeLane; public double stuckT; public double recover; }
[Serializable] class ExpectedTrafficScenario { public string track; public int tier; public ExpectedTrafficInput[] inputs; public ExpectedTrafficOutput[] outputs; }
[Serializable] class ExpectedTrafficFile { public ExpectedTrafficTrack[] tracks; public ExpectedTrafficScenario[] scenarios; }

public class AiTrafficTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryTrafficScenarioMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/ai-traffic-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedTrafficFile>(File.ReadAllText(path));
        var tracks = new Dictionary<string, ExpectedTrafficTrack>();
        foreach (var t in file.tracks) tracks[t.id] = t;

        var failures = new List<string>();
        for (int sc = 0; sc < file.scenarios.Length; sc++)
        {
            var s = file.scenarios[sc];
            var t = tracks[s.track];
            var env = new PhysicsEnv
            {
                Track = new TrackSpace(TrackGeometry.Build(t.layout, t.ar, t.miles), t.W), Surface = t.phys,
                Road = false, Bank = t.bank, SurfGrip = t.grip, Tier = s.tier, Kart = false,
            };
            CarPhysics.SetGroove(env, 0.5);

            var cars = new List<CarState>();
            foreach (var i in s.inputs)
            {
                cars.Add(new CarState
                {
                    S = i.s, Lat = i.lat, X = i.x, Y = i.y, H = i.h, Vx = i.vx, Vy = i.vy, VF = i.vF, Beta = i.beta, R = i.r,
                    Out = i.@out, Pit = i.pit, Recover = i.recover, StuckT = i.stuckT, DodgeT = i.dodgeT, DodgeLane = i.dodgeLane,
                    Team = i.team, Grudge = i.grudge, MaxSpeed = i.maxSpeed, Accel = i.accel, Skill = i.skill,
                    Follow = i.follow, Twitch = i.twitch, Tw = i.tw, GripMul = i.gripMul,
                    Prog = 0, LastS = i.s,
                });
            }
            for (int k = 0; k < cars.Count; k++)
            {
                AiTraffic.Drive(cars[k], cars, 1.0 / 120.0, env, () => 0.3, 10, true);
                var o = s.outputs[k];
                var c = cars[k];
                bool same = Math.Abs(c.SteerTarget - o.steerTarget) <= Tol && Math.Abs(c.Throttle - o.throttle) <= Tol
                    && Math.Abs(c.Brake - o.brake) <= Tol && Math.Abs(c.DodgeT - o.dodgeT) <= Tol && Math.Abs(c.DodgeLane - o.dodgeLane) <= Tol
                    && Math.Abs(c.StuckT - o.stuckT) <= Tol && Math.Abs(c.Recover - o.recover) <= Tol;
                if (!same)
                    failures.Add($"scenario {sc} car {k} ({s.track}): steer {c.SteerTarget} vs {o.steerTarget}, throttle {c.Throttle} vs {o.throttle}, brake {c.Brake} vs {o.brake}, dodgeT {c.DodgeT} vs {o.dodgeT}, dodgeLane {c.DodgeLane} vs {o.dodgeLane}");
            }
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " traffic decisions differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
