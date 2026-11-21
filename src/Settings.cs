using BepInEx.Configuration;
using UnityEngine;

namespace SupplyDrop
{
    internal static class Settings
    {
        internal static ConfigEntry<bool> Enabled, VerboseLog;

        // timing
        internal static ConfigEntry<float> FirstDropMinutes, IntervalMinutes, IntervalRandomMinutes, WarningSeconds;
        // where
        internal static ConfigEntry<float> DropDistanceMin, DropDistanceMax;
        // loot
        internal static ConfigEntry<int> LootLevel, MaxCrates;
        internal static ConfigEntry<bool> CratePerPlayer, ModdedWeapons, ModdedItems, PackDisassemble;
        internal static ConfigEntry<string> GunTypes;
        internal static ConfigEntry<int> ConfigVersion;
        internal static ConfigEntry<string> LootMode;
        internal static ConfigEntry<int> ItemsMin, ItemsMax;
        internal static ConfigEntry<float> LootQuality;
        // zombies
        internal static ConfigEntry<bool> Zombies, ScaleZombiesByDay;
        internal static ConfigEntry<int> ZombieCount;
        internal static ConfigEntry<float> ZombieDelaySeconds, BeaconSeconds;
        // helicopter / UI
        internal static ConfigEntry<bool> Helicopter, UseUH1Y, GuideArrow, ShareWithGuests, MapMarker;
        internal static ConfigEntry<float> HeliVolume, HeliSpeed, HeliScale, ArrowMinutes, MarkerMinutes;
        internal static ConfigEntry<KeyCode> CallDropKey;
        internal static ConfigEntry<bool> AllowCallKey;

        static ConfigDescription R(string d, float min, float max) => new ConfigDescription(d, new AcceptableValueRange<float>(min, max));
        static ConfigDescription RI(string d, int min, int max) => new ConfigDescription(d, new AcceptableValueRange<int>(min, max));

        internal static void Bind(ConfigFile c)
        {
            Enabled = c.Bind("General", "Enabled", true, "Turn supply drops on or off. The host (or single player) runs the drops; guests with the mod see the helicopter, the message and the guide arrow.");
            VerboseLog = c.Bind("General", "VerboseLog", false, "Log extra detail in BepInEx\\SupplyDrop.log.");

            FirstDropMinutes = c.Bind("Timing", "FirstDropMinutes", 30f, R("Minutes of play after loading into the world before the first drop.", 0.5f, 480f));
            IntervalMinutes = c.Bind("Timing", "IntervalMinutes", 90f, R("Average minutes of play between drops (default 90 = about one drop every 1-2 hours with the random spread below).", 1f, 480f));
            IntervalRandomMinutes = c.Bind("Timing", "IntervalRandomMinutes", 30f, R("Each wait is IntervalMinutes plus or minus up to this many minutes.", 0f, 240f));
            WarningSeconds = c.Bind("Timing", "WarningSeconds", 45f, R("Seconds between the 'helicopter inbound' message and the crate dropping.", 10f, 300f));

            DropDistanceMin = c.Bind("Location", "MinDistance", 40f, R("The crate lands at a random spot at least this far (world units) from a random player. It's marked on the map.", 4f, 400f));
            DropDistanceMax = c.Bind("Location", "MaxDistance", 90f, R("...and at most this far. Very far drops can land in areas nobody has loaded yet.", 4f, 600f));

            LootMode = c.Bind("Loot", "Mode", "Random", new ConfigDescription("Random: each crate gets random items from every item that can spawn in the world (a gun with ammo, plus a mix of ammo, gear, food, materials...). Tables: the game's own supply-crate tables (elite/armoury/personal boxes).", new AcceptableValueList<string>("Random", "Tables")));
            ItemsMin = c.Bind("Loot", "ItemsMin", 5, RI("Random mode: fewest items in a crate.", 1, 30));
            ItemsMax = c.Bind("Loot", "ItemsMax", 9, RI("Random mode: most items in a crate.", 1, 30));
            LootLevel = c.Bind("Loot", "LootLevel", 6, RI("How good the loot is (1-15). Uses the game's own supply-crate loot tables; higher means better weapons and more of them.", 1, 15));
            LootQuality = c.Bind("Loot", "AffixIntensity", 0.5f, R("How strong random weapon/armour bonuses on crate loot are (0 = none, 1 = strongest).", 0f, 1f));
            GunTypes = c.Bind("Loot", "GunTypes", "Pistol, Revolver, SMG, Shotgun", "Random mode: which kinds of gun a crate can hold, comma separated. Choices: Pistol, Revolver, SMG, Shotgun, Rifle, Bow, Crossbow, Energy, Liquid, Flare. Leave empty for no guns. The log lists which gun counts as what.");
            ModdedWeapons = c.Bind("Loot", "ModdedWeapons", false, "Random mode: allow guns and melee weapons added by other mods (off = only the game's own weapons).");
            ModdedItems = c.Bind("Loot", "ModdedItems", true, "Random mode: allow other non-weapon items added by other mods (ammo, gear, food...).");
            PackDisassemble = c.Bind("Loot", "PackDisassemble", true, "Let the supply pack itself (green satchel) be disassembled like a backpack once it's empty.");
            CratePerPlayer = c.Bind("Loot", "CratePerPlayer", true, "Drop one crate per player in the session (so nobody has to fight over it).");
            MaxCrates = c.Bind("Loot", "MaxCrates", 4, RI("Most crates in one drop.", 1, 8));

            Zombies = c.Bind("Zombies", "Enabled", true, "The crate's beacon draws a horde: zombies come out around the drop zone after it lands.");
            ZombieCount = c.Bind("Zombies", "Count", 10, RI("How many zombies the beacon draws.", 1, 60));
            ScaleZombiesByDay = c.Bind("Zombies", "ScaleByDay", true, "Grow the horde with the in-game day count the same way the game's alarms do.");
            ZombieDelaySeconds = c.Bind("Zombies", "DelaySeconds", 6f, R("Seconds after the crate lands before the horde starts coming.", 0f, 120f));
            BeaconSeconds = c.Bind("Zombies", "BeaconSeconds", 60f, R("How long the beacon keeps calling zombies.", 5f, 600f));

            Helicopter = c.Bind("Helicopter", "Enabled", true, "Show the helicopter flying over and play its rotor sound.");
            UseUH1Y = c.Bind("Helicopter", "UH1Y", true, "Draw the UH-1Y helicopter (built into this mod; no other mod needed). Off = the simple code-drawn helicopter.");
            HeliVolume = c.Bind("Helicopter", "Volume", 0.6f, R("Rotor sound volume.", 0f, 1f));
            HeliSpeed = c.Bind("Helicopter", "Speed", 14f, R("Flying speed in world units per second.", 2f, 60f));
            HeliScale = c.Bind("Helicopter", "Size", 1f, R("Size of the helicopter sprite.", 0.3f, 4f));

            GuideArrow = c.Bind("UI", "GuideArrow", true, "Show an arrow next to your character that points at the newest crate.");
            ArrowMinutes = c.Bind("UI", "ArrowMinutes", 4f, R("How long the arrow stays (it also goes away when you reach the crate).", 0.5f, 30f));
            MapMarker = c.Bind("UI", "MapMarker", true, "Put the drop on your map as a waypoint (map pin, minimap, edge-of-screen arrow with distance; teammates see it too). Your old waypoint comes back after you reach the crate.");
            MarkerMinutes = c.Bind("UI", "MarkerMinutes", 10f, R("How long the map marker stays if nobody reaches the crate.", 1f, 60f));
            ConfigVersion = c.Bind("General", "ConfigVersion", 0, "Used by the mod to update old default values. Leave it alone.");
            ShareWithGuests = c.Bind("General", "ShareWithGuests", true, "Host: send the drop to guests who have the mod (message, helicopter, arrow).");

            AllowCallKey = c.Bind("Keys", "AllowCallKey", true, "Host: let CallDropKey call a drop right now (handy for testing).");
            CallDropKey = c.Bind("Keys", "CallDropKey", KeyCode.End, "Host: key that calls a supply drop right now. Default End: F5 is the game's quick save, F7/F8 are DogShare's give/take-back dog keys (F9/F10 its kennel test build), Insert is Spawn Menu.");
        }

        // 1.3.0 raised the timing defaults; move configs still on the old defaults to the new ones
        internal static void Migrate()
        {
            if (ConfigVersion.Value >= 2) return;
            if (Mathf.Approximately(IntervalMinutes.Value, 20f)) IntervalMinutes.Value = 90f;
            if (Mathf.Approximately(IntervalRandomMinutes.Value, 5f)) IntervalRandomMinutes.Value = 30f;
            if (Mathf.Approximately(FirstDropMinutes.Value, 8f)) FirstDropMinutes.Value = 30f;
            ConfigVersion.Value = 2;
        }

        internal static string Describe() =>
            $"Config: Enabled={Enabled.Value} first={FirstDropMinutes.Value}m every={IntervalMinutes.Value}±{IntervalRandomMinutes.Value}m warning={WarningSeconds.Value}s " +
            $"distance={DropDistanceMin.Value}-{DropDistanceMax.Value} loot={LootMode.Value} guns=[{GunTypes.Value}] moddedWeapons={ModdedWeapons.Value} moddedItems={ModdedItems.Value} packDisassemble={PackDisassemble.Value} items={ItemsMin.Value}-{ItemsMax.Value} level={LootLevel.Value} affix={LootQuality.Value} perPlayer={CratePerPlayer.Value} max={MaxCrates.Value} " +
            $"zombies={Zombies.Value} x{ZombieCount.Value} scale={ScaleZombiesByDay.Value} delay={ZombieDelaySeconds.Value}s beacon={BeaconSeconds.Value}s " +
            $"heli={Helicopter.Value} vol={HeliVolume.Value} speed={HeliSpeed.Value} arrow={GuideArrow.Value} map={MapMarker.Value} key={(AllowCallKey.Value ? CallDropKey.Value.ToString() : "off")}";
    }
}
