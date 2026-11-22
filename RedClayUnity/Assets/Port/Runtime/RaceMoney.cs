// Port of the race money rules in src/logic.js (endRace) and the purse table in src/data.js: what a finishing position
// pays, and what a sponsor pays and bonuses for one race, including the rule for dropping a sponsor after missed goals.
// The rest of the settlement (associates, crew, repairs, tyre wear, travel, news) is not in this file yet.
using System;

public static class RaceMoney
{
    static readonly double[] PurseShape = { 1, 0.75, 0.6, 0.5, 0.42, 0.37, 0.33, 0.3, 0.28, 0.26, 0.24, 0.22 };

    static double JsRound(double v) => Math.Floor(v + 0.5);

    // the purse for a finishing position (1 = winner) in a division: the win purse scaled down the order
    public static double PurseFor(int tier, int position, double payMultiplier = 1)
    {
        double winPurse = Divisions.WinPurse[tier];
        return JsRound(winPurse * PurseShape[position - 1] / 10) * 10 * payMultiplier;
    }

    public sealed class SponsorRace
    {
        public double Pay, Bonus;     // money this race (already scaled by the hospitality suite)
        public bool Met;              // the goal was met, so the bonus is paid
        public int Misses;            // missed goals in a row afterwards
        public bool Dropped;          // the sponsor walks away after too many misses
    }

    // a sponsor's goal: beat a rival (rivalPosition > 0), or finish in the top N. rivalPosition is 0 when there is none.
    public static SponsorRace SponsorResult(double pay, double bonus, int patience, int misses, int position,
                                            int topN, int rivalPosition, bool beatsRival, int hospLevel)
    {
        bool met = beatsRival ? (rivalPosition == 0 || position < rivalPosition) : position <= topN;
        var r = new SponsorRace { Pay = Hosp(pay, hospLevel), Met = met, Misses = misses };
        if (met)
        {
            r.Bonus = Hosp(bonus, hospLevel);
            r.Misses = 0;
        }
        else
        {
            r.Misses = misses + 1;
            if (r.Misses >= patience) r.Dropped = true;
        }
        return r;
    }

    // src/logic.js hosp(): sponsor payments and bonuses go up 15% per level of the hospitality suite
    static double Hosp(double v, int hospLevel) => JsRound(v * (1 + 0.15 * hospLevel) / 10) * 10;
}
