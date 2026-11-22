// Port of the pay rules in src/logic.js that the race settlement uses: a driver's salary, the hospitality-scaled pay of
// an associate sponsor, the crew chief's pay, and the charter loan payment. Each is a pure function.
using System;

public static class RacePay
{
    public const double CharterPaymentLimit = 300000;   // CHARTER_PAYMENT in src/logic.js: one race's loan payment

    static double JsRound(double v) => Math.Floor(v + 0.5);

    // a pay figure, scaled to the division
    public static double ScaledSalary(double baseSalary, double scale) => JsRound(baseSalary * scale / 10) * 10;

    // the hospitality suite raises a payment by 15% per level, rounded to the nearest 10
    public static double HospAt(double v, int level) => JsRound(v * (1 + 0.15 * level) / 10) * 10;

    // an associate sponsor's pay or bonus: scaled to the division, then raised by the hospitality suite
    public static double AssocAmount(double baseAmount, double scale, int hospLevel) =>
        HospAt(JsRound(baseAmount * scale / 10) * 10, hospLevel);

    // the crew chief's pay for one race
    public static double ChiefBill(double salary, double scale) => salary * scale;

    // the charter loan payment for one race: never more than is still owed
    public static double CharterPayment(double debt) => Math.Min(CharterPaymentLimit, debt);
}
