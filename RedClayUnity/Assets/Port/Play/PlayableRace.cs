// A playable check of the ported physics: one Junior Apex dirt oval, driven with the keyboard.
// Arrow keys or WASD: up/W gas, down/S brake, left/right steer. R resets the car to the start.
// The physics is the ported CarPhysics, stepped at the game's 1/120 s, so the feel is the feel of the port.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayableRace : MonoBehaviour
{
    const double Dt = 1.0 / 120.0;
    const float Scale = 1f / 25f;       // game units to Unity units
    const int MaxStepsPerFrame = 12;

    TrackGeometry geo;
    TrackSpace track;
    PhysicsEnv env;
    CarState car;
    double acc;
    double elapsed;
    int lapsDone;
    Transform carView;
    Camera cam;
    GUIStyle hud;

    void Start()
    {
        // Junior Apex dirt oval: little-hill-dirt-track (ar 1.3, 0.125 miles, half width 38, banking 4)
        geo = TrackGeometry.Build("oval", 1.3, 0.125);
        track = new TrackSpace(geo, 38);
        env = new PhysicsEnv
        {
            Track = track, Surface = "dirt", Road = false, Bank = 4, SurfGrip = 1, WxGrip = 1,
            Kart = false, Tier = 0, DirtRearMu = 1,
        };
        CarPhysics.SetGroove(env, 0.5);

        car = new CarState { MaxSpeed = 210, Accel = 150, IsPlayer = true, Hired = false };
        DrawTrack();
        MakeCar();
        ResetCar();

        cam = Camera.main;
        if (cam == null) { cam = new GameObject("Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
        cam.orthographic = true;
        cam.orthographicSize = 8;
        cam.backgroundColor = new Color(0.36f, 0.27f, 0.2f);
        cam.transform.position = new Vector3(0, 0, -10);

        hud = new GUIStyle { fontSize = 28, normal = { textColor = Color.white } };
    }

    void DrawTrack()
    {
        var go = new GameObject("Track");
        CreateLine(go, "Centre", 0, new Color(0.9f, 0.85f, 0.7f), false);
        CreateLine(go, "Outside", 1, new Color(0.9f, 0.9f, 0.9f), true);
        CreateLine(go, "Inside", -1, new Color(0.9f, 0.9f, 0.9f), true);
    }

    void CreateLine(GameObject parent, string name, int side, Color color, bool edge)
    {
        var lr = new GameObject(name).AddComponent<LineRenderer>();
        lr.transform.SetParent(parent.transform, false);
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.widthMultiplier = edge ? 0.12f : 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        int n = geo.N;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            double s = i * geo.Ds;
            var p = track.PointAt(s, side * (edge ? 38 : 0));
            lr.SetPosition(i, new Vector3((float)p.X * Scale, (float)p.Y * Scale, 0));
        }
    }

    void MakeCar()
    {
        var go = new GameObject("Car");
        var sr = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        sr.color = new Color(1f, 0.48f, 0.1f);
        go.transform.localScale = new Vector3(17 * Scale, 9 * Scale, 1);   // the car: 17 by 9 game units
        carView = go.transform;
    }

    void ResetCar()
    {
        var p = track.PointAt(0, 0);
        car.X = p.X; car.Y = p.Y; car.H = p.H;
        car.Vx = 0; car.Vy = 0; car.R = 0; car.Steer = 0; car.Throttle = 0; car.Brake = 0;
        var hit = track.Info(car.X, car.Y, 0);
        car.S = hit.S; car.LastS = hit.S; car.Lat = hit.Lat; car.Prog = 0; car.Tw = 0; car.Spec = null;
        acc = 0; elapsed = 0; lapsDone = 0;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame) ResetCar();

        bool gas = kb != null && (kb.upArrowKey.isPressed || kb.wKey.isPressed);
        bool brake = kb != null && (kb.downArrowKey.isPressed || kb.sKey.isPressed);
        bool right = kb != null && (kb.rightArrowKey.isPressed || kb.dKey.isPressed);
        bool left = kb != null && (kb.leftArrowKey.isPressed || kb.aKey.isPressed);
        double steer = (right ? 1 : 0) - (left ? 1 : 0);

        acc += Time.deltaTime;
        int steps = 0;
        while (acc >= Dt && steps < MaxStepsPerFrame)
        {
            acc -= Dt;
            steps++;
            car.Throttle = gas ? 1 : 0;
            car.Brake = brake ? 1 : 0;
            car.SteerTarget = steer;
            CarPhysics.StepCar(car, Dt, env);
            elapsed += Dt;
            int laps = (int)Math.Floor(car.Prog / track.Total);
            if (laps > lapsDone) lapsDone = laps;
        }
        if (steps == MaxStepsPerFrame) acc = 0;

        carView.position = new Vector3((float)car.X * Scale, (float)car.Y * Scale, -0.1f);
        carView.rotation = Quaternion.Euler(0, 0, (float)(car.H * Mathf.Rad2Deg));
        cam.transform.position = new Vector3(carView.position.x, carView.position.y, -10);
    }

    void OnGUI()
    {
        double speed = Math.Sqrt(car.Vx * car.Vx + car.Vy * car.Vy);
        GUI.Label(new Rect(20, 16, 700, 40), $"speed {speed:0} game units/s    lap {lapsDone + 1}    time {elapsed:0.0} s", hud);
        GUI.Label(new Rect(20, 60, 900, 40), "Arrow keys / WASD: drive    R: reset", hud);
    }
}
