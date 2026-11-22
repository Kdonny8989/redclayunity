// Checks the C# derby driver against the web game (Tests/Data/ai-derby-expected.json): 60 random arena states, with the
// random source fixed at 0.3 in both.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedDerbyTrack { public string id; public string layout; public double ar; public double miles; public double W; public string phys; public double bank; public double grip; }
[Serializable] class ExpectedDerbyInput
{
    public double x; public double y; public double h; public double vx; public double vy; public int num; public double bump; public double runT; public double backT;
    public int dtgt; public double dtgtT; public double stuckT; public double throttle; public bool @out; public bool finished;
}
[Serializable] class ExpectedDerbyOutput { public double throttle; public double brake; public double steerTarget; public double runT; public double backT; public int dtgt; public double dtgtT; public double stuckT; }
[Serializable] class ExpectedDerbyScenario { public ExpectedDerbyInput[] inputs; public ExpectedDerbyOutput[] outputs; public int playerIndex; }
[Serializable] class ExpectedDerbyFile { public ExpectedDerbyTrack track; public ExpectedDerbyScenario[] scenarios; }

public class AiDerbyTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryDerbyScenarioMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/ai-derby-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedDerbyFile>(File.ReadAllText(path));
        var t = file.track;
        var env = new PhysicsEnv
        {
            Track = new TrackSpace(TrackGeometry.Build(t.layout, t.ar, t.miles), t.W), Surface = t.phys,
            Road = false, Bank = t.bank, SurfGrip = t.grip, Tier = 0, Kart = false, Derby = true,
        };

        var failures = new List<string>();
        for (int sc = 0; sc < file.scenarios.Length; sc++)
        {
            var s = file.scenarios[sc];
            var cars = new List<CarState>();
            foreach (var i in s.inputs)
            {
                cars.Add(new CarState
                {
                    X = i.x, Y = i.y, H = i.h, Vx = i.vx, Vy = i.vy, Num = i.num, Bump = i.bump, RunT = i.runT, BackT = i.backT,
                    DtgtT = i.dtgtT, StuckT = i.stuckT, Throttle = i.throttle, Out = i.@out, Finished = i.finished,
                });
            }
            for (int k = 0; k < cars.Count; k++)
                if (s.inputs[k].dtgt >= 0) cars[k].Dtgt = cars[s.inputs[k].dtgt];
            var ctx = new RaceContext { Time = 10, Stage = "feature", InRace = true, Rnd = () => 0.3, Player = cars[s.playerIndex] };

            for (int k = 0; k < cars.Count; k++)
            {
                AiDerby.Drive(cars[k], cars, 1.0 / 120.0, env, ctx);
                var c = cars[k];
                var o = s.outputs[k];
                int dt = c.Dtgt == null ? -1 : cars.IndexOf(c.Dtgt);
                bool same = Math.Abs(c.Throttle - o.throttle) <= Tol && Math.Abs(c.Brake - o.brake) <= Tol
                    && Math.Abs(c.SteerTarget - o.steerTarget) <= Tol && Math.Abs(c.RunT - o.runT) <= Tol
                    && Math.Abs(c.BackT - o.backT) <= Tol && dt == o.dtgt && Math.Abs(c.DtgtT - o.dtgtT) <= Tol
                    && Math.Abs(c.StuckT - o.stuckT) <= Tol;
                if (!same)
                    failures.Add($"scenario {sc} car {k}: throttle {c.Throttle} vs {o.throttle}, steer {c.SteerTarget} vs {o.steerTarget}, brake {c.Brake} vs {o.brake}, target {dt} vs {o.dtgt}");
            }
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " derby decisions differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
