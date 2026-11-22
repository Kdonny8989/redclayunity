// Port of derbyDrive() in src/derby.js: the demolition derby driver. It hunts the nearest car (sometimes the player),
// aims where the target is going, backs off for a run-up after a hit, and reverses when pinned. The referee (the end of
// the derby) is not in this file yet.
using System;
using System.Collections.Generic;

public static class AiDerby
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    public static void Drive(CarState c, IList<CarState> cars, double dt, PhysicsEnv env, RaceContext ctx)
    {
        var rnd = ctx.Rnd;
        var alive = new List<CarState>();
        foreach (var o in cars) if (o != c && !o.Out && !o.Finished) alive.Add(o);
        if (alive.Count == 0 || !ctx.InRace) { c.Throttle = 0; c.Brake = 0; return; }

        Func<CarState, double> dist = o => Math.Sqrt((o.X - c.X) * (o.X - c.X) + (o.Y - c.Y) * (o.Y - c.Y));

        // after a hit, back away for a run-up, then charge again: hits at speed, not shoving matches
        if (c.Bump > 0.15 && !(c.RunT > 0) && !(c.BackT > 0) && Math.Sqrt(c.Vx * c.Vx + c.Vy * c.Vy) < 70)
        {
            c.RunT = 0.9 + rnd() * 0.7;
            c.DtgtT = 0;
        }
        if (c.RunT > 0)
        {
            c.RunT -= dt; c.Throttle = 0; c.Brake = 1;
            c.SteerTarget = (c.Num % 2 != 0 ? 1 : -1) * 0.6;
            return;
        }

        // pick a target when there isn't one, it has gone, or the time on it has run out
        bool pick = c.Dtgt == null || c.Dtgt.Out || c.Dtgt.Finished;
        if (!pick) { c.DtgtT = c.DtgtT - dt; pick = c.DtgtT < 0; }
        if (pick)
        {
            // the nearest car with room for a run at it; failing that, the nearest
            var roomy = new List<CarState>();
            foreach (var o in alive) if (dist(o) > 45) roomy.Add(o);
            var pool = roomy.Count > 0 ? roomy : alive;
            var m = pool[0];
            foreach (var o in pool) if (dist(o) < dist(m)) m = o;
            c.Dtgt = m;
            if (rnd() < 0.25 && ctx.Player != null && ctx.Player != c && !ctx.Player.Out) c.Dtgt = ctx.Player;
            c.DtgtT = 2 + rnd() * 2.5;
        }

        var t = c.Dtgt;
        double tx = t.X + t.Vx * 0.35, ty = t.Y + t.Vy * 0.35, v = Math.Sqrt(c.Vx * c.Vx + c.Vy * c.Vy);
        double ang = TrackGeometry.WrapAngle(Math.Atan2(ty - c.Y, tx - c.X) - c.H);

        // pinned against a wall or another car: back out
        if (c.BackT > 0)
        {
            c.BackT -= dt; c.Throttle = 0; c.Brake = 1;
            c.SteerTarget = -(ang != 0 ? Math.Sign(ang) : 1);
            return;
        }
        c.StuckT = v < 12 && c.Throttle != 0 ? c.StuckT + dt : 0;
        if (c.StuckT > 0.9) { c.BackT = 0.8; c.StuckT = 0; return; }

        // a target behind gets backed into
        if (Math.Abs(ang) > 2.3 && v < 70)
        {
            c.Throttle = 0; c.Brake = 1;
            c.SteerTarget = Clamp(TrackGeometry.WrapAngle(ang + Math.PI) * 2, -1, 1);
            return;
        }
        c.SteerTarget = Clamp(ang * 2.4, -1, 1); c.Throttle = 1; c.Brake = 0;
    }
}
