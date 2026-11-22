// GENERATED from port/data/game-data.json by the port tooling. Do not edit by hand.
// The division tech items, facility costs and economy tables, as the web game has them.
using System.Collections.Generic;

public sealed class TechItemDef { public string Key; public double[] Costs; public (string Stat, double Amount)[] Fx; }

public static class EconomyTables
{
    public static readonly TechItemDef[][] TierTech = new TechItemDef[][]
    {
        new TechItemDef[] {
            new TechItemDef { Key = "carb", Costs = new double[] {150.0,350.0,700.0}, Fx = new (string Stat, double Amount)[] { ("speed", 0.04), ("grip", -0.01) } },
            new TechItemDef { Key = "gear", Costs = new double[] {120.0,300.0,600.0}, Fx = new (string Stat, double Amount)[] { ("accel", 0.08), ("speed", -0.01) } },
            new TechItemDef { Key = "clutch", Costs = new double[] {200.0,450.0,850.0}, Fx = new (string Stat, double Amount)[] { ("accel", 0.04), ("slick", 0.02) } },
            new TechItemDef { Key = "frame", Costs = new double[] {260.0,520.0,980.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.02), ("repair", 0.1) } },
        },
        new TechItemDef[] {
            new TechItemDef { Key = "shock", Costs = new double[] {600.0,1400.0,2800.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.03), ("slick", 0.04) } },
            new TechItemDef { Key = "siping", Costs = new double[] {700.0,1600.0,3200.0}, Fx = new (string Stat, double Amount)[] { ("slick", 0.06), ("grip", 0.01) } },
            new TechItemDef { Key = "bar", Costs = new double[] {800.0,1800.0,3600.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.02), ("accel", 0.03) } },
            new TechItemDef { Key = "radiator", Costs = new double[] {900.0,2000.0,4000.0}, Fx = new (string Stat, double Amount)[] { ("speed", 0.03), ("repair", 0.05) } },
        },
        new TechItemDef[] {
            new TechItemDef { Key = "stagger", Costs = new double[] {2000.0,4500.0,8500.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.03), ("accel", 0.02) } },
            new TechItemDef { Key = "swaybar", Costs = new double[] {2500.0,5500.0,10000.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.02), ("speed", 0.01) } },
            new TechItemDef { Key = "rear", Costs = new double[] {3000.0,6000.0,11500.0}, Fx = new (string Stat, double Amount)[] { ("accel", 0.06), ("speed", -0.01) } },
            new TechItemDef { Key = "chassis", Costs = new double[] {3500.0,7000.0,13000.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.03), ("repair", 0.08) } },
        },
        new TechItemDef[] {
            new TechItemDef { Key = "aero", Costs = new double[] {9000.0,18000.0,32000.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.04), ("speed", -0.02) } },
            new TechItemDef { Key = "spec", Costs = new double[] {11000.0,22000.0,38000.0}, Fx = new (string Stat, double Amount)[] { ("speed", 0.04), ("accel", 0.03), ("repair", -0.05) } },
            new TechItemDef { Key = "nose", Costs = new double[] {8000.0,16000.0,29000.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.02), ("accel", 0.02) } },
            new TechItemDef { Key = "diffs", Costs = new double[] {9500.0,19000.0,33000.0}, Fx = new (string Stat, double Amount)[] { ("accel", 0.05) } },
        },
        new TechItemDef[] {
            new TechItemDef { Key = "pit", Costs = new double[] {40000.0,90000.0,160000.0}, Fx = new (string Stat, double Amount)[] { ("pitgain", 1.0) } },
            new TechItemDef { Key = "durable", Costs = new double[] {45000.0,100000.0,170000.0}, Fx = new (string Stat, double Amount)[] { ("repair", 0.12), ("dmgresist", 0.1) } },
            new TechItemDef { Key = "fuel", Costs = new double[] {42000.0,95000.0,165000.0}, Fx = new (string Stat, double Amount)[] { ("tirelife", 0.12) } },
            new TechItemDef { Key = "hauler", Costs = new double[] {50000.0,110000.0,180000.0}, Fx = new (string Stat, double Amount)[] { ("travel", 0.15), ("repair", 0.05) } },
        },
        new TechItemDef[] {
            new TechItemDef { Key = "telemetry", Costs = new double[] {1000000.0,2200000.0,4000000.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.02) } },
            new TechItemDef { Key = "tunnel", Costs = new double[] {1400000.0,2600000.0,4800000.0}, Fx = new (string Stat, double Amount)[] { ("speed", 0.03), ("grip", 0.02) } },
            new TechItemDef { Key = "cfd", Costs = new double[] {1100000.0,2300000.0,4300000.0}, Fx = new (string Stat, double Amount)[] { ("grip", 0.04) } },
            new TechItemDef { Key = "cells", Costs = new double[] {1300000.0,2700000.0,5100000.0}, Fx = new (string Stat, double Amount)[] { ("speed", 0.02), ("accel", 0.05), ("repair", 0.04) } },
        },
    };

    // facility key -> costs for levels 1 to 3, and the first division where it can be built
    public static readonly Dictionary<string, double[]> FacilityCosts = new Dictionary<string, double[]>
    {
        { "shop", new double[] {300.0,900.0,2000.0} },
        { "dev", new double[] {250.0,800.0,1800.0} },
        { "hosp", new double[] {350.0,1000.0,2200.0} },
        { "eng", new double[] {1000.0,2500.0,5000.0} },
    };

    public static readonly double[] Travel = {60.0,300.0,800.0,3000.0,15000.0,90000.0};
    public static readonly double[] Repair = {350.0,2200.0,8000.0,30000.0,150000.0,700000.0};
    public static readonly double[] DebtLimit = {800.0,6000.0,30000.0,150000.0,900000.0,6000000.0};
    public static readonly double[] Car2Cost = {600.0,5000.0,25000.0,120000.0,700000.0,4000000.0};
    public static readonly double[] Payroll = {0.0,0.0,0.0,0.0,18000.0,180000.0};
    public static readonly double[] AiTech = {0.005,0.005,0.015,0.025,0.035,0.05};
}
