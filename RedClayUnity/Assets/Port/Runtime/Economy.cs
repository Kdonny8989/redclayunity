// Port of the team economy in src/logic.js: what upgrades cost, what the hospitality suite pays, what a repair costs, and
// the team's bonus from its tech and facilities. Spec and manufacturer-deal effects come from other systems and are
// passed in as values (zero when there are none).
using System;
using System.Collections.Generic;

public sealed class TeamFx
{
    public double Speed, Accel, Grip, Repair, Slick, PitGain, DmgResist, TireLife, Travel;
}

public static class Economy
{
    // JavaScript's Math.round (halves go up)
    static double JsRound(double v) => Math.Floor(v + 0.5);

    // upgrades cost 12% less per level of the fabrication shop
    public static double TechCost(double c, int shopLevel) => JsRound(c * (1 - 0.12 * shopLevel) / 10) * 10;

    // sponsor payments and bonuses go up 15% per level of the hospitality suite
    public static double Hosp(double v, int hospLevel) => JsRound(v * (1 + 0.15 * hospLevel) / 10) * 10;

    // a full rebuild of a wrecked car, less the repair bonus (capped at 60%)
    public static double RepairCost(double dmg, int tier, double repairFx) =>
        JsRound(dmg * EconomyTables.Repair[tier] * (1 - Math.Min(0.6, repairFx)) / 10) * 10;

    // the team's bonus: the tech levels bought in this division, and the race engineering centre
    public static TeamFx TeamEffects(int tier, int[] techLevels, int engLevel, double specSpeed = 0, double empSpeed = 0, double empGrip = 0)
    {
        var o = new TeamFx();
        var items = EconomyTables.TierTech[tier];
        for (int i = 0; i < items.Length; i++)
        {
            double lv = techLevels[i];
            foreach (var (stat, amount) in items[i].Fx)
                Add(o, stat, amount * lv);
        }
        o.Speed += specSpeed;          // specialists (zero in this port)
        o.Speed += empSpeed;           // manufacturer deals (zero in this port)
        o.Grip += empGrip;
        o.Speed += 0.01 * engLevel;    // the race engineering centre
        o.Grip += 0.02 * engLevel;
        return o;
    }

    static void Add(TeamFx o, string stat, double amount)
    {
        switch (stat)
        {
            case "speed": o.Speed += amount; break;
            case "accel": o.Accel += amount; break;
            case "grip": o.Grip += amount; break;
            case "repair": o.Repair += amount; break;
            case "slick": o.Slick += amount; break;
            case "pitgain": o.PitGain += amount; break;
            case "dmgresist": o.DmgResist += amount; break;
            case "tirelife": o.TireLife += amount; break;
            case "travel": o.Travel += amount; break;
            default: throw new ArgumentException("Unknown team effect: " + stat);
        }
    }
}
