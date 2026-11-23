// Checks the C# collision against the web game (Tests/Data/collide-expected.json): 300 random pairs of cars, the positions
// and velocities after contact, and every impact reported.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedCollideCar { public double x; public double y; public double h; public double vx; public double vy; public double r; public int pit; public bool @out; public int recover; public double Iz; }
[Serializable] class ExpectedCollideInput { public ExpectedCollideCar a; public ExpectedCollideCar b; }
[Serializable] class ExpectedCollideState { public double x; public double y; public double vx; public double vy; public double r; public double bump; }
[Serializable] class ExpectedCollideOut { public ExpectedCollideState a; public ExpectedCollideState b; }
[Serializable] class ExpectedImpact { public double raw; public double cx; public double cy; }
[Serializable] class ExpectedCollideCase { public ExpectedCollideInput input; public ExpectedCollideOut @out; public ExpectedImpact[] impacts; }
[Serializable] class ExpectedCollideFile { public ExpectedCollideCase[] cases; }

public class CollisionTests
{
    const double Tol = 1e-7;

    static CarState Make(ExpectedCollideCar e) => new CarState
    {
        X = e.x, Y = e.y, H = e.h, Vx = e.vx, Vy = e.vy, R = e.r, Pit = e.pit, Out = e.@out, Recover = e.recover,
        Spec = new CarSpec { Iz = e.Iz },
    };

    [Test]
    public void EveryCollisionMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/collide-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedCollideFile>(File.ReadAllText(path));

        var failures = new List<string>();
        for (int k = 0; k < file.cases.Length; k++)
        {
            var e = file.cases[k];
            var a = Make(e.input.a);
            var b = Make(e.input.b);
            var impacts = new List<double[]>();
            Collision.Collide(a, b, (p, q, raw, cx, cy) => impacts.Add(new[] { raw, cx, cy }));

            bool same = Near(a.X, e.@out.a.x) && Near(a.Y, e.@out.a.y) && Near(a.Vx, e.@out.a.vx) && Near(a.Vy, e.@out.a.vy) && Near(a.R, e.@out.a.r) && Near(a.Bump, e.@out.a.bump)
                && Near(b.X, e.@out.b.x) && Near(b.Y, e.@out.b.y) && Near(b.Vx, e.@out.b.vx) && Near(b.Vy, e.@out.b.vy) && Near(b.R, e.@out.b.r) && Near(b.Bump, e.@out.b.bump)
                && impacts.Count == e.impacts.Length;
            if (same)
                for (int i = 0; i < impacts.Count; i++)
                    if (!Near(impacts[i][0], e.impacts[i].raw) || !Near(impacts[i][1], e.impacts[i].cx) || !Near(impacts[i][2], e.impacts[i].cy)) same = false;
            if (!same) failures.Add($"case {k}: a ({a.X},{a.Y}) vs ({e.@out.a.x},{e.@out.a.y}), impacts {impacts.Count} vs {e.impacts.Length}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " collisions differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }

    static bool Near(double got, double want) => Math.Abs(got - want) <= Tol * Math.Max(1, Math.Abs(want));
}
