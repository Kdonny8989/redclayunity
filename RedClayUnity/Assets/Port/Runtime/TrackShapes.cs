// Port of SHAPES in src/core.js and src/events.js (the figure-eight).
// Each shape is a closed polygon listed counterclockwise on screen, with a corner radius per point.
// Apex is the index of the dogleg corner for tri-ovals and D-ovals (-1 when there is none).
using System;
using System.Collections.Generic;

public sealed class ShapeDef
{
    public List<double[]> Pts = new List<double[]>();
    public List<double> R = new List<double>();
    public int Apex = -1;
}

public static class TrackShapes
{
    public static ShapeDef Get(string layout, double ar)
    {
        switch (layout)
        {
            case "oval":
            case "paperclip":
                return Make(new[] { -ar, 1, ar, 1, ar, -1, -ar, -1 }, new[] { 1.0, 1, 1, 1 });
            case "doval":
                return Make(new[] { -ar, 1, 0, 1.22, ar, 1, ar, -1, -ar, -1 }, new[] { 1.0, 4, 1, 1, 1 }, 1);
            case "trioval":
                return Make(new[] { -ar, 1, 0, 1.5, ar, 1, ar, -1, -ar, -1 }, new[] { 0.9, 1.6, 0.9, 1, 1 }, 1);
            case "triangle":
                return Make(new[] { -1.5, 0.9, 1.5, 0.9, 0.15, -1.3 }, new[] { 0.55, 0.75, 0.45 });
            case "asym":
                return Make(new[] { -ar, 1, ar, 0.72, ar, -0.72, -ar, -1 }, new[] { 1.0, 0.72, 0.72, 1 });
            case "road":
                return Make(new[] { -2, 1, 1.4, 1, 2, 0.5, 2, -0.4, 1.2, -0.4, 0.9, -1.2, -0.2, -1.2, -0.5, -0.3, -1.2, -0.3, -1.5, -1, -2.2, -1, -2.4, 0.2 },
                            new[] { 0.35, 0.3, 0.3, 0.25, 0.22, 0.3, 0.25, 0.22, 0.22, 0.25, 0.3, 0.35 });
            case "street":
                return Make(new[] { -2, 0.8, 2, 0.8, 2, -0.8, 0.9, -0.8, 0.9, 0, -0.3, 0, -0.3, -0.8, -2, -0.8 },
                            new[] { 0.2, 0.2, 0.2, 0.16, 0.16, 0.16, 0.16, 0.2 });
            case "figure8":
                return Figure8(ar);
            default:
                throw new ArgumentException("Unknown track layout: " + layout);
        }
    }

    static ShapeDef Make(double[] flat, double[] radii, int apex = -1)
    {
        var s = new ShapeDef { Apex = apex };
        for (int i = 0; i < flat.Length; i += 2) s.Pts.Add(new[] { flat[i], flat[i + 1] });
        s.R.AddRange(radii);
        return s;
    }

    // src/events.js: a smooth infinity sign, 140 points, both loops crossing in the middle
    static ShapeDef Figure8(double ar)
    {
        var s = new ShapeDef();
        for (int k = 0; k < 140; k++)
        {
            double t = k / 140.0 * Math.PI * 2;
            s.Pts.Add(new[] { ar * 1.45 * Math.Cos(t), Math.Sin(2 * t) });
            s.R.Add(0.02);
        }
        return s;
    }
}
