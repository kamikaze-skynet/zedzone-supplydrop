using System;
using UnityEngine;
using ZedZone.HordeMode;

namespace SupplyDrop
{
    // The supply pack the crate comes in (the game's horde-mode parachute satchel) has no
    // disassemble recipe, so it can't be broken down. Give it the same recipe as one of the
    // game's own backpacks (or a few cloth materials if no backpack has one). Runs on every
    // player, host or guest, once the item list is loaded.
    internal static class Pack
    {
        static IntPtr _doneFor;
        static bool _gaveUp;

        internal static void GiveUp() => _gaveUp = true;

        internal static void Tick()
        {
            if (_gaveUp || !Settings.PackDisassemble.Value) return;
            var im = ItemManager.instance;
            if (im == null || im.itemList == null || im.itemList.Count == 0) return;
            if (im.Pointer == _doneFor) return;
            _doneFor = im.Pointer;

            int packId = HordeSupplyPack.PackItemId;
            ItemAttr pack = null;
            foreach (var a in im.itemList) if (a != null && a.itemId == packId) { pack = a; break; }
            if (pack == null) { SLog.Warn($"Supply pack item {packId} not found; it stays non-disassemblable."); return; }

            string name = Name(pack);
            if (HasItems(pack.disassembleData))
            {
                SLog.Info($"Supply pack ({name}, id {packId}) can already be disassembled.");
                return;
            }

            var data = new DisassembleData();
            data.disassembledItems = new Il2CppSystem.Collections.Generic.List<RecipeItemData>();
            data.requiredTools = new Il2CppSystem.Collections.Generic.List<int>();
            string from = null;

            // 1) copy a vanilla backpack's recipe
            foreach (var a in im.itemList)
            {
                try
                {
                    if (a == null || a.itemId == packId || a.itemType != ItemType.Backpack) continue;
                    if (!string.IsNullOrEmpty(a.modGuid)) continue;
                    var d = a.disassembleData;
                    if (!HasItems(d)) continue;
                    foreach (var r in d.disassembledItems)
                        data.disassembledItems.Add(new RecipeItemData { itemId = r.itemId, itemNumber = r.itemNumber });
                    if (d.requiredTools != null) foreach (var t in d.requiredTools) data.requiredTools.Add(t);
                    data.requareGrinder = d.requareGrinder;
                    from = "the " + Name(a);
                    break;
                }
                catch { }
            }

            // 2) otherwise a few pieces of cloth
            if (from == null)
            {
                foreach (var a in im.itemList)
                {
                    try
                    {
                        if (a == null || a.itemType != ItemType.Material || !string.IsNullOrEmpty(a.modGuid)) continue;
                        string n = Name(a).ToLowerInvariant();
                        if (!(n.Contains("cloth") || n.Contains("fabric") || n.Contains("rag"))) continue;
                        data.disassembledItems.Add(new RecipeItemData { itemId = a.itemId, itemNumber = 3 });
                        from = "3x " + Name(a);
                        break;
                    }
                    catch { }
                }
            }

            if (from == null) { SLog.Warn("Found no backpack recipe or cloth to give the supply pack; it stays non-disassemblable."); return; }
            pack.disassembleData = data;
            SLog.Info($"Supply pack ({name}, id {packId}, {pack.itemType}) can now be disassembled (same as {from}).");
        }

        static bool HasItems(DisassembleData d) => d != null && d.disassembledItems != null && d.disassembledItems.Count > 0;

        static string Name(ItemAttr a) { try { return a.ItemName_EN ?? a.itemId.ToString(); } catch { return a.itemId.ToString(); } }
    }
}
