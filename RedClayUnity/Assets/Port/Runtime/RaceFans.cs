// Port of the fan buzz and the engine-claim rule in src/logic.js: the fan buzz kept between 0 and 100, the upkeep a big
// following pays, and when a claimer may take an engine (Outlaw Dirt, the top three).
using System;

public static class RaceFans
{
    static double JsRound(double v) => Math.Floor(v + 0.5);
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    // the fan buzz after a change, kept between 0 and 100
    public static double FansAfter(double fans, double n) => Clamp(JsRound(fans + n), 0, 100);

    // a big following takes upkeep above 50
    public static double FansUpkeep(double fans) => Math.Ceiling((fans - 50) / 25);

    // the claimer rule: in Outlaw Dirt, a top-three finisher may have the engine claimed (roll is a random number in [0, 1))
    public static bool EngineClaimed(int tier, int pos, double roll) => tier == 1 && pos <= 3 && roll < 0.3;
}
