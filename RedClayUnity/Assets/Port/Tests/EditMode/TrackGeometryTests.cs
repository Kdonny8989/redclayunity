// Checks the C# track geometry against the numbers exported from the web game (Tests/Data/track-geometry-expected.json).
// Every track, every point: X, Y, heading and curvature must match within a small tolerance.
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedTrack { public string id; public string layout; public double ar; public double miles; public double W; public int n; public double ds; public double total; public double minR; public double[] x; public double[] y; public double[] h; public double[] k; }
[Serializable] class ExpectedFile { public ExpectedTrack[] tracks; }

public class TrackGeometryTests
{
    const double Tol = 1e-6;

    [Test]
    public void EveryTrackMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/track-geometry-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedFile>(File.ReadAllText(path));
        Assert.IsNotNull(file.tracks);
        Assert.Greater(file.tracks.Length, 0);

        var failures = new System.Collections.Generic.List<string>();
        foreach (var e in file.tracks)
        {
            var g = TrackGeometry.Build(e.layout, e.ar, e.miles);
            string why = Compare(e, g);
            if (why != null) failures.Add(e.id + ": " + why);
        }
        Assert.AreEqual(0, failures.Count, "Tracks that differ from the web game:\n" + string.Join("\n", failures));
    }

    static string Compare(ExpectedTrack e, TrackGeometry g)
    {
        if (g.N != e.n) return $"point count {g.N} vs {e.n}";
        if (Math.Abs(g.Ds - e.ds) > Tol) return $"spacing {g.Ds} vs {e.ds}";
        if (Math.Abs(g.Total - e.total) > Tol) return $"length {g.Total} vs {e.total}";
        if (Math.Abs(g.MinR - e.minR) > Tol * Math.Max(1, e.minR)) return $"tightest corner {g.MinR} vs {e.minR}";
        for (int i = 0; i < g.N; i++)
        {
            if (Math.Abs(g.X[i] - e.x[i]) > Tol) return $"x[{i}] {g.X[i]} vs {e.x[i]}";
            if (Math.Abs(g.Y[i] - e.y[i]) > Tol) return $"y[{i}] {g.Y[i]} vs {e.y[i]}";
            if (Math.Abs(TrackGeometry.WrapAngle(g.H[i] - e.h[i])) > Tol) return $"heading[{i}] {g.H[i]} vs {e.h[i]}";
            if (Math.Abs(g.K[i] - e.k[i]) > Tol) return $"curvature[{i}] {g.K[i]} vs {e.k[i]}";
        }
        return null;
    }
}
