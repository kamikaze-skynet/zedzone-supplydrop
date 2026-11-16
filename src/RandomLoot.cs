using System;
using System.Collections.Generic;
using UnityEngine;
using ZedZone.HordeMode;
using ItemList = Il2CppSystem.Collections.Generic.List<RecipeItemData>;

namespace SupplyDrop
{
    // Random crate contents drawn from every item the game knows (its own and other mods'),
    // limited to items that can normally spawn in the world. Each crate gets a spread of
    // categories so it isn't all screws: guns come with a magazine and ammo.
    internal static class RandomLoot
    {
        static readonly Dictionary<int, List<ItemAttr>> _byCat = new Dictionary<int, List<ItemAttr>>();
        static IntPtr _builtFor;
        static readonly System.Random _rng = new System.Random();

        // categories and how likely each slot is to be one of them
        const int Gun = 0, Ammo = 1, Melee = 2, Gear = 3, Food = 4, Material = 5, Other = 6;
        static readonly string[] CatNames = { "gun", "ammo", "melee/throwable", "gear", "food/drink", "material", "other" };
        static readonly int[] Weights = { 16, 20, 12, 16, 16, 14, 6 };

        static int CatOf(ItemType t)
        {
            switch (t)
            {
                case ItemType.RangedWeapon: return Gun;
                case ItemType.Ammo: case ItemType.Magazine: case ItemType.Arrow: return Ammo;
                case ItemType.MeleeWeapon: case ItemType.Throwable: return Melee;
                case ItemType.Clothing: case ItemType.Backpack: case ItemType.FaceMask: case ItemType.Cloak: return Gear;
                case ItemType.Food: case ItemType.Liqiud: return Food;
                case ItemType.Material: return Material;
                default: return Other;   // gun parts, deployables, containers
            }
        }

        static bool Build()
        {
            var im = ItemManager.instance;
            if (im == null || im.itemList == null || im.itemList.Count == 0) return false;
            if (im.Pointer == _builtFor && _byCat.Count > 0) return true;
            _byCat.Clear();
            int n = 0;
            foreach (var a in im.itemList)
            {
                try
                {
                    if (a == null || a.isMissingMod || a.hiddenItem) continue;
                    if (!a.generateItemInRoom && !a.generateItemInField) continue;   // only things that spawn in the world
                    int c = CatOf(a.itemType);
                    if (!_byCat.TryGetValue(c, out var l)) _byCat[c] = l = new List<ItemAttr>();
                    l.Add(a); n++;
                }
                catch { }
            }
            _builtFor = im.Pointer;
            var parts = new List<string>();
            foreach (var kv in _byCat) parts.Add($"{CatNames[kv.Key]} {kv.Value.Count}");
            SLog.Info($"Random loot pool: {n} items ({string.Join(", ", parts)}).");
            return n > 0;
        }

        static int PickCat()
        {
            int total = 0;
            for (int i = 0; i < Weights.Length; i++) if (_byCat.ContainsKey(i)) total += Weights[i];
            int r = _rng.Next(Math.Max(1, total));
            for (int i = 0; i < Weights.Length; i++)
            {
                if (!_byCat.ContainsKey(i)) continue;
                if (r < Weights[i]) return i;
                r -= Weights[i];
            }
            return Other;
        }

        internal static ItemList Make()
        {
            if (!Build()) { SLog.Warn("The item list isn't loaded yet; can't roll random loot."); return null; }
            int min = Math.Max(1, Settings.ItemsMin.Value);
            int max = Math.Max(min, Settings.ItemsMax.Value);
            int count = _rng.Next(min, max + 1);
            var list = new ItemList();
            var names = new List<string>();
            var used = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                // first slot is always a gun so every crate is worth the run
                int cat = i == 0 && _byCat.ContainsKey(Gun) ? Gun : PickCat();
                var pool = _byCat[cat];
                ItemAttr a = null;
                for (int t = 0; t < 6 && a == null; t++)
                {
                    var c = pool[_rng.Next(pool.Count)];
                    if (!used.Contains(c.itemId)) a = c;
                }
                if (a == null) continue;
                used.Add(a.itemId);
                int stack = Math.Max(1, a.stackNumber);
                try
                {
                    if (cat == Gun)
                    {
                        int rounds = _rng.Next(20, 91);
                        HordeRewardTable.AddWeaponKit(list, a.itemId, rounds, _rng.Next(1, 3));
                        names.Add($"{a.ItemName_EN} kit");
                        continue;
                    }
                }
                catch (Exception e) { SLog.Debug($"Weapon kit for {a.itemId} failed ({e.Message}); adding the gun alone."); }
                float amount;
                if (cat == Ammo) amount = Mathf.Max(1, Mathf.RoundToInt(stack * (float)(0.3 + _rng.NextDouble() * 0.7)));
                else if (cat == Material || cat == Food) amount = Mathf.Max(1, _rng.Next(1, Math.Max(2, Math.Min(stack, 6) + 1)));
                else amount = 1;
                list.Add(new RecipeItemData { itemId = a.itemId, itemNumber = amount });
                names.Add(amount > 1 ? $"{a.ItemName_EN} x{amount}" : a.ItemName_EN);
            }
            SLog.Info("Crate contents: " + string.Join(", ", names));
            return list;
        }
    }
}
