// Port of collide() and circles() in src/core.js: two cars touch on their front and rear circles, push apart, and
// exchange speed and spin. Each real impact is reported through onImpact (the damage engine, which is not in this port yet).
using System;

public static class Collision
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    // the front and rear contact circles of a car
    static double[][] Circles(CarState c)
    {
        double fx = Math.Cos(c.H) * 9, fy = Math.Sin(c.H) * 9;
        return new[] { new[] { c.X + fx, c.Y + fy }, new[] { c.X - fx, c.Y - fy } };
    }

    public static void Collide(CarState a, CarState b, Action<CarState, CarState, double, double, double> onImpact = null)
    {
        if (a.Out || b.Out || a.Pit > 0 || b.Pit > 0 || Math.Abs(a.X - b.X) > 30 || Math.Abs(a.Y - b.Y) > 30) return;
        if (a.Recover > 0 || b.Recover > 0) return;   // a car untangling itself from a spin passes through the others for a moment
        var ca = Circles(a);
        var cb = Circles(b);
        foreach (var p in ca)
            foreach (var q in cb)
            {
                double dx = q[0] - p[0], dy = q[1] - p[1], d = Math.Sqrt(dx * dx + dy * dy);
                if (!(d > 0 && d < 10.5)) continue;
                double nx = dx / d, ny = dy / d, push = (10.5 - d) / 2;
                a.X -= nx * push; a.Y -= ny * push; b.X += nx * push; b.Y += ny * push;
                // where they touch, and each car's lever arm to it
                double cx = (p[0] + q[0]) / 2, cy = (p[1] + q[1]) / 2;
                double ax = cx - a.X, ay = cy - a.Y, bx = cx - b.X, by = cy - b.Y;
                double Ia = (a.Spec != null ? a.Spec.Iz : 80) * 1.4, Ib = (b.Spec != null ? b.Spec.Iz : 80) * 1.4;
                double wa = a.R, wb = b.R;
                double rvx = (b.Vx - wb * by) - (a.Vx - wa * ay), rvy = (b.Vy + wb * bx) - (a.Vy + wa * ax);
                double rv = rvx * nx + rvy * ny;
                if (rv < 0)
                {
                    double an = ax * ny - ay * nx, bn = bx * ny - by * nx;
                    double j = -(1 + 0.25) * rv / (2 + an * an / Ia + bn * bn / Ib);
                    a.Vx -= j * nx; a.Vy -= j * ny; b.Vx += j * nx; b.Vy += j * ny;
                    a.R = wa - j * an / Ia; b.R = wb + j * bn / Ib;
                    // rubbing: friction along the contact, so cars lean on each other instead of sliding off like ice
                    double tx = -ny, ty = nx, vt = rvx * tx + rvy * ty, at = ax * ty - ay * tx, bt = bx * ty - by * tx;
                    double jt = Clamp(-vt / (2 + at * at / Ia + bt * bt / Ib), -0.3 * j, 0.3 * j);
                    a.Vx -= jt * tx; a.Vy -= jt * ty; b.Vx += jt * tx; b.Vy += jt * ty;
                    a.R -= jt * at / Ia; b.R += jt * bt / Ib;
                    onImpact?.Invoke(a, b, -rv, cx, cy);
                }
                a.Bump = 0.2; b.Bump = 0.2;
            }
    }
}
