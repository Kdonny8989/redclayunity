// Port of buildGeometry() in src/core.js. Builds the racing line of a track as evenly spaced points.
// Rounding matches JavaScript: Math.Round is replaced by Math.Floor(v + 0.5), which is what Math.round does.
using System;
using System.Collections.Generic;

public sealed class TrackGeometry
{
    public int N;            // number of points on the lap
    public double Ds;        // spacing between points (game units)
    public double[] X, Y, H, K;
    public double Total;     // lap length (game units)
    public double MinR;      // tightest corner radius on the line

    const double PI = Math.PI;

    public static double MilesToUnits(double miles) => 1520.0 * Math.Pow(miles / 0.25, 0.6);

    public static double WrapAngle(double a)
    {
        while (a > PI) a -= 2 * PI;
        while (a < -PI) a += 2 * PI;
        return a;
    }

    static double JsRound(double v) => Math.Floor(v + 0.5);

    public static TrackGeometry Build(string layout, double ar, double miles)
    {
        ShapeDef sh = TrackShapes.Get(layout, ar);
        double target = MilesToUnits(miles);
        var P = sh.Pts;
        int n = P.Count;

        // fillet the polygon into straights and arcs
        var raw = new List<double[]>();
        var arcStart = new int[n];
        var arcEnd = new int[n];
        for (int i = 0; i < n; i++)
        {
            var a = P[(i - 1 + n) % n];
            var b = P[i];
            var c = P[(i + 1) % n];
            double d1x = b[0] - a[0], d1y = b[1] - a[1];
            double d2x = c[0] - b[0], d2y = c[1] - b[1];
            double l1 = Math.Sqrt(d1x * d1x + d1y * d1y), l2 = Math.Sqrt(d2x * d2x + d2y * d2y);
            double u1x = d1x / l1, u1y = d1y / l1, u2x = d2x / l2, u2y = d2y / l2;
            double h1 = Math.Atan2(u1y, u1x), h2 = Math.Atan2(u2y, u2x);
            double th = h2 - h1;
            while (th > PI) th -= 2 * PI;
            while (th < -PI) th += 2 * PI;
            double r = sh.R[i];
            double tlen = r * Math.Tan(Math.Abs(th) / 2);
            double tmax = 0.5 * Math.Min(l1, l2) * 0.999;
            if (tlen > tmax)
            {
                tlen = tmax;
                r = tlen / Math.Tan(Math.Abs(th) / 2);
            }
            double s0x = b[0] - u1x * tlen, s0y = b[1] - u1y * tlen;
            double side = th < 0 ? 1 : -1;               // left turns curve toward the left normal
            double lnx = Math.Sin(h1), lny = -Math.Cos(h1);
            double cx = s0x + lnx * r * side, cy = s0y + lny * r * side;
            double a0 = Math.Atan2(s0y - cy, s0x - cx);
            int steps = (int)Math.Max(2, Math.Ceiling(Math.Abs(th) / 0.05));
            arcStart[i] = raw.Count;
            for (int k = 0; k <= steps; k++)
            {
                double ang = a0 + th * k / steps;
                raw.Add(new[] { cx + Math.Cos(ang) * r, cy + Math.Sin(ang) * r });
            }
            arcEnd[i] = raw.Count - 1;
        }

        // start the lap halfway along the first straight (or at the dogleg for tri-ovals and D-ovals)
        var loop = new List<double[]>();
        if (sh.Apex >= 0)
        {
            int mid = (int)JsRound((arcStart[sh.Apex] + arcEnd[sh.Apex]) / 2.0);
            loop.AddRange(raw.GetRange(mid, raw.Count - mid));
            loop.AddRange(raw.GetRange(0, mid));
        }
        else
        {
            var fa = raw[arcEnd[0]];
            var fb = raw[arcStart[1]];
            loop.Add(new[] { (fa[0] + fb[0]) / 2, (fa[1] + fb[1]) / 2 });
            loop.AddRange(raw.GetRange(arcStart[1], raw.Count - arcStart[1]));
            loop.AddRange(raw.GetRange(0, arcStart[1]));
        }

        double per = 0;
        for (int i = 0; i < loop.Count; i++)
        {
            var p = loop[i];
            var q = loop[(i + 1) % loop.Count];
            per += Math.Sqrt((q[0] - p[0]) * (q[0] - p[0]) + (q[1] - p[1]) * (q[1] - p[1]));
        }
        double sc = target / per;
        var pts = new List<double[]>(loop.Count);
        foreach (var p in loop) pts.Add(new[] { p[0] * sc, p[1] * sc });

        // resample evenly by distance
        const double dsWanted = 4;
        int NN = (int)JsRound(target / dsWanted);
        double step = target / NN;
        var X = new double[NN];
        var Y = new double[NN];
        int seg = 0;
        double segStart = 0;
        double segLen = Dist(pts[0], pts[1]);
        for (int i = 0; i < NN; i++)
        {
            double d = i * step;
            while (d > segStart + segLen && seg < pts.Count - 1)
            {
                segStart += segLen;
                seg++;
                var p2 = pts[seg];
                var q2 = pts[(seg + 1) % pts.Count];
                segLen = Dist(p2, q2);
            }
            var p = pts[seg];
            var q = pts[(seg + 1) % pts.Count];
            double f = segLen != 0 ? (d - segStart) / segLen : 0;
            X[i] = p[0] + (q[0] - p[0]) * f;
            Y[i] = p[1] + (q[1] - p[1]) * f;
        }

        var H = new double[NN];
        var K = new double[NN];
        for (int i = 0; i < NN; i++)
        {
            int a = (i - 1 + NN) % NN, b = (i + 1) % NN;
            H[i] = Math.Atan2(Y[b] - Y[a], X[b] - X[a]);
        }
        for (int i = 0; i < NN; i++)
        {
            int a = (i - 3 + NN) % NN, b = (i + 3) % NN;
            double dh = H[b] - H[a];
            while (dh > PI) dh -= 2 * PI;
            while (dh < -PI) dh += 2 * PI;
            K[i] = dh / (6 * step);
        }
        double minR = 1e9;
        for (int i = 0; i < NN; i++)
            if (Math.Abs(K[i]) > 1e-6) minR = Math.Min(minR, 1 / Math.Abs(K[i]));

        return new TrackGeometry { N = NN, Ds = step, X = X, Y = Y, H = H, K = K, Total = target, MinR = minR };
    }

    static double Dist(double[] p, double[] q) => Math.Sqrt((q[0] - p[0]) * (q[0] - p[0]) + (q[1] - p[1]) * (q[1] - p[1]));
}
