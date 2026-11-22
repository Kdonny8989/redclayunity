// Port of the grip settings in src/surface.js (dirt states) and weatherGrip() in src/logic.js.
// The track's grip for a race depends on the time of day, the weather, and the state of the dirt.
using System;

public sealed class DirtState
{
    public string Name;
    public double Grip, RearMu, Accel;
}

public sealed class WeatherState
{
    public string Tod = "day";        // "day", "dusk" or "night"
    public string Wx = "clear";       // "clear", "wet", "rain" or "shower"
    public bool ShowerOn;             // a shower is falling now
    public double Drying;             // above 0: a shower is drying out
    public string Dirt = "normal";    // "blue", "normal" or "tacky"
}

public static class WeatherGrip
{
    public static readonly DirtState Blue = new DirtState { Name = "blue", Grip = 0.92, RearMu = 0.86, Accel = 1 };
    public static readonly DirtState Normal = new DirtState { Name = "normal", Grip = 1, RearMu = 1, Accel = 1 };
    public static readonly DirtState Tacky = new DirtState { Name = "tacky", Grip = 1.07, RearMu = 1, Accel = 0.9 };

    public static DirtState DirtStateOf(WeatherState w)
    {
        if (w == null) return Normal;
        switch (w.Dirt)
        {
            case "blue": return Blue;
            case "tacky": return Tacky;
            default: return Normal;
        }
    }

    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    // grip for the track surface: time of day, rain and drying, and the dirt state on dirt
    public static double Grip(double frac, WeatherState w, string surface)
    {
        if (w == null) return 1;
        double temp = w.Tod == "day" ? 0.98 : w.Tod == "night" ? 1.02 : 1;
        double wet = 1;
        if (w.Wx == "wet") wet = 0.84 + 0.16 * Clamp(frac * 1.6, 0, 1);   // a heavy dirt track comes in as it dries
        if (w.Wx == "rain") wet = 0.84;                                    // racing in the rain
        if (w.Wx == "shower" && w.ShowerOn) wet = 0.86;
        if (w.Wx == "shower" && w.Drying > 0) wet = 0.94;
        return temp * wet * (surface == "dirt" ? DirtStateOf(w).Grip : 1);
    }
}
