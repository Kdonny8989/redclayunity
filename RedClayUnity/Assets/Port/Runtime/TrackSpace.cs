// Port of trackInfo, pointAt, curvAt and idxOf in src/core.js: the racing line as a lookup space.
// s is distance along the lap, lat is the sideways offset (positive = right of travel).
using System;

public struct TrackHit { public double S, Lat, Nx, Ny; }
public struct TrackPoint { public double X, Y, H; }

public sealed class TrackSpace
{
    public readonly TrackGeometry G;
    public readonly double W;     // half the track width (game units); sets the search radius and the wall distance

    public TrackSpace(TrackGeometry g, double w) { G = g; W = w; }

    System.Collections.Generic.List<double[]> crossings;

    // src/events.js crossings(): pairs of distances around the lap that meet at the same spot (the figure-eight only)
    public System.Collections.Generic.List<double[]> Crossings(bool fig8)
    {
        if (crossings != null) return crossings;
        var outList = new System.Collections.Generic.List<double[]>();
        var g = G;
        if (fig8)
        {
            bool has = false;
            double best = 0, bestA = 0, bestB = 0;
            for (int i = 0; i < g.N; i += 2)
                for (int j = i + (int)Math.Floor(g.N * 0.25); j < g.N; j += 2)
                {
                    if (Math.Abs(i - j) > g.N * 0.75) continue;
                    double dx = g.X[i] - g.X[j], dy = g.Y[i] - g.Y[j];
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < W && (!has || d < best)) { has = true; best = d; bestA = i * g.Ds; bestB = j * g.Ds; }
                }
            if (has) outList.Add(new[] { bestA, bestB });
        }
        crossings = outList;
        return crossings;
    }

    public double Total => G.Total;
    public double Ds => G.Ds;
    public int N => G.N;

    public int IdxOf(double s)
    {
        double f = Math.Floor(s / G.Ds) % G.N;
        return (int)(((f % G.N) + G.N) % G.N);
    }

    public double CurvAt(double s) => G.K[IdxOf(s)];

    // world position -> distance along track (s), lateral offset (lat), right-hand normal
    public TrackHit Info(double x, double y, double? hint = null)
    {
        int N = G.N;
        int best = -1;
        double bd = 1e18;
        void Scan(int from, int to)
        {
            for (int j = from; j <= to; j++)
            {
                int i = ((j % N) + N) % N;
                double dx = x - G.X[i], dy = y - G.Y[i], d = dx * dx + dy * dy;
                if (d < bd) { bd = d; best = i; }
            }
        }
        if (hint.HasValue) { int h0 = IdxOf(hint.Value); Scan(h0 - 30, h0 + 30); }
        if (best < 0 || bd > (W * 2) * (W * 2)) { bd = 1e18; Scan(0, N - 1); }

        double hd = G.H[best], fx = Math.Cos(hd), fy = Math.Sin(hd), nx = -fy, ny = fx;
        double ddx = x - G.X[best], ddy = y - G.Y[best];
        double s = (((best * G.Ds + ddx * fx + ddy * fy) % Total) + Total) % Total;
        return new TrackHit { S = s, Lat = ddx * nx + ddy * ny, Nx = nx, Ny = ny };
    }

    // distance along track + lateral offset -> world position and track heading
    public TrackPoint PointAt(double s, double lat)
    {
        s = ((s % Total) + Total) % Total;
        double f = s / G.Ds;
        int i = (int)(Math.Floor(f) % G.N), j = (i + 1) % G.N;
        double t = f - Math.Floor(f);
        double x = G.X[i] + (G.X[j] - G.X[i]) * t, y = G.Y[i] + (G.Y[j] - G.Y[i]) * t;
        double h = G.H[i] + TrackGeometry.WrapAngle(G.H[j] - G.H[i]) * t;
        return new TrackPoint { X = x - Math.Sin(h) * lat, Y = y + Math.Cos(h) * lat, H = h };
    }
}
