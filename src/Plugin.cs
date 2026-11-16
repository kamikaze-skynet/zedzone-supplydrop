using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SupplyDrop
{
    [BepInPlugin(Guid, "Supply Drop", Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "girthy_mods.zedzone.supplydrop";
        public const string Version = "1.1.0";

        public override void Load()
        {
            Settings.Bind(Config);
            SLog.Verbose = Settings.VerboseLog.Value;
            SLog.Open(Log, Version);
            SLog.Info(Settings.Describe());

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<SupplyHost>();
                var go = new GameObject("SupplyDrop.Host");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<SupplyHost>();
            }
            catch (Exception e) { SLog.Error("Could not create the host object; the mod is off", e); return; }

            try { new Harmony(Guid).PatchAll(typeof(ChatPatches)); SLog.Info("Co-op messages hooked."); }
            catch (Exception e) { SLog.Error("Hooking co-op chat failed; guests won't see the helicopter or the message", e); }

            KeyCheck.Run();
            SLog.Info($"Supply Drop {Version} loaded.");
        }
    }

    // Hidden object that runs the drop timer, the helicopter and the guide arrow.
    public class SupplyHost : MonoBehaviour
    {
        public SupplyHost(IntPtr ptr) : base(ptr) { }

        void Update()
        {
            try { Drops.Tick(); }
            catch (Exception e) { SLog.Error("Drop update failed", e); Drops.Abort(); }
            try { Net.Tick(); }
            catch (Exception e) { SLog.Error("Co-op update failed", e); }
            try { Heli.Tick(); }
            catch (Exception e) { SLog.Error("Helicopter update failed", e); Heli.Clear(); }
            try { Marker.Tick(); }
            catch (Exception e) { SLog.Error("Map marker update failed", e); }
            try { Arrow.Tick(); }
            catch (Exception e) { SLog.Error("Arrow update failed", e); Arrow.Clear(); }
        }
    }
}
