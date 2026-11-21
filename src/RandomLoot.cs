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
        static readonly Dictionary<int, string> _gunKind = new Dictionary<int, string>();
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
            _gunKind.Clear();
            ClassifyGuns(im.itemList);
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

        static readonly string[] SmgWords = { "smg", "submachine", "sub-machine", "sub machine", "uzi", "mp5", "mp7", "mp40", "mp-40", "ump", "p90", "vector", "mac-10", "mac10", "mac 10", "thompson", "tommy", "skorpion", "bizon", "tec-9", "tec9", "sten", "ppsh", "pp-19", "pp19", "kriss" };

        // Gun kinds come from the game's weapon type; "Rifle" is split into SMG when the gun fires
        // pistol ammo or its name says so.
        static void ClassifyGuns(Il2CppSystem.Collections.Generic.List<ItemAttr> all)
        {
            var guns = new List<ItemAttr_RangedWeapon>();
            var pistolAmmo = new HashSet<int>();
            foreach (var a in all)
            {
                try
                {
                    if (a == null || a.itemType != ItemType.RangedWeapon) continue;
                    var g = a.TryCast<ItemAttr_RangedWeapon>();
                    if (g == null) continue;
                    guns.Add(g);
                    if ((g.weaponType == RangedWeaponType.Pistol || g.weaponType == RangedWeaponType.Revolver) && g.ammoId > 0) pistolAmmo.Add(g.ammoId);
                }
                catch { }
            }
            var byKind = new SortedDictionary<string, List<string>>();
            foreach (var g in guns)
            {
                string kind;
                try
                {
                    if (g.isFlareGun) kind = "Flare";
                    else switch (g.weaponType)
                    {
                        case RangedWeaponType.Pistol: kind = "Pistol"; break;
                        case RangedWeaponType.Revolver: kind = "Revolver"; break;
                        case RangedWeaponType.ShotGun: kind = "Shotgun"; break;
                        case RangedWeaponType.Bow: kind = "Bow"; break;
                        case RangedWeaponType.Crossbow: kind = "Crossbow"; break;
                        case RangedWeaponType.EnergyWeapon: case RangedWeaponType.EnergyChargeWeapon: kind = "Energy"; break;
                        case RangedWeaponType.LiquidWeapon: kind = "Liquid"; break;
                        default:
                            string name = (g.ItemName_EN ?? "").ToLowerInvariant();
                            bool smg = pistolAmmo.Contains(g.ammoId);
                            foreach (var w in SmgWords) if (name.Contains(w)) { smg = true; break; }
                            kind = smg ? "SMG" : "Rifle";
                            break;
                    }
                }
                catch { kind = "Rifle"; }
                _gunKind[g.itemId] = kind;
                if (!byKind.TryGetValue(kind, out var l)) byKind[kind] = l = new List<string>();
                l.Add($"{SafeName(g)}{(IsModded(g) ? " (mod)" : "")}");
            }
            foreach (var kv in byKind) SLog.Info($"Guns counted as {kv.Key}: {string.Join(", ", kv.Value)}");
        }

        static string SafeName(ItemAttr a) { try { return a.ItemName_EN ?? a.itemId.ToString(); } catch { return a.itemId.ToString(); } }

        static bool IsModded(ItemAttr a)
        {
            try { return !string.IsNullOrEmpty(a.modGuid); } catch { return false; }
        }

        static HashSet<string> AllowedGunKinds()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in (Settings.GunTypes.Value ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var k = part.Trim();
                if (k.Equals("Shotguns", StringComparison.OrdinalIgnoreCase)) k = "Shotgun";
                if (k.Equals("Pistols", StringComparison.OrdinalIgnoreCase)) k = "Pistol";
                if (k.Equals("SMGs", StringComparison.OrdinalIgnoreCase)) k = "SMG";
                set.Add(k);
            }
            return set;
        }

        // Is this item allowed in a crate with the current settings? (read every crate, so Mod Menu changes apply at once)
        static bool Allowed(ItemAttr a, int cat, HashSet<string> gunKinds)
        {
            bool modded = IsModded(a);
            if (cat == Gun || cat == Melee && a.itemType == ItemType.MeleeWeapon)
            {
                if (modded && !Settings.ModdedWeapons.Value) return false;
            }
            else if (modded && !Settings.ModdedItems.Value) return false;
            if (cat == Gun)
            {
                if (!_gunKind.TryGetValue(a.itemId, out var kind)) kind = "Rifle";
                return gunKinds.Contains(kind);
            }
            return true;
        }

        static Dictionary<int, List<ItemAttr>> _pools = new Dictionary<int, List<ItemAttr>>();

        static int PickCat()
        {
            int total = 0;
            for (int i = 0; i < Weights.Length; i++) if (_pools.ContainsKey(i)) total += Weights[i];
            int r = _rng.Next(Math.Max(1, total));
            for (int i = 0; i < Weights.Length; i++)
            {
                if (!_pools.ContainsKey(i)) continue;
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
            var kinds = AllowedGunKinds();
            _pools = new Dictionary<int, List<ItemAttr>>();
            foreach (var kv in _byCat)
            {
                var l = new List<ItemAttr>();
                foreach (var a in kv.Value) { try { if (Allowed(a, kv.Key, kinds)) l.Add(a); } catch { } }
                if (l.Count > 0) _pools[kv.Key] = l;
            }
            if (_pools.Count == 0) { SLog.Warn("No items are allowed by the Loot settings; the crate will use the game's tables."); return null; }
            var list = new ItemList();
            var names = new List<string>();
            var used = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                // first slot is always a gun so every crate is worth the run
                int cat = i == 0 && _pools.ContainsKey(Gun) ? Gun : PickCat();
                if (!_pools.TryGetValue(cat, out var pool)) continue;
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
