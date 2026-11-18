using System;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SupplyDrop
{
    // The UH-1Y renders from the Helicopter mod's Blender model (zedzone-helicopter/blender/UH1Y.blend),
    // embedded in this DLL. Top-down, nose pointing right; the main rotor mast is at the image centre.
    // Same scale as the Helicopter mod (166 px per world unit, drawn at 1.3x) so both look alike.
    // If loading fails the code-drawn helicopter in Art.cs is used instead.
    internal static class UH1Y
    {
        internal const float PixelsPerUnit = 166f;
        internal const float DrawScale = 1.3f;
        static Sprite _body, _rotor;
        static bool _tried;

        internal static Sprite Body => Ready() ? _body : null;
        internal static Sprite Rotor => Ready() ? _rotor : null;

        internal static bool Ready()
        {
            if (Art.Alive(_body) && Art.Alive(_rotor)) return true;
            if (_tried) return false;
            _tried = true;
            try
            {
                _body = Load("uh1y_body.png");
                _rotor = Load("uh1y_rotor.png");
                SLog.Info($"Using the UH-1Y helicopter art ({_body.texture.width}x{_body.texture.height}).");
                return true;
            }
            catch (Exception e) { SLog.Warn("Couldn't load the UH-1Y art (" + e.Message + "); using the simple helicopter."); _body = _rotor = null; return false; }
        }

        static Sprite Load(string file)
        {
            string res = "SupplyDrop.Art." + file;
            byte[] bytes;
            using (var s = typeof(UH1Y).Assembly.GetManifestResourceStream(res))
            {
                if (s == null) throw new FileNotFoundException("missing resource " + res);
                using (var ms = new MemoryStream()) { s.CopyTo(ms); bytes = ms.ToArray(); }
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, new Il2CppStructArray<byte>(bytes))) throw new InvalidDataException("bad image " + file);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            UnityEngine.Object.DontDestroyOnLoad(tex);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero);
            UnityEngine.Object.DontDestroyOnLoad(sp);
            sp.hideFlags = HideFlags.HideAndDontSave;
            return sp;
        }
    }
}
