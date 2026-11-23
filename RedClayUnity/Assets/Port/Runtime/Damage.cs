// Port of the damage rules in src/damage.js and applyDamage / hurt / knockOut in src/logic.js: where a hit lands, what it
// does to each part of the car, and when a car is out. The side effects of a hit (sparks, sound, the on-screen warnings)
// are not here; they are interface code.
using System;

public static class Damage
{
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    static double ZoneValue(CarState c, string zone) => zone switch
    {
        "front" => c.ZoneFront, "rear" => c.ZoneRear, "left" => c.ZoneLeft, "right" => c.ZoneRight, _ => 0,
    };

    static void SetZone(CarState c, string zone, double v)
    {
        switch (zone)
        {
            case "front": c.ZoneFront = v; break;
            case "rear": c.ZoneRear = v; break;
            case "left": c.ZoneLeft = v; break;
            case "right": c.ZoneRight = v; break;
            default: throw new ArgumentException("Unknown zone: " + zone);
        }
    }

    // which part of the car a point touches
    public static string ZoneAt(CarState c, double px, double py)
    {
        double fx = Math.Cos(c.H), fy = Math.Sin(c.H), dx = px - c.X, dy = py - c.Y;
        double f = dx * fx + dy * fy, l = -dx * fy + dy * fx;
        return Math.Abs(f) > 6.5 ? (f > 0 ? "front" : "rear") : (l > 0 ? "right" : "left");
    }

    // damage from a hit at speed v (scaled for the division): it grows faster than the speed does
    public static double HitDamage(double v, double from, double scale) =>
        v <= from ? 0 : Math.Pow((v - from) / 120, 1.4) * scale;

    // the top speed and the grip, from the car's damage
    public static void ApplyDamage(CarState c)
    {
        c.MaxSpeed = c.BaseMax * (1 - 0.2 * c.Dmg - 0.08 * c.ZoneFront) * c.PowerCut;
        c.GripMul = c.BaseGrip * (1 - 0.16 * c.Dmg);
    }

    // the nose, tail and sides change the car's handling
    public static void ApplyZoneEffects(CarState c)
    {
        if (c.BaseTw == null) c.BaseTw = c.TwMul;
        c.RearGrip = 1 - 0.12 * c.ZoneRear;
        c.Toe = 0.018 * (c.ZoneRight - c.ZoneLeft);
        c.TwMul = c.BaseTw.Value * (1 + 0.4 * Math.Max(c.ZoneLeft, c.ZoneRight));
        ApplyDamage(c);
    }

    // the car as a whole: at 100% it's out of the race
    public static void Hurt(CarState c, double d, TrackSpace track)
    {
        if (c.Out || c.Finished || d <= 0) return;
        c.Dmg = Math.Min(1, c.Dmg + d);
        ApplyDamage(c);
        if (c.Dmg >= 1) KnockOut(c, track);
    }

    // the car is out: it is set down beside the track, stopped
    public static void KnockOut(CarState c, TrackSpace track)
    {
        c.Out = true;
        c.Spin = 0;
        var p = track.PointAt(c.S, -track.W - 26);
        c.X = p.X; c.Y = p.Y; c.H = p.H;
        c.Vx = 0; c.Vy = 0;
    }

    // one hit on one part of the car: the part takes damage, the handling changes, and the car as a whole takes a share
    public static void DamageZone(CarState c, string zone, double amount, TrackSpace track, bool haunted = false)
    {
        if (c.Out || c.Finished || amount <= 0.002) return;
        if (c.DmgResist > 0) amount *= 1 - Math.Min(0.5, c.DmgResist);
        if (haunted && zone == "right") amount *= 1.6;   // the bullring's concrete walls
        // stronger parts take less of each hit
        SetZone(c, zone, Math.Min(1, ZoneValue(c, zone) + amount));
        c.HitT = 0.4;
        ApplyZoneEffects(c);
        Hurt(c, amount * 0.55, track);
    }
}
