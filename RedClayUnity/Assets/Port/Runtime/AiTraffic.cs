// Port of driveAI() in src/core.js: one AI car's decision for a frame, including the traffic handling (dodging slow or
// sideways cars, following, and staying clear of cars alongside). The random source is passed in so tests are repeatable.
// The crossing caution and the pack are in AiPack.cs; the derby is in AiDerby.cs.
using System;
using System.Collections.Generic;

public static class AiTraffic
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    // one AI decision. inRace: whether the race is running (the wheel only wanders then).
    public static void Drive(CarState c, IList<CarState> cars, double dt, PhysicsEnv env, RaceContext ctx)
    {
        // the derby: hunt, don't race
        if (env.Derby && ctx.Stage == "feature") { AiDerby.Drive(c, cars, dt, env, ctx); return; }
        double W = env.Track.W, TOTAL = env.Track.Total;
        var rnd = ctx.Rnd;
        double gameTime = ctx.Time;
        bool inRace = ctx.InRace;
        // recovering from a spin: back up with the wheel turned the other way, then drive off
        double h0 = env.Track.G.H[env.Track.IdxOf(c.S)];
        double err = TrackGeometry.WrapAngle(h0 - c.H);
        double v0 = Math.Sqrt(c.Vx * c.Vx + c.Vy * c.Vy);
        if (c.Recover > 0)
        {
            c.Recover -= dt; c.Throttle = 0; c.Brake = 1;
            c.SteerTarget = -(err != 0 ? Math.Sign(err) : 1);
            if (Math.Abs(err) < 0.5) c.Recover = 0;
            return;
        }
        c.StuckT = gameTime > 4 && (v0 < 10 || (Math.Abs(err) > 1.9 && v0 < 50)) ? c.StuckT + dt : 0;
        if (c.StuckT > 0.6) { c.Recover = 1.1; c.StuckT = 0; return; }

        double lane = Clamp(env.GrooveCenter + c.LaneOff + c.LaneNudge, -W + 14, W - 14);
        double target = c.MaxSpeed;
        if (c.DodgeT > 0) { c.DodgeT -= dt; lane = c.DodgeLane; }
        foreach (var o in cars)
        {
            if (o == c || o.Out || o.Pit > 0) continue;
            // distance ahead around the track, so a car a lap down (or up) is seen where it really is
            double gap = o.S - c.S; if (gap < 0) gap += TOTAL;
            // alongside: a car close beside us, either way round the lap
            double near = gap > TOTAL / 2 ? gap - TOTAL : gap;
            if (c.DodgeT <= 0 && Math.Abs(near) < 18 && Math.Abs(o.Lat - c.Lat) < 22)
            {
                lane = Clamp(o.Lat + (c.Lat >= o.Lat ? 1 : -1) * 24, -W + 14, W - 14);
                continue;
            }
            // trouble ahead: a car that's slow, sideways or backing up. Give it room and slow enough to stop short of it.
            if (gap > 0 && gap < 120 && c.VF > 70 && c.VF > o.VF + 6 && (o.VF < c.VF * 0.6 || Math.Abs(o.Beta) > 0.45 || o.Recover > 0))
            {
                if (Math.Abs(o.Lat - lane) < 30)
                {
                    if (c.DodgeT <= 0)
                    {
                        double low = o.Lat - 34, high = o.Lat + 34;
                        c.DodgeLane = Clamp(low > -W + 14 && (high > W - 14 || Math.Abs(low - lane) < Math.Abs(high - lane)) ? low : high, -W + 14, W - 14);
                        c.DodgeT = 1.2;
                    }
                    lane = c.DodgeLane;
                    if (Math.Abs(o.Lat - lane) < 22)
                        target = Math.Min(target, Math.Max(28, Math.Sqrt(Math.Pow(Math.Max(0, o.VF), 2) + 2 * 260 * Math.Max(0, gap - 22))));
                }
                continue;
            }
            if (gap > 0 && gap < 50 && Math.Abs(o.Lat - lane) < 16)
            {
                if (c.DodgeT <= 0 && c.VF > o.VF - 5)
                {
                    double low = o.Lat - 24, high = o.Lat + 24;
                    // the JavaScript: (low ok && random < 0.6) || high out of bounds ? low : high. The random is drawn only when low is ok.
                    bool useLow = (low > -W + 14 && rnd() < 0.6) || high > W - 14;
                    c.DodgeLane = Clamp(useLow ? low : high, -W + 14, W - 14);
                    c.DodgeT = 1.4;
                    lane = c.DodgeLane;
                }
                // a driver with a grudge leans on your cars instead of backing off
                if (gap < c.Follow && Math.Abs(o.Lat - lane) < 14 && !(c.Grudge && o.Team))
                    target = Math.Min(target, Math.Max(o.VF - 4, gap > 16 ? 26 : 0));
            }
        }
        AiDriver.Steer(c, env, lane, 46 + Math.Max(0, c.VF * 0.16));
        if (c.Twitch != 0 && inRace) c.SteerTarget = Clamp(c.SteerTarget + (rnd() - 0.5) * c.Twitch, -1, 1);
        target = Math.Min(target, AiPack.CrossingCaution(c, cars, env));
        target = Math.Min(target, AiDriver.CornerLimit(c, env, lane));
        target = AiPack.PackTarget(c, cars, target, dt, env, ctx);

        c.Throttle = c.VF < target ? 1 : 0;
        if (Math.Abs(c.Beta) > 0.22) c.Throttle = Math.Abs(c.Beta) > 0.5 ? 0 : 0.5;   // sideways: ease off and let the tyres grab
        c.Brake = c.VF > target + 18 ? 1 : 0;

        // less experienced drivers make mistakes: a lift, a twitch of the wheel
        if (c.Bobble > 0) { c.Bobble -= dt; c.Throttle = 0; c.SteerTarget = Clamp(c.SteerTarget + c.BobbleDir, -1, 1); }
        else if (rnd() < Math.Max(0, 1 - c.Skill) * 1.6 * dt) { c.Bobble = 0.3 + rnd() * 0.35; c.BobbleDir = (rnd() - 0.5) * 0.8; }

        // on dirt, a less experienced driver sometimes gets the rear loose under power
        bool inCorner = Math.Abs(env.Track.CurvAt(c.S)) > 1.0 / 1200;
        if (c.Spin == 0 && inCorner && c.VF > 120 && env.Surface == "dirt" && rnd() < Math.Max(0, 1 - c.Skill) * 0.02 * dt)
            c.R = c.R + (rnd() < 0.5 ? -1 : 1) * 0.9;
    }

    // one frame for an AI car, as the game runs it: the decision, then the pack's hard stop (src/logic.js)
    public static void Tick(CarState c, IList<CarState> cars, double dt, PhysicsEnv env, RaceContext ctx)
    {
        Drive(c, cars, dt, env, ctx);
        AiPack.PackPost(c, cars, env);
        if (c.PackBrake != 0) { c.Brake = 1; c.Throttle = 0; }
    }
}
