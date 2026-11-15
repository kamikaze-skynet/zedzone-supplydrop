using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SupplyDrop
{
    // All sprites are drawn in code (top-down pixel art), so the mod ships as a single DLL.
    internal static class Art
    {
        static Sprite _body, _rotor, _tailRotor, _shadow, _arrow;
        static bool _failed;

        internal const int BodyW = 60, BodyH = 26;

        internal static Sprite Body => Ready() ? _body : null;
        internal static Sprite Rotor => Ready() ? _rotor : null;
        internal static Sprite TailRotor => Ready() ? _tailRotor : null;
        internal static Sprite Shadow => Ready() ? _shadow : null;
        internal static Sprite ArrowSprite => Ready() ? _arrow : null;

        internal static bool Alive(UnityEngine.Object o) { try { return o != null && o.Pointer != IntPtr.Zero && o; } catch { return false; } }

        static bool Ready()
        {
            if (_failed) return false;
            if (Alive(_body) && Alive(_rotor) && Alive(_tailRotor) && Alive(_shadow) && Alive(_arrow)) return true;
            try
            {
                var body = BodyPixels();
                _body = Make(body, BodyW, BodyH, new Vector2(0.62f, 0.5f), 16f);
                _shadow = Make(ShadowPixels(body), BodyW, BodyH, new Vector2(0.62f, 0.5f), 16f);
                _rotor = Make(RotorPixels(72, 3), 72, 72, new Vector2(0.5f, 0.5f), 16f);
                _tailRotor = Make(RotorPixels(14, 1), 14, 14, new Vector2(0.5f, 0.5f), 16f);
                _arrow = Make(ArrowPixels(), 16, 16, new Vector2(0.5f, 0.5f), 16f);
                return true;
            }
            catch (Exception e) { _failed = true; SLog.Error("Couldn't draw the helicopter sprites; no helicopter or arrow will show", e); return false; }
        }

        static Sprite Make(Color32[] px, int w, int h, Vector2 pivot, float ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(new Il2CppStructArray<Color32>(px));
            tex.Apply(false, false);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect, Vector4.zero);
            UnityEngine.Object.DontDestroyOnLoad(s);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Outline = new Color32(30, 34, 22, 255);
        static readonly Color32 Olive = new Color32(84, 94, 58, 255);
        static readonly Color32 OliveDark = new Color32(62, 70, 42, 255);
        static readonly Color32 OliveLight = new Color32(112, 124, 80, 255);
        static readonly Color32 Glass = new Color32(120, 170, 190, 255);
        static readonly Color32 GlassShine = new Color32(200, 230, 240, 255);
        static readonly Color32 Skid = new Color32(48, 50, 50, 255);
        static readonly Color32 Stripe = new Color32(220, 180, 40, 255);
        static readonly Color32 Lamp = new Color32(230, 40, 30, 255);

        // Top-down view, nose pointing right (+x). Width 60, height 26, centre line y = 13.
        static Color32[] BodyPixels()
        {
            int w = BodyW, h = BodyH;
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;
            float cy = 12.5f;
            void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c; }

            // skids (drawn first, the body covers their middle)
            for (int x = 28; x <= 55; x++) { Set(x, 2, Skid); Set(x, 23, Skid); }
            for (int k = 0; k < 2; k++) { int x = 33 + k * 14; for (int y = 2; y <= 23; y++) Set(x, y, Skid); }
            Set(56, 3, Skid); Set(56, 22, Skid);

            // tail boom
            for (int x = 2; x <= 32; x++)
            {
                float half = 1.5f + (x - 2) * 0.05f;
                for (int y = 0; y < h; y++)
                {
                    float d = Math.Abs(y - cy);
                    if (d <= half + 1f) Set(x, y, d > half ? Outline : (y > cy ? OliveLight : OliveDark));
                }
            }
            // horizontal stabiliser
            for (int x = 7; x <= 11; x++) for (int y = 5; y <= 20; y++) Set(x, y, (y == 5 || y == 20 || x == 7 || x == 11) ? Outline : Olive);
            // tail fin (seen from above: a thin dark strip)
            for (int x = 1; x <= 6; x++) { Set(x, 12, Outline); Set(x, 13, Outline); }

            // fuselage: an ellipse
            float ex = 41f, rx = 16.5f, ry = 9.5f;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float nx = (x + 0.5f - ex) / rx, ny = (y + 0.5f - cy - 0.5f) / ry;
                    float r = nx * nx + ny * ny;
                    if (r > 1f) continue;
                    Color32 c = r > 0.82f ? Outline : (ny > 0.35f ? OliveLight : (ny < -0.45f ? OliveDark : Olive));
                    Set(x, y, c);
                }
            // engine housing on top (a darker block behind the rotor hub) and exhausts
            for (int x = 33; x <= 41; x++) for (int y = 9; y <= 16; y++) Set(x, y, (x == 33 || y == 9 || y == 16) ? Outline : OliveDark);
            Set(32, 9, Skid); Set(32, 16, Skid);
            // yellow supply stripe around the cabin
            for (int y = 0; y < h; y++) for (int x = 44; x <= 45; x++) if (px[y * w + x].a > 0 && !Same(px[y * w + x], Outline)) Set(x, y, Stripe);
            // cockpit glass at the nose
            float gx = 52f, grx = 5.5f, gry = 7f;
            for (int x = 46; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float nx = (x + 0.5f - gx) / grx, ny = (y + 0.5f - cy - 0.5f) / gry;
                    float r = nx * nx + ny * ny;
                    if (r > 1f || px[y * w + x].a == 0 || Same(px[y * w + x], Outline)) continue;
                    Set(x, y, (ny > 0.2f && nx < 0.3f) ? GlassShine : Glass);
                }
            // tail lamp
            Set(2, 12, Lamp);
            return px;
        }

        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        static Color32[] ShadowPixels(Color32[] body)
        {
            var px = new Color32[body.Length];
            for (int i = 0; i < body.Length; i++) px[i] = body[i].a > 0 ? new Color32(0, 0, 0, 255) : Clear;
            return px;
        }

        // Blades from the hub; the faint disc is the blur of the spinning blades.
        static Color32[] RotorPixels(int n, int blades)
        {
            var px = new Color32[n * n];
            float c = (n - 1) / 2f, R = n / 2f - 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - c, dy = y - c, d = (float)Math.Sqrt(dx * dx + dy * dy);
                    Color32 col = Clear;
                    if (d <= R) col = new Color32(20, 20, 20, 34);
                    if (d <= R)
                    {
                        for (int b = 0; b < (blades <= 1 ? 2 : blades * 2); b++)
                        {
                            double a = Math.PI * b / (blades <= 1 ? 2 : blades);
                            double ax = Math.Cos(a), ay = Math.Sin(a);
                            double along = dx * ax + dy * ay, across = Math.Abs(-dx * ay + dy * ax);
                            if (along > 0 && across <= (n > 20 ? 1.3 : 0.8)) col = new Color32(26, 26, 26, 210);
                        }
                    }
                    if (d <= (n > 20 ? 3f : 1.2f)) col = new Color32(60, 60, 60, 255);
                    px[y * n + x] = col;
                }
            return px;
        }

        // A chevron pointing right (+x).
        static Color32[] ArrowPixels()
        {
            int n = 16;
            var px = new Color32[n * n];
            var fill = new Color32(255, 205, 50, 255);
            var edge = new Color32(40, 30, 10, 255);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fy = Math.Abs(y - 7.5f);
                    // a filled triangle with a notch at the back
                    bool inside = x >= 2 && x <= 14 && fy <= (14 - x) * 0.62f + 0.5f && x >= 2 + fy * 0.55f;
                    bool inner = x >= 4 && x <= 12 && fy <= (12 - x) * 0.62f && x >= 4 + fy * 0.55f;
                    px[y * n + x] = inner ? fill : (inside ? edge : Clear);
                }
            return px;
        }
    }
}
