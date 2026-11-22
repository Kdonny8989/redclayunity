// Port of the race bills in src/logic.js: gas and hauling, tyre wear, pit-stop tyres, the wind-tunnel bill, interest on
// debt, and merchandise sales. Each is a pure function, so it can be checked against the web game exactly.
using System;

public static class RaceBills
{
    // the constants the bills use (src/logic.js)
    public static readonly double[] TireWear = { 40, 240 };                         // Junior Apex and Outlaw Dirt
    public static readonly double[] PitTires = { 30, 180, 800, 3000, 15000, 80000 };  // a set of tyres at each division
    public static readonly double[] Merch = { 60, 400, 1800, 8000, 50000, 300000 };   // at 100 fan buzz, per race
    public const double TunnelBill = 2500;                                          // Regional Feeder: per tech level

    static double JsRound(double v) => Math.Floor(v + 0.5);
    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    // gas and hauling; a guest ride on their hauler costs nothing
    public static double TravelBill(int tier, bool mc, bool guestRide, double travelFx)
    {
        double travelCut = 1 - Math.Min(0.5, travelFx);
        return guestRide && !mc ? 0 : JsRound(EconomyTables.Travel[tier] * (mc ? 1.5 : 1) * travelCut / 10) * 10;
    }

    // driving sideways eats tyres
    public static double TireWearBill(int tier, double wear) => JsRound(Clamp(wear / 4500, 0.15, 1) * TireWear[tier]);

    // a two-tyre stop is half a set
    public static double PitTireSets(int stops, int splits) => stops != 0 ? stops - 0.5 * splits : 0;

    public static double WindTunnelBill(double levels) => TunnelBill * levels;

    // 4% of the hole, when the bank is below zero
    public static double InterestOnDebt(double money, double net) =>
        money + net < 0 ? JsRound(-(money + net) * 0.04 / 10) * 10 : 0;

    // merchandise at 100 fan buzz, scaled by the fans
    public static double MerchSales(int tier, double fans) => JsRound(Merch[tier] * fans / 100 / 10) * 10;
}
