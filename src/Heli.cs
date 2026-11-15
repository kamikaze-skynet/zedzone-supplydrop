using System;
using System.Collections.Generic;
using UnityEngine;

namespace SupplyDrop
{
    // The helicopter is only a visual: each player draws it locally on the same path
    // (straight line over the drop point, crossing it at the agreed time). The crate itself
    // is the game's own parachute supply pack, spawned by the host when it passes overhead.
    internal static class Heli
    {
        const float PathHalfLength = 75f;  // world units from the drop point to where it appears / vanishes
        const float Altitude = 5f;         // how far above its shadow the body is drawn

        sealed class Flight
        {
            internal GameObject Root, Body, Rotor, Tail, ShadowGo;
            internal AudioSource Audio;
            internal Vector2 Target, Dir;
            internal float OverheadAt, Speed, Scale;
        }

        static readonly List<Flight> _live = new List<Flight>();
        static readonly List<(Vector2 target, float heading, float overheadAt)> _pending = new List<(Vector2, float, float)>();
        static int _layer = int.MinValue;

        internal static void Clear()
        {
            foreach (var f in _live) Destroy(f);
            _live.Clear();
            _pending.Clear();
        }

        static void Destroy(Flight f)
        {
            try { if (Art.Alive(f.Root)) UnityEngine.Object.Destroy(f.Root); } catch { }
            try { if (Art.Alive(f.ShadowGo)) UnityEngine.Object.Destroy(f.ShadowGo); } catch { }
        }

        internal static int TopLayer()
        {
            if (_layer != int.MinValue) return _layer;
            _layer = 0;
            try
            {
                var layers = SortingLayer.layers;
                int best = int.MinValue;
                for (int i = 0; i < layers.Length; i++)
                {
                    int v = SortingLayer.GetLayerValueFromID(layers[i].id);
                    if (v > best) { best = v; _layer = layers[i].id; }
                }
                SLog.Debug($"Drawing on sorting layer {SortingLayer.IDToName(_layer)}");
            }
            catch (Exception e) { SLog.Debug("Sorting layers unavailable: " + e.Message); }
            return _layer;
        }

        static SpriteRenderer Part(string name, Transform parent, Sprite sprite, int order, Color color)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerID = TopLayer();
            sr.sortingOrder = order;
            return sr;
        }

        // headingDeg: direction of travel (0 = east, 90 = north). overheadAt: Time.time when it crosses target.
        internal static void Spawn(Vector2 target, float headingDeg, float overheadAt)
        {
            if (!Settings.Helicopter.Value) return;
            // wait out of sight until it's time to fly in
            if (overheadAt - Time.time > FlyInSeconds + 0.5f) { _pending.Add((target, headingDeg, overheadAt)); return; }
            var body = Art.Body;
            if (body == null) return;
            float rad = headingDeg * Mathf.Deg2Rad;
            var f = new Flight
            {
                Target = target,
                Dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)),
                OverheadAt = overheadAt,
                Speed = Settings.HeliSpeed.Value,
                Scale = Settings.HeliScale.Value,
            };
            try
            {
                f.Root = new GameObject("SupplyDrop.Helicopter");
                f.Root.transform.localScale = new Vector3(f.Scale, f.Scale, 1f);
                var bodySr = Part("Body", f.Root.transform, body, 32000, Color.white);
                f.Body = bodySr.gameObject;
                f.Body.transform.localRotation = Quaternion.Euler(0, 0, headingDeg);
                // main rotor sits over the engine (a bit behind the middle of the fuselage)
                var rotor = Part("Rotor", f.Body.transform, Art.Rotor, 32002, Color.white);
                f.Rotor = rotor.gameObject;
                f.Rotor.transform.localPosition = new Vector3((37f - Art.BodyW * 0.62f) / 16f, 0f, 0f);
                var tail = Part("TailRotor", f.Body.transform, Art.TailRotor, 32001, Color.white);
                f.Tail = tail.gameObject;
                f.Tail.transform.localPosition = new Vector3((3f - Art.BodyW * 0.62f) / 16f, 0.25f, 0f);

                var shadow = Part("SupplyDrop.HeliShadow", null, Art.Shadow, 31990, new Color(0f, 0f, 0f, 0.28f));
                f.ShadowGo = shadow.gameObject;
                f.ShadowGo.transform.localScale = new Vector3(f.Scale * 0.9f, f.Scale * 0.9f, 1f);
                f.ShadowGo.transform.localRotation = Quaternion.Euler(0, 0, headingDeg);

                var clip = RotorSound.Clip();
                if (clip != null && Settings.HeliVolume.Value > 0f)
                {
                    f.Audio = f.Root.AddComponent<AudioSource>();
                    f.Audio.clip = clip;
                    f.Audio.loop = true;
                    f.Audio.spatialBlend = 0f;
                    f.Audio.volume = 0f;
                    f.Audio.playOnAwake = false;
                    RotorSound.UseGameMixer(f.Audio);
                    f.Audio.Play();
                }
                Place(f, Time.time);
                _live.Add(f);
                SLog.Debug($"Helicopter spawned, heading {headingDeg:0} deg, over the drop in {overheadAt - Time.time:0.#} s");
            }
            catch (Exception e) { SLog.Error("Couldn't spawn the helicopter", e); Destroy(f); }
        }

        // Seconds the helicopter needs from where it appears to the drop point.
        internal static float FlyInSeconds => PathHalfLength / Mathf.Max(1f, Settings.HeliSpeed.Value);

        static void Place(Flight f, float now)
        {
            Vector2 ground = f.Target + f.Dir * (f.Speed * (now - f.OverheadAt));
            f.ShadowGo.transform.position = new Vector3(ground.x + 0.6f * f.Scale, ground.y - 0.3f * f.Scale, 0f);
            // a little bob so it doesn't look pasted on
            float bob = Mathf.Sin(now * 2.1f) * 0.08f;
            f.Root.transform.position = new Vector3(ground.x, ground.y + Altitude * f.Scale * 0.5f + bob, 0f);
        }

        internal static void Tick()
        {
            float now = Time.time;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                if (InGameController.instance == null) { _pending.RemoveAt(i); continue; }
                if (p.overheadAt - now <= FlyInSeconds + 0.5f) { _pending.RemoveAt(i); Spawn(p.target, p.heading, p.overheadAt); }
            }
            if (_live.Count == 0) return;
            Vector3 cam = Vector3.zero; bool haveCam = false;
            try { var c = Camera.main; if (c != null) { cam = c.transform.position; haveCam = true; } } catch { }
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var f = _live[i];
                bool gone = !Art.Alive(f.Root) || !Art.Alive(f.ShadowGo) || now > f.OverheadAt + PathHalfLength / Mathf.Max(1f, f.Speed) + 1f;
                if (gone || InGameController.instance == null) { Destroy(f); _live.RemoveAt(i); continue; }
                Place(f, now);
                f.Rotor.transform.Rotate(0f, 0f, -1500f * Time.deltaTime);
                f.Tail.transform.Rotate(0f, 0f, -2200f * Time.deltaTime);
                if (f.Audio != null && haveCam)
                {
                    float d = Vector2.Distance(new Vector2(cam.x, cam.y), (Vector2)f.ShadowGo.transform.position);
                    float k = Mathf.Clamp01(1f - d / 70f);
                    float fade = Mathf.Clamp01((now - (f.OverheadAt - PathHalfLength / f.Speed)) / 1.5f);
                    f.Audio.volume = Settings.HeliVolume.Value * k * k * fade;
                }
            }
        }
    }
}
