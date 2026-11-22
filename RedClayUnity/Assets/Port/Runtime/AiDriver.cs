// Port of aiSteer() and cornerLimit() in src/core.js: how an AI driver steers toward the racing line and how fast it
// will carry a car into the corners ahead. Traffic, the pack, cautions and random mistakes are not in this file yet.
using System;

public static class AiDriver
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    // steer like a driver: aim at a point down the lane, turn the wheel for that curve, and catch the car if it rotates too much
    public static void Steer(CarState c, PhysicsEnv env, double lane, double look)
    {
        var P = c.Spec ?? (c.Spec = CarPhysics.SpecFor(env));
        double v = Math.Max(1, Math.Abs(c.VF));
        var ahead = env.Track.PointAt(c.S + look, lane);
        double diff = TrackGeometry.WrapAngle(Math.Atan2(ahead.Y - c.Y, ahead.X - c.X) - c.H);
        double kWant = 2 * Math.Sin(diff) / look, rWant = v * kWant;
        double delta = Math.Atan(P.L * kWant) + 0.045 * (rWant - c.R) - 0.35 * c.Beta;
        c.SteerTarget = Clamp(delta / CarPhysics.SteerLimit(v), -1, 1);
    }

    // the fastest speed the AI will carry: scan ahead for corners, leaving room to brake for them
    public static double CornerLimit(CarState c, PhysicsEnv env, double lane)
    {
        var g = env.Track.G;
        double look = 50 + Math.Max(0, c.VF) * 0.55;
        const int stride = 3;
        int i0 = env.Track.IdxOf(c.S);
        double best = 1e9;
        for (int k = 0; k * g.Ds <= look; k += stride)
        {
            double kk = g.K[(i0 + k) % g.N];
            if (Math.Abs(kk) < 1.0 / 1500) continue;
            double r = Math.Max(25, 1 / Math.Abs(kk) + lane * (kk < 0 ? 1 : -1));
            var P = c.Spec ?? (c.Spec = CarPhysics.SpecFor(env));
            double top = Math.Max(60, c.MaxSpeed);
            double muA = 0.8 * c.Margin * CarPhysics.GripAt(env, lane) * c.GripMul * CarPhysics.TireGrip(c) * Math.Min(1, P.RearMu + 0.04);
            double vt = Math.Sqrt(muA * CarPhysics.G * r / Math.Max(0.3, 1 - muA * P.DfFrac * CarPhysics.G * r / (top * top))) * c.Skill;
            double allowed = Math.Sqrt(vt * vt + 2 * 230 * k * g.Ds);
            if (allowed < best) best = allowed;
        }
        return best;
    }
}
