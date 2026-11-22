// Port of the vehicle dynamics in src/core.js: tyres, grip, steering, power, drag, weight transfer and the wall
// impact. Race-state effects (weather, dirt state, skills, the player's assists) come in through PhysicsEnv and CarState,
// so this file has no game state of its own. Operation order follows the JavaScript exactly.
using System;

public sealed class PhysicsEnv
{
    public TrackSpace Track;
    public string Surface = "dirt";      // "dirt" or "asphalt"
    public bool Road;                    // road course or street layout
    public double Bank;                  // banking, as the track defines it
    public double SurfGrip = 1;          // the track's grip factor
    public double WxGrip = 1;            // weather grip
    public bool Kart;                    // the kart division: its own wheelbase and downforce
    public int Tier;                     // division index (0 = Junior Apex ... 5 = Apex Cup)
    public double DirtRearMu = 1;        // dirt state: rear traction (1 = normal clay)
    public bool Rain;                    // wet track: worn tyres lose grip
    public bool AssistOff;               // the player's stability assist is switched off
    public bool Fig8;                    // the figure-eight: its two halves cross in the middle
    public double GrooveCenter = -24;    // set by SetGroove
    public double TrackEvo;              // how far the track has evolved (dries out or rubbers in)
    public bool Red;                     // red flag: the cars are held
    public Action<CarState, double, int, double, double, double, double> OnWall;   // (car, speed, side, along, slide, px, py)
}

public sealed class CarState
{
    public double X, Y, Vx, Vy, H, R, Steer, SteerTarget, Throttle, Brake;
    public double MaxSpeed = 255, Accel = 170, GripMul = 1, Ax;
    public double Tw, TwMul = 1, Toe, RearGrip = 1;
    public double Draft, SkillDraft, RearHit, DirtyAir, TireLife, SkillTire;
    public double Spin, Bump, S, LastS, Lat, Prog, Beta, Slip, VF;
    public bool SpinKick, Out, IsPlayer, Hired = true, Finished;
    public double PushT;          // bump-draft push time left (seconds)
    public double Skill = 1;      // driver skill (AI): scales the corner speed it can carry
    public double Margin = 1;     // driver persona: how much grip margin the driver leaves in hand
    // AI driver state
    public double LaneOff, LaneNudge, DodgeT, DodgeLane, StuckT, Recover, Bobble, BobbleDir;
    public double Follow = 30;    // persona: how close the driver will follow a slower car
    public double Twitch;         // persona: how much the wheel wanders
    public bool Grudge, Team;

    // pack state (downforce classes)
    public double CheckT, RippleAt, PackBrake;
    public bool CheckArmed, Desperate;
    public CarState RippleFrom;
    public int Pit;
    public CarSpec Spec;
}

public sealed class CarSpec
{
    public double L, A, B, HL, Iz, Ap, C, RearMu, DfFrac;
}

public static class CarPhysics
{
    public const double G = 430;                       // the game's gravity: grip and weight transfer are measured against it
    public const double BASE_LAT = 430;
    const double PI = Math.PI;
    const double TW_DIST = 2.6e-5, TW_SLIP = 1.1e-4, TW_GRIP = 0.2;
    const double C_ASPHALT = 1.62, C_DIRT = 1.32;
    static readonly double[] DfByTier = { 0, 0, 0.04, 0.08, 0.15, 0.24 };

    static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
    static double WrapAngle(double a) => TrackGeometry.WrapAngle(a);

    // ----- grip -----
    public static double TireGrip(CarState c) => 1 - TW_GRIP * c.Tw;

    // src/surface.js: worn tyres on a wet track lose up to 30% of their grip
    public static double WetWornFactor(CarState c, PhysicsEnv env)
    {
        if (!env.Rain) return 1;
        return c.Tw > 0.4 ? 1 - 0.3 * Math.Min(1, (c.Tw - 0.4) / 0.6) : 1;
    }

    // src/core.js setGroove: the grip groove moves up the track as the surface changes
    public static void SetGroove(PhysicsEnv env, double frac)
    {
        env.TrackEvo = Clamp(frac, 0, 1);
        if (env.Surface == "asphalt")
        {
            env.GrooveCenter = env.Road ? 0 : -env.Track.W * 0.3;
            return;
        }
        env.GrooveCenter = env.Track.W * (-0.46 + 0.92 * Clamp(frac, 0, 1));
    }

    public static double GripAt(PhysicsEnv env, double lat)
    {
        double W = env.Track.W;
        double bank = 1 + env.Bank * 0.011;
        if (env.Surface == "asphalt")
        {
            bool marbles = env.Road ? Math.Abs(lat) > W * 0.6 : lat > W * 0.45;
            double gz = (lat - env.GrooveCenter) / 26;
            return (0.9 + (0.12 + 0.05 * env.TrackEvo) * Math.Exp(-(gz * gz)) - (marbles ? 0.12 : 0)) * bank * env.SurfGrip * env.WxGrip;
        }
        double gw = Math.Max(14, W * 0.38);
        double cushion = lat > W * 0.7 ? 0.08 * Clamp((lat - W * 0.7) / (W * 0.2), 0, 1) : 0;
        double gg = (lat - env.GrooveCenter) / gw;
        return (0.8 + 0.32 * Math.Exp(-(gg * gg)) + cushion) * (1 - 0.07 * env.TrackEvo) * bank * env.SurfGrip * env.WxGrip;
    }

    // ----- tyres and steering -----
    public static double TireCurve(double alpha, double ap, double C)
    {
        if (C == 0) C = C_ASPHALT;
        double B = Math.Tan(Math.PI / (2 * C)) / ap;
        return Math.Sin(C * Math.Atan(B * alpha));
    }

    // how far the wheel can turn: less at speed, as in a real car
    public static double SteerLimit(double v)
    {
        return 0.42 * 70 / (70 + 0.32 * v);
    }

    public static CarSpec SpecFor(PhysicsEnv env)
    {
        bool kart = env.Kart;
        double L = kart ? 12 : 17, a = L * (kart ? 0.55 : 0.52), b = L - a;
        bool dirt = env.Surface == "dirt";
        return new CarSpec
        {
            L = L, A = a, B = b, HL = kart ? 0.10 : 0.17, Iz = a * b * (kart ? 0.8 : 1.0),
            Ap = dirt ? 0.19 : (kart ? 0.12 : 0.10),
            C = dirt ? C_DIRT : C_ASPHALT,
            RearMu = dirt ? 0.92 * env.DirtRearMu : 1.0,
            DfFrac = kart ? 0 : (env.Tier >= 0 && env.Tier < DfByTier.Length ? DfByTier[env.Tier] : 0),
        };
    }

    // ----- the car update -----
    public static void StepCar(CarState c, double dt, PhysicsEnv env)
    {
        if (env.Red)
        {
            double k = Math.Exp(-5 * dt);
            c.Vx *= k; c.Vy *= k; c.R = 0; c.Throttle = 0; c.Brake = 1;
            return;
        }
        StepHalf(c, dt / 2, env);
        StepHalf(c, dt / 2, env);
    }

    static void StepHalf(CarState c, double dt, PhysicsEnv env)
    {
        if (c.Out || c.Pit > 0) return;
        var P = c.Spec ?? (c.Spec = SpecFor(env));
        double fx = Math.Cos(c.H), fy = Math.Sin(c.H), rx = -fy, ry = fx;
        double vF = c.Vx * fx + c.Vy * fy, vR = c.Vx * rx + c.Vy * ry;
        double r = c.R;
        var ti = env.Track.Info(c.X, c.Y, c.S);
        double v = Math.Sqrt(vF * vF + vR * vR);
        double mu = GripAt(env, ti.Lat) * c.GripMul * TireGrip(c) * WetWornFactor(c, env);

        // steering: the wheel turns at a steering-rack speed toward the driver's input
        double dMax = SteerLimit(v) , want = Clamp(c.SteerTarget, -1, 1) * dMax;
        c.Steer += Clamp(want - c.Steer, -2.6 * dt, 2.6 * dt);
        double d = c.Steer + c.Toe;

        // power, drag and downforce
        double top = Math.Max(60, c.MaxSpeed), power = 0.2857 * c.Accel * top;
        double drag = power / (top * top * top) * (1 - 0.148 * Math.Min(1, c.Draft * (1 + c.SkillDraft))) * (1 + 0.05 * c.RearHit);
        double down = P.DfFrac * G / (top * top) * v * v;

        // weight on each axle, moved by last step's acceleration
        double ax0 = c.Ax, Ftot = G + down;
        double Fzf = Math.Max(0.15 * Ftot, Ftot * P.B / P.L - P.HL * ax0);
        double Fzr = Math.Max(0.15 * Ftot, Ftot - Fzf);
        double muF = mu * (1 - 0.22 * c.DirtyAir);
        double muR = mu * P.RearMu * c.RearGrip * (1 + 0);   // restart grip (skills) is not in this port yet

        // driving and braking force, each axle limited to its grip
        double drive = c.Throttle * Math.Min(c.Accel * 1.3, power / Math.Max(Math.Abs(vF), 25));
        double sgn = vF >= 0 ? 1 : -1;
        double brakeA = c.Brake * mu * Ftot * (env.Surface == "dirt" ? 0.78 : 0.92);
        double Fxf = -sgn * brakeA * 0.6, Fxr = drive - sgn * brakeA * 0.4;
        if (c.Brake != 0 && vF < 4) { Fxf = 0; Fxr = vF > -22 ? -c.Brake * 70 : 0; }
        Fxf = Clamp(Fxf, -muF * Fzf, muF * Fzf);
        Fxr = Clamp(Fxr, -muR * Fzr, muR * Fzr);
        double capF = Math.Sqrt(Math.Max(0, (muF * Fzf) * (muF * Fzf) - Fxf * Fxf));
        double capR = Math.Sqrt(Math.Max(0, (muR * Fzr) * (muR * Fzr) - Fxr * Fxr));

        // slip angles and sideways tyre forces
        double vxs = Math.Max(Math.Abs(vF), 10);
        double af = Math.Atan2(vR + P.A * r, vxs) - d * sgn;
        double ar = Math.Atan2(vR - P.B * r, vxs);
        double Fyf = -capF * TireCurve(af, P.Ap, P.C), Fyr = -capR * TireCurve(ar, P.Ap, P.C);

        // the player's stability assist (only for real oversteer, never while scraping a wall)
        double esc = 0;
        if (c.IsPlayer && !c.Hired && !env.AssistOff && v > 30 && !(c.Bump > 0)
            && Math.Abs(ar) > P.Ap * 1.25 && Math.Abs(ar) > Math.Abs(af))
        {
            if (c.Throttle != 0) { double cut = 0.5; Fxr *= cut; drive *= cut; }
            esc = -Clamp(r - vF * Math.Tan(d) / P.L, -2, 2) * 1.2;
        }

        // accelerations in the car's own frame
        double roll = 6 + 0.12 * Math.Abs(vF) + (c.Throttle != 0 ? 0 : 0.18 * Math.Abs(vF));
        double axb = Fxf * Math.Cos(d) - Fyf * Math.Sin(d) + Fxr - sgn * (drag * vF * vF + roll);
        double ayb = Fxf * Math.Sin(d) + Fyf * Math.Cos(d) + Fyr;
        double yawAcc = (P.A * (Fyf * Math.Cos(d) + Fxf * Math.Sin(d)) - P.B * Fyr) / P.Iz + esc;
        if (env.Surface == "dirt" && v > 60)
        {
            double s0 = c.S, lt = c.Lat;
            double rough = Math.Sin(s0 * 0.19 + lt * 0.11) * Math.Sin(s0 * 0.047 - lt * 0.05);
            yawAcc += rough * Math.Min(1, v / 220) * (lt > env.Track.W * 0.7 ? 7 : 2.4);
        }
        vF += (axb + vR * r) * dt;
        vR += (ayb - vF * r) * dt;
        r += yawAcc * dt;

        // at walking pace the tyres just roll where they point
        if (v < 18)
        {
            double k = 1 - v / 18, rk = vF * Math.Tan(d) / P.L;
            r += (rk - r) * k;
            vR -= vR * k * 6 * dt;
        }

        // spins: a sudden shove to the car's rotation, then the physics play it out
        if (c.Spin != 0 && !c.SpinKick) { r += c.Spin * 0.38; c.SpinKick = true; }
        if (c.Spin != 0)
        {
            c.Spin *= Math.Exp(-1.8 * dt);
            if (Math.Abs(c.Spin) < 0.25) { c.Spin = 0; c.SpinKick = false; }
        }
        c.Ax = axb; c.R = r; c.H += r * dt;
        double slide = Math.Abs(vR) + Math.Max(0, Math.Abs(ar) - P.Ap) * v * 0.35;
        c.Slip = slide;
        c.Beta = v > 25 ? Math.Atan2(vR, Math.Max(Math.Abs(vF), 1)) : 0;
        double tl = c.TireLife > 0 ? Math.Min(0.4, c.TireLife) : 0;
        c.Tw = Math.Min(1, c.Tw + (Math.Abs(vF) * TW_DIST + slide * TW_SLIP) * c.TwMul * (1 - tl) * (1 - c.SkillTire) * dt);
        double fx2 = Math.Cos(c.H), fy2 = Math.Sin(c.H);
        c.Vx = fx2 * vF - fy2 * vR;
        c.Vy = fy2 * vF + fx2 * vR;
        c.X += c.Vx * dt;
        c.Y += c.Vy * dt;

        // walls: a real impact, landing on the bumper corner nearest the wall
        var t2 = env.Track.Info(c.X, c.Y, c.S);
        double W = env.Track.W;
        if (t2.Lat > W - 6 || t2.Lat < -W + 6)
        {
            double side = t2.Lat > 0 ? 1 : -1;
            var p = env.Track.PointAt(t2.S, side * (W - 6));
            c.X = p.X; c.Y = p.Y;
            double nx = t2.Nx * side, ny = t2.Ny * side;
            double hx = Math.Cos(c.H), hy = Math.Sin(c.H), along = hx * nx + hy * ny;
            double arm = (along >= 0 ? 1 : -1) * 12, rxw = hx * arm, ryw = hy * arm;
            var res = ImpulseOn(c, rxw, ryw, nx, ny, 0.28, 0.32, P.Iz * 1.4);
            if (res.HasValue)
            {
                c.Bump = 0.3;
                env.OnWall?.Invoke(c, res.Value.Vn, (int)side, along, res.Value.Vt, c.X + rxw, c.Y + ryw);
            }
        }
        var t3 = env.Track.Info(c.X, c.Y, t2.S);
        double dsv = t3.S - c.LastS;
        if (dsv < -env.Track.Total / 2) dsv += env.Track.Total;
        if (dsv > env.Track.Total / 2) dsv -= env.Track.Total;
        c.Prog += dsv; c.LastS = t3.S; c.S = t3.S; c.Lat = t3.Lat;
        c.VF = c.Vx * Math.Cos(c.H) + c.Vy * Math.Sin(c.H);
        if (c.Bump > 0) c.Bump -= dt;
    }

    struct ImpulseResult { public double Vn, Vt; }

    static ImpulseResult? ImpulseOn(CarState c, double rx, double ry, double nx, double ny, double e, double mu, double I)
    {
        double w = c.R, vcx = c.Vx - w * ry, vcy = c.Vy + w * rx;
        double vn = vcx * nx + vcy * ny;
        if (vn <= 0) return null;
        double rn = rx * ny - ry * nx, jn = (1 + e) * vn / (1 + rn * rn / I);
        c.Vx -= jn * nx; c.Vy -= jn * ny; c.R = w - jn * rn / I;
        double tx = -ny, ty = nx;
        double vt = (c.Vx - c.R * ry) * tx + (c.Vy + c.R * rx) * ty, rt = rx * ty - ry * tx;
        double jt = Clamp(vt / (1 + rt * rt / I), -mu * jn, mu * jn);
        c.Vx -= jt * tx; c.Vy -= jt * ty; c.R -= jt * rt / I;
        return new ImpulseResult { Vn = vn, Vt = Math.Abs(vt) };
    }
}
