// Port of crossingCaution() in src/events.js and the pack logic in src/pack.js: the accordion (a sharp check-up through a
// corner ripples its braking back through the pack), the desperation laps, and the hard stop for trouble ahead.
using System;
using System.Collections.Generic;

// The race state the AI reads: the clock, the stage, the number of laps, whether the race is running, and the random source
public sealed class RaceContext
{
    public double Time = 10;
    public string Stage = "race";
    public int Laps = 8;
    public bool InRace = true;
    public Func<double> Rnd = () => 0.3;
}

public static class AiPack
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
    static double Mod(double a, double m) => ((a % m) + m) % m;

    // at a crossing, the driver who will get there second eases off rather than T-bone someone
    public static double CrossingCaution(CarState c, IList<CarState> cars, PhysicsEnv env)
    {
        var cx = env.Track.Crossings(env.Fig8);
        if (cx.Count == 0) return double.PositiveInfinity;
        double TOTAL = env.Track.Total;
        double v = Math.Max(40, c.VF);
        double cap = double.PositiveInfinity;
        foreach (var pair in cx)
        {
            double a = pair[0], b = pair[1];
            double aheadA = Mod(a - c.S, TOTAL), aheadB = Mod(b - c.S, TOTAL);
            double dMe = Math.Min(aheadA, aheadB);
            if (dMe > 170 || dMe < 4) continue;
            double myLeg = aheadA < aheadB ? a : b, otherLeg = myLeg == a ? b : a, tMe = dMe / v;
            foreach (var o in cars)
            {
                if (o == c || o.Out || o.Pit > 0) continue;
                double dO = Mod(otherLeg - o.S, TOTAL);
                if (dO > 200 || dO < 2) continue;
                double tO = dO / Math.Max(40, o.VF);
                if (Math.Abs(tO - tMe) < 0.5 * (1.4 - c.Skill) * 2.2 && tO <= tMe) cap = Math.Min(cap, v * 0.7);
            }
        }
        return cap;
    }

    // the pack's target speed for one car: the accordion, and the desperation laps in the last three of a feature
    public static double PackTarget(CarState c, IList<CarState> cars, double target, double dt, PhysicsEnv env, RaceContext ctx)
    {
        if (!Aero.AeroOn(c, env) || c.Out || c.Pit > 0) return target;
        double TOTAL = env.Track.Total;
        c.CheckT = Math.Max(0, c.CheckT - dt);
        bool inTurn = Math.Abs(env.Track.CurvAt(c.S)) > 1.0 / 1200;
        // a sharp check-up in a corner is armed once per corner, and only sometimes triggers the ripple
        if (inTurn && c.VF > 60 && target < c.VF - 14)
        {
            if (!c.CheckArmed) { c.CheckArmed = true; if (ctx.Rnd() < 0.35) c.CheckT = 0.8; }
        }
        else if (!inTurn) c.CheckArmed = false;

        double t = target, nudge = 0;
        // the car ahead has checked up: brake a beat later, and a little harder for each car back in the line
        foreach (var o in cars)
        {
            if (o == c || o.Out || o.Pit > 0 || !(o.CheckT > 0)) continue;
            double gap = o.S - c.S; if (gap < 0) gap += TOTAL;
            if (gap < 2 || gap > 70 || Math.Abs(o.Lat - c.Lat) > 16) continue;
            if (c.RippleAt == 0 || c.RippleFrom != o) { c.RippleFrom = o; c.RippleAt = ctx.Time + 0.12 + gap * 0.004; }
            if (ctx.Time >= c.RippleAt && ctx.Time < c.RippleAt + 0.9) t = Math.Min(t, Math.Max(24, c.VF * 0.9));
        }

        // desperation laps: the last three laps of a feature, the podium and the car chasing it throw everything at it
        int rank = 1;
        foreach (var o in cars) if (o != c && !o.Out && o.Prog > c.Prog) rank++;
        if (ctx.Stage == "feature" && ctx.Laps > 3 && c.Prog >= (ctx.Laps - 3) * TOTAL && rank <= 4 && !c.IsPlayer)
        {
            c.Desperate = true;
            t *= 1.035;   // later braking, more speed through the corner
            // lunge: a car just ahead gets a nudge toward its inside line
            foreach (var o in cars)
            {
                if (o == c || o.Out || o.Pit > 0) continue;
                double gap = o.S - c.S; if (gap < 0) gap += TOTAL;
                if (gap > 0 && gap < 26 && Math.Abs(o.Lat - c.Lat) < 12)
                {
                    nudge = Clamp((o.Lat - c.Lat) * -0.3 + (c.Lat > 0 ? -4 : 4), -8, 8);
                    break;
                }
            }
        }
        else c.Desperate = false;
        c.LaneNudge = nudge;
        return t;
    }

    // the hard stop: a car ahead that's stopped, or sitting in our line, is hit with the brakes, not just the lift
    public static void PackPost(CarState c, IList<CarState> cars, PhysicsEnv env)
    {
        double TOTAL = env.Track.Total;
        c.PackBrake = 0;
        if (!Aero.AeroOn(c, env) || c.Out || c.Pit > 0) return;
        foreach (var o in cars)
        {
            if (o == c || o.Pit > 0) continue;
            if (!(o.Out || o.Recover > 0 || Math.Abs(o.Spin) > 0.5)) continue;
            double gap = o.S - c.S; if (gap < 0) gap += TOTAL;
            if (gap > 0 && gap < 40 && Math.Abs(o.Lat - c.Lat) < 14) { c.PackBrake = 1; return; }
        }
    }
}
