using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using LiteNetLib;
using UnityEngine;
using ZedZone.Net;

namespace SupplyDrop
{
    // Host <-> guest messages ride on the game's co-op chat (MsgChat), marked with a prefix and
    // hidden from the chat box. Only guests that said hello (so they run the mod) get messages.
    //   guest -> host  H                                      "I have Supply Drop"
    //   host -> guest  I|id|x|y|heading|secondsToOverhead     helicopter inbound
    //                  L|id|x|y                               crate landed at x,y
    // The crate itself, its loot and the zombies are the game's own objects, which the game syncs.
    internal static class Net
    {
        internal const string Mark = "@@SD1|";
        internal static bool Is(string s) => s != null && s.StartsWith(Mark, StringComparison.Ordinal);

        static readonly Dictionary<string, NetPeer> _guests = new Dictionary<string, NetPeer>();
        static bool _helloSent;
        static float _helloAt = -1f;

        internal static bool IsGuest
        {
            get { try { return NetworkManager.isOnline && NetworkManager.isClient; } catch { return false; } }
        }

        internal static bool IsOnlineHost
        {
            get { try { return NetworkManager.isOnline && !NetworkManager.isClient; } catch { return false; } }
        }

        internal static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        internal static float P(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        // Guest: say hello once per world load, a few seconds after the world is up.
        internal static void Tick()
        {
            bool inWorld = InGameController.instance != null;
            if (!inWorld) { _helloSent = false; _helloAt = -1f; if (!IsOnlineHost) _guests.Clear(); return; }
            if (!IsGuest || _helloSent) return;
            if (_helloAt < 0f) { _helloAt = Time.realtimeSinceStartup + 5f; return; }
            if (Time.realtimeSinceStartup < _helloAt) return;
            _helloSent = true;
            try { NetChatSync.instance?.SendChat(Mark + "H"); SLog.Info("Told the host this guest runs Supply Drop."); }
            catch (Exception e) { SLog.Warn("Saying hello to the host failed: " + e.Message); }
        }

        internal static void SendToGuests(string body)
        {
            if (!IsOnlineHost || !Settings.ShareWithGuests.Value || _guests.Count == 0) return;
            var nm = NetworkManager.instance;
            if (nm == null) return;
            string text = Mark + body;
            foreach (var kv in new List<KeyValuePair<string, NetPeer>>(_guests))
            {
                try
                {
                    var msg = new MsgChat { senderId = 0, senderName = "", text = text };
                    nm.SendTo(kv.Value, msg, DeliveryMethod.ReliableOrdered, 0);
                }
                catch (Exception e) { SLog.Debug($"Dropping guest {kv.Key}: {e.Message}"); _guests.Remove(kv.Key); }
            }
            SLog.Debug($"Sent '{body}' to {_guests.Count} guest(s).");
        }

        internal static void OnHostReceive(string text, NetPeer peer)
        {
            var p = text.Substring(Mark.Length).Split('|');
            if (p[0] != "H" || peer == null) return;
            string key = peer.Id.ToString(CultureInfo.InvariantCulture);
            try
            {
                var nm = NetworkManager.instance;
                if (nm != null && nm.TryGetPeerCharacter(peer, out var charId, out _) && !string.IsNullOrEmpty(charId)) key = charId;
            }
            catch { }
            bool isNew = !_guests.ContainsKey(key);
            _guests[key] = peer;
            if (isNew) SLog.Info($"Guest {key} runs Supply Drop; they'll see the helicopter and messages.");
            Drops.SendActiveTo(peer);
        }

        internal static void SendTo(NetPeer peer, string body)
        {
            try
            {
                var msg = new MsgChat { senderId = 0, senderName = "", text = Mark + body };
                NetworkManager.instance?.SendTo(peer, msg, DeliveryMethod.ReliableOrdered, 0);
            }
            catch (Exception e) { SLog.Debug("Send to one guest failed: " + e.Message); }
        }

        internal static void OnClientReceive(string text)
        {
            var p = text.Substring(Mark.Length).Split('|');
            try
            {
                if (p[0] == "I" && p.Length >= 6)
                    Drops.ShowInbound(int.Parse(p[1], CultureInfo.InvariantCulture), new Vector2(P(p[2]), P(p[3])), P(p[4]), P(p[5]), false);
                else if (p[0] == "L" && p.Length >= 4)
                    Drops.ShowLanded(int.Parse(p[1], CultureInfo.InvariantCulture), new Vector2(P(p[2]), P(p[3])));
            }
            catch (Exception e) { SLog.Warn($"Bad message from host '{text}': {e.Message}"); }
        }
    }

    [HarmonyPatch]
    internal static class ChatPatches
    {
        // Never show our lines in the chat box or as speech bubbles.
        [HarmonyPatch(typeof(NetChatSync), nameof(NetChatSync.Append))]
        [HarmonyPrefix]
        static bool Append(string text) => !Net.Is(text);

        [HarmonyPatch(typeof(NetChatSync), nameof(NetChatSync.OnHostReceive))]
        [HarmonyPrefix]
        static bool OnHostReceive(MsgChat msg, NetPeer peer)
        {
            var text = msg?.text;
            if (!Net.Is(text)) return true;
            try { Net.OnHostReceive(text, peer); } catch (Exception e) { SLog.Error("Handling a guest message failed", e); }
            return false;
        }

        [HarmonyPatch(typeof(NetChatSync), nameof(NetChatSync.OnClientReceive))]
        [HarmonyPrefix]
        static bool OnClientReceive(MsgChat msg)
        {
            var text = msg?.text;
            if (!Net.Is(text)) return true;
            try { Net.OnClientReceive(text); } catch (Exception e) { SLog.Error("Handling a host message failed", e); }
            return false;
        }
    }
}
