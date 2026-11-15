using System;
using UnityEngine;

namespace SupplyDrop
{
    // A small yellow arrow circling the local player, pointing at the newest crate.
    // Goes away when the player gets close or after UI.ArrowMinutes.
    internal static class Arrow
    {
        static GameObject _go;
        static SpriteRenderer _sr;
        static Vector2 _target;
        static float _until = -1f;
        static bool _landed;

        internal static void Point(Vector2 target, bool landed)
        {
            if (!Settings.GuideArrow.Value) return;
            _target = target;
            _landed = landed;
            _until = Time.time + Settings.ArrowMinutes.Value * 60f;
        }

        internal static void Clear()
        {
            _until = -1f;
            try { if (Art.Alive(_go)) UnityEngine.Object.Destroy(_go); } catch { }
            _go = null; _sr = null;
        }

        internal static void Tick()
        {
            if (_until < 0f) return;
            if (InGameController.instance == null || Time.time > _until) { Clear(); return; }
            HumanCharacterController me = null;
            try { me = GameController.instance?.playerCharacter; } catch { }
            if (me == null) { if (Art.Alive(_go)) _go.SetActive(false); return; }

            Vector2 pos = me.transform.position;
            Vector2 to = _target - pos;
            float dist = to.magnitude;
            if (_landed && dist < 2.5f) { SLog.Debug("Reached the crate; arrow off."); Clear(); return; }

            if (!Art.Alive(_go))
            {
                var sprite = Art.ArrowSprite;
                if (sprite == null) { _until = -1f; return; }
                _go = new GameObject("SupplyDrop.Arrow");
                _sr = _go.AddComponent<SpriteRenderer>();
                _sr.sprite = sprite;
                _sr.sortingLayerID = Heli.TopLayer();
                _sr.sortingOrder = 31980;
            }
            if (!_go.activeSelf) _go.SetActive(true);
            Vector2 dir = dist > 0.01f ? to / dist : Vector2.right;
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 5f);
            _go.transform.position = new Vector3(pos.x + dir.x * 1.6f, pos.y + dir.y * 1.6f, 0f);
            _go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            _go.transform.localScale = new Vector3(pulse, pulse, 1f);
            if (_sr != null) _sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.55f + dist / 30f));
        }
    }
}
