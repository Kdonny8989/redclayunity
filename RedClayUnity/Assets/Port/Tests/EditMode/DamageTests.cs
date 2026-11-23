// Checks the C# damage rules against the web game (Tests/Data/damage-expected.json): 300 hits on random cars (with and
// without the bullring's wall rule), 200 zone checks, and 100 hit sizes.
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

[Serializable] class ExpectedDamageZones { public double front; public double rear; public double left; public double right; }
[Serializable] class ExpectedDamageInput
{
    public double x; public double y; public double h; public double s; public double dmg; public ExpectedDamageZones zones; public double twMul;
    public double baseMax; public double baseGrip; public double powerCut; public double dmgresist; public double vx; public double vy;
    public bool haunted; public string zone; public double amount;
    public double maxSpeed; public double rearGrip; public double toe; public double gripMul;
}
[Serializable] class ExpectedDamageOut
{
    public double dmg; public ExpectedDamageZones zones; public double maxSpeed; public double gripMul; public double rearGrip; public double toe;
    public double twMul; public bool @out; public double x; public double y; public double h; public double vx; public double vy;
}
[Serializable] class ExpectedDamageCase { public string track; public ExpectedDamageInput input; public ExpectedDamageOut @out; }
[Serializable] class ExpectedDamageTrack { public string id; public string layout; public double ar; public double miles; public double W; }
[Serializable] class ExpectedZoneCase { public double x; public double y; public double h; public double px; public double py; public string zone; }
[Serializable] class ExpectedHitCase { public double v; public double from; public double scale; public double damage; }
[Serializable] class ExpectedDamageFile { public ExpectedDamageTrack[] tracks; public ExpectedDamageCase[] cases; public ExpectedZoneCase[] zoneCases; public ExpectedHitCase[] hitCases; }

public class DamageTests
{
    const double Tol = 1e-9;

    static bool Near(double a, double b) => Math.Abs(a - b) <= Tol * Math.Max(1, Math.Abs(b));

    [Test]
    public void EveryHitMatchesTheWebGame()
    {
        string path = Path.Combine(Application.dataPath, "Port/Tests/Data/damage-expected.json");
        Assert.IsTrue(File.Exists(path), "Missing expected data: " + path);
        var file = JsonUtility.FromJson<ExpectedDamageFile>(File.ReadAllText(path));

        var tracks = new Dictionary<string, ExpectedDamageTrack>();
        foreach (var t in file.tracks) tracks[t.id] = t;

        var failures = new List<string>();
        foreach (var e in file.cases)
        {
            var i = e.input;
            var tt = tracks[e.track];
            var space = new TrackSpace(TrackGeometry.Build(tt.layout, tt.ar, tt.miles), tt.W);
            var c = new CarState
            {
                X = i.x, Y = i.y, H = i.h, S = i.s, Dmg = i.dmg, ZoneFront = i.zones.front, ZoneRear = i.zones.rear, ZoneLeft = i.zones.left, ZoneRight = i.zones.right,
                TwMul = i.twMul, BaseMax = i.baseMax, BaseGrip = i.baseGrip, PowerCut = i.powerCut, DmgResist = i.dmgresist, Vx = i.vx, Vy = i.vy,
                MaxSpeed = i.maxSpeed, RearGrip = i.rearGrip, Toe = i.toe, GripMul = i.gripMul,
            };
            Damage.DamageZone(c, i.zone, i.amount, space, i.haunted);
            var o = e.@out;
            bool same = Near(c.Dmg, o.dmg) && Near(c.ZoneFront, o.zones.front) && Near(c.ZoneRear, o.zones.rear) && Near(c.ZoneLeft, o.zones.left) && Near(c.ZoneRight, o.zones.right)
                && Near(c.MaxSpeed, o.maxSpeed) && Near(c.GripMul, o.gripMul) && Near(c.RearGrip, o.rearGrip) && Near(c.Toe, o.toe) && Near(c.TwMul, o.twMul)
                && c.Out == o.@out && Near(c.X, o.x) && Near(c.Y, o.y) && Near(c.H, o.h) && Near(c.Vx, o.vx) && Near(c.Vy, o.vy);
            if (!same) failures.Add($"hit {i.zone} {i.amount} on dmg {i.dmg}: dmg {c.Dmg} vs {o.dmg}, max {c.MaxSpeed} vs {o.maxSpeed}, out {c.Out} vs {o.@out}");
        }
        foreach (var z in file.zoneCases)
        {
            var c = new CarState { X = z.x, Y = z.y, H = z.h };
            string got = Damage.ZoneAt(c, z.px, z.py);
            if (got != z.zone) failures.Add($"zone at ({z.px},{z.py}): {got} vs {z.zone}");
        }
        foreach (var h in file.hitCases)
        {
            double got = Damage.HitDamage(h.v, h.from, h.scale);
            if (!Near(got, h.damage)) failures.Add($"hit damage v {h.v}: {got} vs {h.damage}");
        }
        Assert.AreEqual(0, failures.Count, failures.Count + " damage results differ from the web game:\n" + string.Join("\n", failures.GetRange(0, Math.Min(20, failures.Count))));
    }
}
