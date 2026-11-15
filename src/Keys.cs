using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SupplyDrop
{
    // The game uses the new Input System; fall back to the legacy one if a key has no mapping.
    internal static class Keys
    {
        static bool _legacyBroken;

        internal static bool Down(KeyCode key)
        {
            var k = Map(key);
            try
            {
                var kb = Keyboard.current;
                if (k != Key.None && kb != null) return kb[k].wasPressedThisFrame;
            }
            catch { }
            if (_legacyBroken) return false;
            try { return Input.GetKeyDown(key); } catch { _legacyBroken = true; return false; }
        }

        static Key Map(KeyCode key)
        {
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return (Key)Enum.Parse(typeof(Key), "Digit" + (key - KeyCode.Alpha0));
            if (key == KeyCode.Return) return Key.Enter;
            if (key == KeyCode.BackQuote) return Key.Backquote;
            return Enum.TryParse(key.ToString(), out Key k) ? k : Key.None;
        }
    }
}
