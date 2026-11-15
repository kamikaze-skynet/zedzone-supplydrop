using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SupplyDrop
{
    // A looping helicopter sound made in code: blade slaps (filtered noise bursts with a low thump)
    // about 6 times a second over a turbine whine and wind. One second long so it loops cleanly.
    internal static class RotorSound
    {
        static AudioClip _clip;
        static bool _failed;

        internal static AudioClip Clip()
        {
            if (Art.Alive(_clip)) return _clip;
            if (_failed) return null;
            try
            {
                const int rate = 22050;
                int n = rate;          // 1 s
                const int slaps = 6;   // per second
                var data = new float[n];
                var rng = new System.Random(1234);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < n; i++)
                {
                    double t = (double)i / rate;
                    double phase = (t * slaps) % 1.0;           // 0..1 within one blade pass
                    double env = Math.Exp(-phase * 14.0);        // sharp attack, quick decay
                    float white = (float)(rng.NextDouble() * 2 - 1);
                    lp += (white - lp) * 0.18f;                  // low-passed noise = "whump"
                    lp2 += (white - lp2) * 0.04f;                // darker noise = wind
                    double thump = Math.Sin(2 * Math.PI * 55 * phase / slaps) * Math.Exp(-phase * 9.0);
                    double whine = 0.05 * Math.Sin(2 * Math.PI * 420 * t) + 0.03 * Math.Sin(2 * Math.PI * 840 * t);
                    double s = lp * env * 1.6 + thump * 0.55 + lp2 * 0.35 + whine;
                    data[i] = (float)Math.Max(-1, Math.Min(1, s * 0.8));
                }
                // fade the loop seam over 10 ms
                int f = rate / 100;
                for (int i = 0; i < f; i++) { float k = (float)i / f; data[i] = data[i] * k + data[n - f + i] * (1 - k); }

                _clip = AudioClip.Create("SupplyDrop.Rotor", n, 1, rate, false);
                _clip.SetData(new Il2CppStructArray<float>(data), 0);
                UnityEngine.Object.DontDestroyOnLoad(_clip);
                _clip.hideFlags = HideFlags.HideAndDontSave;
                return _clip;
            }
            catch (Exception e) { _failed = true; SLog.Error("Couldn't make the rotor sound; the helicopter will be silent", e); return null; }
        }

        // Route through the game's own sound mixer so the game's volume slider applies.
        internal static void UseGameMixer(AudioSource src)
        {
            try
            {
                var ac = AudioController.instance;
                if (ac == null) return;
                foreach (var s in ac.GetComponentsInChildren<AudioSource>(true))
                {
                    if (s != null && s.outputAudioMixerGroup != null) { src.outputAudioMixerGroup = s.outputAudioMixerGroup; return; }
                }
            }
            catch (Exception e) { SLog.Debug("No game mixer for the rotor sound: " + e.Message); }
        }
    }
}
