using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace SupplyDrop
{
    // At startup, look through the other mods' BepInEx configs (and the game's own fixed keys)
    // and warn in SupplyDrop.log if one of them uses the same key as CallDropKey.
    internal static class KeyCheck
    {
        static readonly Dictionary<KeyCode, string> GameKeys = new Dictionary<KeyCode, string>
        {
            { KeyCode.F5, "the game's quick save" },
            { KeyCode.F12, "Steam's screenshot key" },
            { KeyCode.Escape, "the game's menu" },
        };

        internal static void Run()
        {
            if (!Settings.AllowCallKey.Value) return;
            var mine = Settings.CallDropKey.Value;
            var clashes = new List<string>();
            if (GameKeys.TryGetValue(mine, out var g)) clashes.Add(g);
            try
            {
                foreach (var file in Directory.GetFiles(Paths.ConfigPath, "*.cfg"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.IndexOf("supplydrop", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    foreach (var raw in File.ReadAllLines(file))
                    {
                        var line = raw.Trim();
                        if (line.StartsWith("#") || line.StartsWith("[")) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim(), val = line.Substring(eq + 1).Trim();
                        if (key.IndexOf("key", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        if (string.Equals(val, mine.ToString(), StringComparison.OrdinalIgnoreCase))
                            clashes.Add($"{key} in {name}.cfg");
                    }
                }
            }
            catch (Exception e) { SLog.Debug("Key check skipped: " + e.Message); }
            if (clashes.Count == 0) SLog.Info($"Call-drop key {mine}: no other mod uses it.");
            else SLog.Warn($"Call-drop key {mine} is also {string.Join(", ", clashes)}. Change Keys.CallDropKey in girthy_mods.zedzone.supplydrop.cfg.");
        }
    }
}
