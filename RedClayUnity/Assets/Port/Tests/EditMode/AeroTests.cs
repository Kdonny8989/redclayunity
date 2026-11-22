// Checks the C# aero against the web game (Tests/Data/aero-expected.json): side-drafting, dirty air and the bump-draft
// push for 58 car setups over 8 scenarios.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedAeroInput { public double prog; public double lat; public double s; public double vF; public double draft; public double pushT; public int pit; public bool @out; public bool finished; public bool isPlayer; public bool hired; }
[Serializable] class ExpectedAeroOutput { public double dirtyAir; public double rearHit; public double draftAfter; }
[Serializable] class ExpectedAeroScenario { public ExpectedAeroInput[] inputs; public ExpectedAeroOutput[] outputs; }
[Serializable] class ExpectedAeroTrack { public string id; public string layout; public double ar; public double miles; public double W; public string phys; public double bank; public double grip; }
[Serializable] class ExpectedAeroFile { public ExpectedAeroTrack track; public int tier; public ExpectedAeroScenario[] scenarios; }

public class AeroTests
{
    const double Tol = 1e-9;

    [Test]
    public void EveryAeroScenarioMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/aero-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedAeroFile>(File.ReadAllText(path));
        var t = file.track;
        var geo = TrackGeometry.Build(t.layout, t.ar, t.miles);
        var env = new PhysicsEnv
        {
            Track = new TrackSpace(geo, t.W), Surface = t.phys, Road = t.layout == "road" || t.layout == "street",
            Bank = t.bank, SurfGrip = t.grip, Tier = file.tier, Kart = false,
        };

        var failures = new List<string>();
        for (int sc = 0; sc < file.scenarios.Length; sc++)
        {
            var sco = file.scenarios[sc];
            var cars = new List<CarState>();
            foreach (var i in sco.inputs)
            {
                cars.Add(new CarState
                {
                    Prog = i.prog, Lat = i.lat, S = i.s, VF = i.vF, Draft = i.draft, PushT = i.pushT, Pit = i.pit,
                    Out = i.@out, Finished = i.finished, IsPlayer = i.isPlayer, Hired = i.hired,
                });
            }
            Aero.Compute(cars, env);
            for (int k = 0; k < cars.Count; k++)
            {
                var o = sco.outputs[k];
                var c = cars[k];
                if (Math.Abs(c.DirtyAir - o.dirtyAir) > Tol || Math.Abs(c.RearHit - o.rearHit) > Tol || Math.Abs(c.Draft - o.draftAfter) > Tol)
                    failures.Add($"scenario {sc} car {k}: dirtyAir {c.DirtyAir} vs {o.dirtyAir}, rearHit {c.RearHit} vs {o.rearHit}, draft {c.Draft} vs {o.draftAfter}");
            }
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " aero results differ from the web game:\n" + string.Join("\n", failures));
    }
}
