using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using LiteNetLib;
using UnityEngine;
using ZedZone.HordeMode;
using ZedZone.Net;
using V2List = Il2CppSystem.Collections.Generic.List<UnityEngine.Vector2>;
using ItemList = Il2CppSystem.Collections.Generic.List<RecipeItemData>;

namespace SupplyDrop
{
    // Host (or single player) side: the timer, picking the drop zone, dropping the crates when
    // the helicopter is overhead, and starting the beacon that draws the horde.
    //
    // Game systems used:
    //  - HordeSupplyDrop.DropPack: the game's parachute supply pack (falls from the sky with a
    //    blinking landing marker, lands as an openable pack item). Loot from HordeRewardTable
    //    and the flare-gun airdrop table (FlareGunAirDropConfigs).
    //  - HordeSupplyDrop.BroadcastDrop: tells guests to show the parachutes.
    //  - AlarmController.StartAlarmAtPosition: the game's alarm, which spawns zombies around a spot
    //    and sends them at it (same thing a car alarm or the flare gun does).
    internal static class Drops
    {
        sealed class Active
        {
            internal int Id;
            internal Vector2 Target;
            internal float Heading, OverheadAt, LandedAt = -1f;
            internal bool Dropped, AlarmStarted;
            internal string Who;
            internal List<Vector2> Points = new List<Vector2>();
        }

        static Active _active;
        static float _next = -1f;
        static int _ids;
        static readonly System.Random _rng = new System.Random();
        static IntPtr _world;

        internal static void Abort() { _active = null; _next = Time.time + 60f; }

        static bool IsAuthority
        {
            get { try { return NetSim.isSimulationAuthority; } catch { return !Net.IsGuest; } }
        }

        internal static void Tick()
        {
            var igc = InGameController.instance;
            if (igc == null) { _active = null; _next = -1f; _world = IntPtr.Zero; return; }
            if (igc.Pointer != _world) { _world = igc.Pointer; _active = null; _next = -1f; }
            if (!Settings.Enabled.Value || !IsAuthority) return;

            float now = Time.time;
            if (_next < 0f) { _next = now + Settings.FirstDropMinutes.Value * 60f; SLog.Info($"First supply drop in {Settings.FirstDropMinutes.Value:0.#} min."); }

            if (Settings.AllowCallKey.Value && Keys.Down(Settings.CallDropKey.Value))
            {
                if (_active != null) Notify("A supply drop is already on its way.");
                else { SLog.Info($"{Settings.CallDropKey.Value} pressed: calling a drop now."); TryBegin(true); }
            }

            if (_active == null)
            {
                if (now >= _next && Time.timeScale > 0f) TryBegin(false);
                return;
            }

            var a = _active;
            if (!a.Dropped && now >= a.OverheadAt) DropCrates(a);
            if (a.Dropped && a.LandedAt < 0f && now >= a.OverheadAt + ParachuteSeconds()) Landed(a);
            if (a.LandedAt >= 0f && !a.AlarmStarted && now >= a.LandedAt + Settings.ZombieDelaySeconds.Value) StartBeacon(a);
            if (a.AlarmStarted || (a.LandedAt >= 0f && !Settings.Zombies.Value)) { _active = null; ScheduleNext(); }
        }

        // a timing change (e.g. from Mod Menu) applies to the drop that's waiting, not just the one after
        internal static void WatchSettings()
        {
            Settings.IntervalMinutes.SettingChanged += (s, e) => Rescheduled();
            Settings.IntervalRandomMinutes.SettingChanged += (s, e) => Rescheduled();
        }

        static void Rescheduled()
        {
            if (_active == null && _next > 0f && InGameController.instance != null) ScheduleNext();
        }

        static void ScheduleNext()
        {
            float r = Settings.IntervalRandomMinutes.Value;
            float m = Mathf.Max(1f, Settings.IntervalMinutes.Value + (float)(_rng.NextDouble() * 2 - 1) * r);
            _next = Time.time + m * 60f;
            SLog.Info($"Next supply drop in {m:0.#} min.");
        }

        static float ParachuteSeconds()
        {
            try { float s = HordeSupplyParachute.dropSeconds; if (s > 0.2f && s < 60f) return s; } catch { }
            return 6f;
        }

        // ---- players

        internal static List<HumanCharacterController> Players()
        {
            var list = new List<HumanCharacterController>();
            try
            {
                foreach (var h in UnityEngine.Object.FindObjectsOfType<HumanCharacterController>())
                {
                    if (h == null || !h.isPlayer) continue;
                    var cd = h.characterData;
                    if (cd == null || cd.deadFlag) continue;
                    list.Add(h);
                }
            }
            catch (Exception e) { SLog.Warn("Listing players failed: " + e.Message); }
            return list;
        }

        static string NameOf(HumanCharacterController h)
        {
            try { var n = h.characterData?.characterName; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            return h.isNetPuppet ? "a teammate" : "you";
        }

        // ---- start a drop

        static void TryBegin(bool forced)
        {
            DirectorController dir = null;
            try { dir = DirectorController.instance; } catch { }
            if (!forced && dir != null)
            {
                bool ok = true;
                try { ok = dir.IsDirectorEligible(); } catch { }
                if (!ok) { _next = Time.time + 60f; SLog.Debug("Not a good time (game director says no: base, menu, cutscene...); trying again in 1 min."); return; }
            }

            var outside = Players().FindAll(p => { try { return p.characterData.zLevel == 0; } catch { return true; } });
            if (outside.Count == 0)
            {
                _next = Time.time + 60f;
                SLog.Info("Nobody is above ground; trying again in 1 min.");
                if (forced) Notify("Supply drop: nobody is outside right now.");
                return;
            }
            var who = outside[_rng.Next(outside.Count)];
            Vector2 at = who.transform.position;

            Vector2 target = at;
            for (int tries = 0; tries < 6; tries++)
            {
                float ang = (float)(_rng.NextDouble() * Math.PI * 2);
                float dist = Mathf.Lerp(Settings.DropDistanceMin.Value, Mathf.Max(Settings.DropDistanceMin.Value, Settings.DropDistanceMax.Value), (float)_rng.NextDouble());
                var raw = at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                try { target = HordeSupplyDrop.ResolveLandPoint(raw, 0); }
                catch (Exception e) { target = raw; SLog.Debug("ResolveLandPoint failed: " + e.Message); }
                if (!float.IsNaN(target.x) && (target - at).magnitude <= Settings.DropDistanceMax.Value * 1.6f) break;
            }

            var a = new Active
            {
                Id = ++_ids,
                Target = target,
                Heading = (float)(_rng.NextDouble() * 360.0),
                OverheadAt = Time.time + Settings.WarningSeconds.Value,
                Who = NameOf(who),
            };
            _active = a;
            SLog.Info($"Supply drop #{a.Id}: helicopter inbound, crate lands near {a.Who} at ({target.x:0.#}, {target.y:0.#}), {(target - at).magnitude:0.#} units from them, in {Settings.WarningSeconds.Value:0} s.");
            ShowInbound(a.Id, a.Target, a.Heading, a.OverheadAt - Time.time, true);
            Net.SendToGuests(InboundMsg(a));
        }

        static string InboundMsg(Active a) =>
            $"I|{a.Id}|{Net.F(a.Target.x)}|{Net.F(a.Target.y)}|{Net.F(a.Heading)}|{Net.F(Mathf.Max(0f, a.OverheadAt - Time.time))}";

        // A guest who joins (or says hello) during a drop still gets it.
        internal static void SendActiveTo(NetPeer peer)
        {
            var a = _active;
            if (a == null) return;
            if (!a.Dropped) Net.SendTo(peer, InboundMsg(a));
            else Net.SendTo(peer, $"L|{a.Id}|{Net.F(a.Target.x)}|{Net.F(a.Target.y)}");
        }

        // ---- the crates

        static void DropCrates(Active a)
        {
            a.Dropped = true;
            int players = Math.Max(1, Players().Count);
            int crates = Settings.CratePerPlayer.Value ? players : 1;
            crates = Mathf.Clamp(crates, 1, Settings.MaxCrates.Value);
            var collector = new V2List();
            int made = 0;
            for (int i = 0; i < crates; i++)
            {
                try
                {
                    var items = Loot(i);
                    if (items == null || items.Count == 0) { SLog.Warn($"Crate {i + 1}: loot table came back empty, skipped."); continue; }
                    Vector2 p = a.Target;
                    if (i > 0)
                    {
                        float ang = i * 2.4f;
                        p += new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.6f;
                    }
                    int before = collector.Count;
                    try { HordeSupplyDrop.DropPack(p, items, Settings.LootQuality.Value, collector, 0); }
                    catch (Exception e)
                    {
                        // fallback: build the pack ourselves and hang it on a parachute
                        SLog.Warn($"Crate {i + 1}: the game's drop call failed ({e.Message}); dropping the pack directly.");
                        Vector2 land = p;
                        try { land = HordeSupplyDrop.ResolveLandPoint(p, 0); } catch { }
                        var pack = HordeSupplyPack.Build(items, Settings.LootQuality.Value, null);
                        try { HordeSupplyParachute.Spawn(land, pack, 0); }
                        catch (Exception e2) { SLog.Warn($"Parachute failed too ({e2.Message}); putting the pack on the ground."); HordeSupplyPack.DropOnGround(pack, land, 0); }
                        collector.Add(land);
                    }
                    made++;
                    SLog.Info($"Crate {i + 1}/{crates}: {items.Count} loot entries{(collector.Count > before ? $", lands at ({collector[collector.Count - 1].x:0.#}, {collector[collector.Count - 1].y:0.#})" : "")}.");
                }
                catch (Exception e) { SLog.Error($"Dropping crate {i + 1} failed", e); }
            }
            for (int i = 0; i < collector.Count; i++) a.Points.Add(collector[i]);
            if (a.Points.Count > 0) a.Target = a.Points[0];
            if (made == 0) { Notify("The supply helicopter couldn't drop its cargo."); return; }

            if (Net.IsOnlineHost && collector.Count > 0)
            {
                try { HordeSupplyDrop.BroadcastDrop(collector, 0); SLog.Debug("Parachutes sent to guests."); }
                catch (Exception e) { SLog.Warn("Couldn't show the parachutes to guests: " + e.Message); }
            }
        }

        static ItemList Loot(int crate)
        {
            if (!string.Equals(Settings.LootMode.Value, "Tables", StringComparison.OrdinalIgnoreCase))
            {
                try { var r = RandomLoot.Make(); if (r != null && r.Count > 0) return r; }
                catch (Exception e) { SLog.Warn("Random loot failed (" + e.Message + "); using the game's tables."); }
            }
            int level = Mathf.Clamp(Settings.LootLevel.Value + _rng.Next(-1, 2), 1, 20);
            // first crate: the good one; the rest: a mix
            int kind = crate == 0 ? 0 : _rng.Next(4);
            ItemList items = null;
            try
            {
                switch (kind)
                {
                    case 0: items = HordeRewardTable.EliteBox(level); break;
                    case 1: items = HordeRewardTable.SpawnArmoryBox(); break;
                    case 2: items = HordeRewardTable.PersonalBox(level); break;
                    default: items = FlareLoot() ?? HordeRewardTable.PersonalBox(level); break;
                }
            }
            catch (Exception e) { SLog.Warn($"Loot table {kind} failed ({e.Message}); using the personal box."); }
            if (items == null || items.Count == 0)
            {
                try { items = HordeRewardTable.PersonalBox(level); } catch (Exception e) { SLog.Error("No loot table works", e); }
            }
            // the first crate also gets a few extras from the flare-gun airdrop table
            if (crate == 0 && items != null)
            {
                var extra = FlareLoot();
                if (extra != null) for (int i = 0; i < extra.Count; i++) items.Add(extra[i]);
            }
            SLog.Debug($"Crate {crate + 1}: loot kind {kind}, level {level}, {items?.Count ?? 0} entries");
            return items;
        }

        static FlareGunAirDropConfigs _flare;
        static bool _flareSearched;

        static FlareGunAirDropConfig FlareConfig()
        {
            if (!_flareSearched || (_flare != null && !Art.Alive(_flare)))
            {
                _flareSearched = true; _flare = null;
                try
                {
                    foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<FlareGunAirDropConfigs>()))
                    {
                        var c = o?.TryCast<FlareGunAirDropConfigs>();
                        if (c != null && c.airdrops != null && c.airdrops.Length > 0) { _flare = c; break; }
                    }
                    SLog.Info(_flare != null ? $"Using the flare-gun airdrop tables ({_flare.airdrops.Length})." : "Flare-gun airdrop tables not loaded yet; using the supply-crate tables only.");
                }
                catch (Exception e) { SLog.Debug("Flare airdrop lookup failed: " + e.Message); }
            }
            if (_flare == null) return null;
            var arr = _flare.airdrops;
            return arr.Length == 0 ? null : arr[_rng.Next(arr.Length)];
        }

        static ItemList FlareLoot()
        {
            try
            {
                var cfg = FlareConfig();
                var src = cfg?.itemConfig;
                if (src == null || src.Length == 0) return null;
                var list = new ItemList();
                for (int i = 0; i < src.Length; i++) if (src[i] != null) list.Add(src[i].Copy());
                return list;
            }
            catch (Exception e) { SLog.Debug("Flare loot failed: " + e.Message); return null; }
        }

        // ---- landing and the horde

        static void Landed(Active a)
        {
            a.LandedAt = Time.time;
            SLog.Info($"Supply drop #{a.Id} landed.");
            ShowLanded(a.Id, a.Target);
            Net.SendToGuests($"L|{a.Id}|{Net.F(a.Target.x)}|{Net.F(a.Target.y)}");
        }

        static void StartBeacon(Active a)
        {
            a.AlarmStarted = true;
            if (!Settings.Zombies.Value) return;
            var ac = AlarmController.instance;
            if (ac == null) { SLog.Warn("No alarm controller in this world; no horde for this drop."); return; }
            int count = Settings.ZombieCount.Value;
            if (Settings.ScaleZombiesByDay.Value)
            {
                try { count = Math.Max(count, AlarmController.ScaleAlarmEnemyNumberByDay(count)); } catch { }
            }
            var ids = ZombieIds();
            if (ids == null || ids.Length == 0) { SLog.Warn("No zombie types found for the horde."); return; }
            try
            {
                var go = ac.StartAlarmAtPosition(new Vector3(a.Target.x, a.Target.y, 0f), Settings.BeaconSeconds.Value, count, ids, 0, false, false, default);
                SLog.Info($"Beacon on: {count} zombies called to the crate for {Settings.BeaconSeconds.Value:0} s (types {string.Join(",", ids)}){(go == null ? " - the game refused the alarm here" : "")}.");
            }
            catch (Exception e) { SLog.Error("Starting the beacon horde failed", e); }
        }

        static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int> ZombieIds()
        {
            var ids = new List<int>();
            // the game's own airdrop monster list first
            try
            {
                var cfg = FlareConfig();
                if (cfg?.monsters != null)
                    for (int i = 0; i < cfg.monsters.Length; i++) if (!ids.Contains(cfg.monsters[i])) ids.Add(cfg.monsters[i]);
            }
            catch { }
            if (ids.Count == 0)
            {
                try
                {
                    ChunkType here = ChunkType.Field;
                    try { var dir = DirectorController.instance; if (dir != null) here = dir.GetPlayerChunkType(); } catch { }
                    var mm = MonsterManager.instance;
                    var attrs = mm?.monsterAttrs;
                    if (attrs != null)
                    {
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            var m = attrs[i];
                            if (m == null || m.isAnimal || m.monsterSpeciesType != MonsterSpeciesType.Zombie) continue;
                            var ct = m.generateChunkTypes;
                            if (ct != null && ct.Contains(here)) ids.Add(m.monsterId);
                        }
                    }
                }
                catch (Exception e) { SLog.Debug("Zombie type lookup failed: " + e.Message); }
                int common = -1;
                try { common = MonsterManager.OrdinaryInfectedAttrId; } catch { }
                if (common > 0) { ids.Add(common); ids.Add(common); }   // weight plain infected up
            }
            return ids.Count == 0 ? null : new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int>(ids.ToArray());
        }

        // ---- what every player sees (host locally, guests from messages)

        internal static void ShowInbound(int id, Vector2 target, float heading, float secondsToOverhead, bool local)
        {
            float overheadAt = Time.time + secondsToOverhead;
            Notify($"Supply helicopter inbound! Crate drops {Where(target)} in {Mathf.RoundToInt(secondsToOverhead)} s. It's marked on your map. Its beacon will draw zombies.");
            Arrow.Point(target, false);
            Marker.Set(target, false);
            // appear so it reaches the drop point right on time
            Heli.Spawn(target, heading, overheadAt);
            if (!local) SLog.Info($"Host says: supply drop #{id} inbound at ({target.x:0.#}, {target.y:0.#}).");
        }

        internal static void ShowLanded(int id, Vector2 target)
        {
            Notify($"Supply crate down {Where(target)}! Grab it before the horde does.");
            Arrow.Point(target, true);
            Marker.Set(target, true);
        }

        static string Where(Vector2 target)
        {
            try
            {
                var me = GameController.instance?.playerCharacter;
                if (me == null) return "nearby";
                Vector2 d = target - (Vector2)me.transform.position;
                if (d.magnitude < 4f) return "right next to you";
                float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;   // 0 = east, 90 = north
                string[] names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
                int idx = (int)Mathf.Repeat(Mathf.Round(deg / 45f), 8);
                return $"to the {names[idx]} ({Mathf.RoundToInt(d.magnitude)} m)";
            }
            catch { return "nearby"; }
        }

        internal static void Notify(string text)
        {
            SLog.Debug("Notice: " + text);
            try { SystemNotificationPanel.ShowNotificationSafe(text, true); }
            catch (Exception e) { SLog.Warn("On-screen message failed: " + e.Message); }
        }
    }
}
