// Port of computeAero() and aeroTick() in src/aero.js: side-drafting, dirty air in corners, and the bump-draft push.
// Only cars with downforce (the Regional Feeder and up) feel the air.
using System;
using System.Collections.Generic;

public static class Aero
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    static bool AeroOn(CarState c, PhysicsEnv env)
    {
        var P = c.Spec ?? (c.Spec = CarPhysics.SpecFor(env));
        return P.DfFrac > 0;
    }

    // how hard a car is loaded through the corner it's in: 0 on the straights, 1 in a tight turn at speed
    static double CornerLoad(CarState c, PhysicsEnv env) =>
        Clamp(Math.Abs(env.Track.CurvAt(c.S)) * 600, 0, 1) * Clamp(c.VF / 200, 0, 1);

    // each frame, after drafting: side-drafting, and the dirty air in a corner
    public static void Compute(IList<CarState> cars, PhysicsEnv env)
    {
        foreach (var c in cars) { c.DirtyAir = 0; c.RearHit = 0; }
        foreach (var c in cars)
        {
            if (c.Out || c.Finished || c.Pit > 0 || !AeroOn(c, env)) continue;
            foreach (var o in cars)
            {
                if (o == c || o.Out || o.Finished || o.Pit > 0) continue;
                double along = o.Prog - c.Prog, ad = Math.Abs(c.Lat - o.Lat);
                // side-drafting: your nose alongside the rival's rear quarter, a touch ahead
                if (c.Prog - o.Prog > 0 && c.Prog - o.Prog < 12 && ad > 14 && ad < 26)
                {
                    c.Draft = Math.Max(c.Draft, 0.35);
                    o.RearHit = 1;
                }
                // dirty air: tucked into the wake of the car ahead, in a corner
                if (along > 4 && along < 30 && ad < 9)
                {
                    double near = Clamp(1 - (along - 4) / 26, 0, 1);
                    c.DirtyAir = Math.Max(c.DirtyAir, CornerLoad(c, env) * near);
                }
            }
        }
        // a bump-draft push is the strongest draft there is
        foreach (var c in cars) if (c.PushT > 0) c.Draft = 1;
    }

    // the push timer counts down
    public static void Tick(double dt, IList<CarState> cars)
    {
        foreach (var c in cars) if (c.PushT > 0) c.PushT = Math.Max(0, c.PushT - dt);
    }
}
