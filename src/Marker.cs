using System;
using UnityEngine;

namespace SupplyDrop
{
    // Marks the drop with the game's own waypoint: a pin on the map and minimap, an arrow with
    // the distance at the edge of the screen, and teammates see it as your waypoint too.
    // Whatever waypoint you had before comes back once you reach the crate or the marker times out
    // (unless you moved the waypoint yourself in the meantime).
    internal static class Marker
    {
        static bool _active, _hadPrev, _landed;
        static Vector2 _ours, _prev;
        static float _until;

        internal static void Set(Vector2 target, bool landed)
        {
            if (!Settings.MapMarker.Value) return;
            try
            {
                if (!_active)
                {
                    _hadPrev = PlayerWaypoint.TryGetLocal(out _prev, out _);
                }
                Vector2 at = target;
                try { at = PlayerWaypoint.ClampToMap(target); } catch { }
                if (PlayerWaypoint.TrySet(at))
                {
                    _active = true; _ours = at; _landed = landed;
                    _until = Time.time + Settings.MarkerMinutes.Value * 60f;
                    SLog.Info($"Drop marked on the map at ({at.x:0.#}, {at.y:0.#}){(_hadPrev ? $"; your old waypoint ({_prev.x:0.#}, {_prev.y:0.#}) comes back later" : "")}.");
                }
                else SLog.Warn("The game refused the map marker (underground or off the map?).");
            }
            catch (Exception e) { SLog.Warn("Map marker failed: " + e.Message); }
        }

        internal static void Tick()
        {
            if (!_active) return;
            if (InGameController.instance == null) { _active = false; return; }
            bool done = Time.time > _until;
            if (!done && _landed)
            {
                try
                {
                    var me = GameController.instance?.playerCharacter;
                    if (me != null && Vector2.Distance(me.transform.position, _ours) < 3f) done = true;
                }
                catch { }
            }
            if (done) Restore();
        }

        static void Restore()
        {
            _active = false;
            try
            {
                // leave it alone if the player set another waypoint since
                if (!PlayerWaypoint.TryGetLocal(out var cur, out _) || Vector2.Distance(cur, _ours) > 0.5f) return;
                if (_hadPrev) { PlayerWaypoint.TrySet(_prev); SLog.Debug("Old waypoint restored."); }
                else { PlayerWaypoint.Clear(false); SLog.Debug("Drop marker cleared."); }
            }
            catch (Exception e) { SLog.Debug("Restoring the waypoint failed: " + e.Message); }
        }
    }
}
