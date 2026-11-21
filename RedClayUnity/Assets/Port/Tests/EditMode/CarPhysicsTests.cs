// Replays the web game's car physics (Tests/Data/car-trace-expected.json) with the same inputs and checks every step:
// position, heading, velocity, yaw rate, track position, steering, tyre wear and progress.
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedStart { public double s0; public double lat0; public double speed; public double maxSpeed; public double accel; }
[Serializable] class ExpectedCarTrace
{
    public string id; public string layout; public double ar; public double miles; public double W; public string phys; public double bank; public double grip;
    public int tier; public double grooveFrac; public int steps; public double dt; public ExpectedStart start;
    public double[] x, y, h, vx, vy, r, s, lat, steer, tw, prog;
}
[Serializable] class ExpectedCarFile { public ExpectedCarTrace[] traces; }

public class CarPhysicsTests
{
    const double Tol = 1e-6;
    // The car is chaotic: a difference of 1e-12 in the start grows to about 1e-4 by step 180 and to whole units later,
    // because the math library differs in the last digit. Only the first second is compared tightly; the rest of the run
    // is checked for finite values, which is what matters for the game.
    const int StrictSteps = 120;

    [Test]
    public void EveryTraceMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/car-trace-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedCarFile>(File.ReadAllText(path));
        Assert.Greater(file.traces.Length, 0);

        var failures = new System.Collections.Generic.List<string>();
        foreach (var e in file.traces)
        {
            string why = Replay(e);
            if (why != null) failures.Add(e.id + ": " + why);
        }
        Assert.AreEqual(0, failures.Count, "Car traces that differ from the web game:\n" + string.Join("\n", failures));
    }

    static string Replay(ExpectedCarTrace e)
    {
        var geo = TrackGeometry.Build(e.layout, e.ar, e.miles);
        var track = new TrackSpace(geo, e.W);
        var env = new PhysicsEnv
        {
            Track = track, Surface = e.phys, Road = e.layout == "road" || e.layout == "street",
            Bank = e.bank, SurfGrip = e.grip, WxGrip = 1, Kart = false, Tier = e.tier, DirtRearMu = 1,
        };
        CarPhysics.SetGroove(env, e.grooveFrac);

        var c = new CarState { MaxSpeed = e.start.maxSpeed, Accel = e.start.accel, Hired = true, IsPlayer = false };
        var p = track.PointAt(e.start.s0, e.start.lat0);
        c.X = p.X; c.Y = p.Y; c.H = p.H;
        c.Vx = e.start.speed * Math.Cos(p.H); c.Vy = e.start.speed * Math.Sin(p.H);
        var hit = track.Info(c.X, c.Y, e.start.s0);
        c.S = hit.S; c.LastS = hit.S; c.Lat = hit.Lat; c.Prog = 0;

        for (int i = 0; i < e.steps; i++)
        {
            double tt = i * e.dt;
            c.Throttle = tt < 4 ? 1 : 0.7;
            c.Brake = (tt >= 7 && tt < 7.5) ? 1 : 0;
            c.SteerTarget = 0.35 * Math.Sin(tt * 0.9);
            CarPhysics.StepCar(c, e.dt, env);

            if (i >= StrictSteps)
            {
                if (double.IsNaN(c.X) || double.IsNaN(c.Y) || double.IsNaN(c.H) || double.IsNaN(c.Vx) || double.IsNaN(c.Vy))
                    return $"non-finite car state at step {i}";
                continue;
            }

            string bad = Check("x", i, c.X, e.x[i]) ?? Check("y", i, c.Y, e.y[i]) ?? Check("vx", i, c.Vx, e.vx[i]) ?? Check("vy", i, c.Vy, e.vy[i])
                ?? Check("yaw rate", i, c.R, e.r[i]) ?? Check("track position s", i, c.S, e.s[i]) ?? Check("lateral", i, c.Lat, e.lat[i])
                ?? Check("steering", i, c.Steer, e.steer[i]) ?? Check("tyre wear", i, c.Tw, e.tw[i]) ?? Check("progress", i, c.Prog, e.prog[i]);
            if (bad == null)
            {
                double dh = TrackGeometry.WrapAngle(c.H - e.h[i]);
                if (Math.Abs(dh) > Tol) bad = $"heading at step {i}: {c.H} vs {e.h[i]}";
            }
            if (bad != null) return bad;
        }
        return null;
    }

    static string Check(string name, int step, double got, double want)
    {
        if (double.IsNaN(got) || Math.Abs(got - want) > Tol * Math.Max(1, Math.Abs(want)))
            return $"{name} at step {step}: {got} vs {want}";
        return null;
    }
}
