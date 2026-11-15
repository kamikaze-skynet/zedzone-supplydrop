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
        internal static ConfigEntry<bool> CratePerPlayer;
        internal static ConfigEntry<float> LootQuality;
        // zombies
        internal static ConfigEntry<bool> Zombies, ScaleZombiesByDay;
        internal static ConfigEntry<int> ZombieCount;
        internal static ConfigEntry<float> ZombieDelaySeconds, BeaconSeconds;
        // helicopter / UI
        internal static ConfigEntry<bool> Helicopter, GuideArrow, ShareWithGuests;
        internal static ConfigEntry<float> HeliVolume, HeliSpeed, HeliScale, ArrowMinutes;
        internal static ConfigEntry<KeyCode> CallDropKey;
        internal static ConfigEntry<bool> AllowCallKey;

        static ConfigDescription R(string d, float min, float max) => new ConfigDescription(d, new AcceptableValueRange<float>(min, max));
        static ConfigDescription RI(string d, int min, int max) => new ConfigDescription(d, new AcceptableValueRange<int>(min, max));

        internal static void Bind(ConfigFile c)
        {
            Enabled = c.Bind("General", "Enabled", true, "Turn supply drops on or off. The host (or single player) runs the drops; guests with the mod see the helicopter, the message and the guide arrow.");
            VerboseLog = c.Bind("General", "VerboseLog", false, "Log extra detail in BepInEx\\SupplyDrop.log.");

            FirstDropMinutes = c.Bind("Timing", "FirstDropMinutes", 8f, R("Minutes after loading into the world before the first drop.", 0.5f, 240f));
            IntervalMinutes = c.Bind("Timing", "IntervalMinutes", 20f, R("Average minutes between drops.", 1f, 240f));
            IntervalRandomMinutes = c.Bind("Timing", "IntervalRandomMinutes", 5f, R("Each wait is IntervalMinutes plus or minus up to this many minutes.", 0f, 120f));
            WarningSeconds = c.Bind("Timing", "WarningSeconds", 45f, R("Seconds between the 'helicopter inbound' message and the crate dropping.", 10f, 300f));

            DropDistanceMin = c.Bind("Drop", "DistanceMin", 18f, R("Closest the crate lands to the chosen player (world units).", 4f, 200f));
            DropDistanceMax = c.Bind("Drop", "DistanceMax", 32f, R("Farthest the crate lands from the chosen player (world units).", 4f, 300f));

            LootLevel = c.Bind("Loot", "LootLevel", 6, RI("How good the loot is (1-15). Uses the game's own supply-crate loot tables; higher means better weapons and more of them.", 1, 15));
            LootQuality = c.Bind("Loot", "AffixIntensity", 0.5f, R("How strong random weapon/armour bonuses on crate loot are (0 = none, 1 = strongest).", 0f, 1f));
            CratePerPlayer = c.Bind("Loot", "CratePerPlayer", true, "Drop one crate per player in the session (so nobody has to fight over it).");
            MaxCrates = c.Bind("Loot", "MaxCrates", 4, RI("Most crates in one drop.", 1, 8));

            Zombies = c.Bind("Zombies", "Enabled", true, "The crate's beacon draws a horde: zombies come out around the drop zone after it lands.");
            ZombieCount = c.Bind("Zombies", "Count", 10, RI("How many zombies the beacon draws.", 1, 60));
            ScaleZombiesByDay = c.Bind("Zombies", "ScaleByDay", true, "Grow the horde with the in-game day count the same way the game's alarms do.");
            ZombieDelaySeconds = c.Bind("Zombies", "DelaySeconds", 6f, R("Seconds after the crate lands before the horde starts coming.", 0f, 120f));
            BeaconSeconds = c.Bind("Zombies", "BeaconSeconds", 60f, R("How long the beacon keeps calling zombies.", 5f, 600f));

            Helicopter = c.Bind("Helicopter", "Enabled", true, "Show the helicopter flying over and play its rotor sound.");
            HeliVolume = c.Bind("Helicopter", "Volume", 0.6f, R("Rotor sound volume.", 0f, 1f));
            HeliSpeed = c.Bind("Helicopter", "Speed", 14f, R("Flying speed in world units per second.", 2f, 60f));
            HeliScale = c.Bind("Helicopter", "Size", 1f, R("Size of the helicopter sprite.", 0.3f, 4f));

            GuideArrow = c.Bind("UI", "GuideArrow", true, "Show an arrow next to your character that points at the newest crate.");
            ArrowMinutes = c.Bind("UI", "ArrowMinutes", 4f, R("How long the arrow stays (it also goes away when you reach the crate).", 0.5f, 30f));
            ShareWithGuests = c.Bind("General", "ShareWithGuests", true, "Host: send the drop to guests who have the mod (message, helicopter, arrow).");

            AllowCallKey = c.Bind("Keys", "AllowCallKey", true, "Host: let CallDropKey call a drop right now (handy for testing).");
            CallDropKey = c.Bind("Keys", "CallDropKey", KeyCode.End, "Host: key that calls a supply drop right now. Default End: F5 is the game's quick save, F7/F8 are DogShare's give/take-back dog keys (F9/F10 its kennel test build), Insert is Spawn Menu.");
        }

        internal static string Describe() =>
            $"Config: Enabled={Enabled.Value} first={FirstDropMinutes.Value}m every={IntervalMinutes.Value}±{IntervalRandomMinutes.Value}m warning={WarningSeconds.Value}s " +
            $"distance={DropDistanceMin.Value}-{DropDistanceMax.Value} loot={LootLevel.Value} affix={LootQuality.Value} perPlayer={CratePerPlayer.Value} max={MaxCrates.Value} " +
            $"zombies={Zombies.Value} x{ZombieCount.Value} scale={ScaleZombiesByDay.Value} delay={ZombieDelaySeconds.Value}s beacon={BeaconSeconds.Value}s " +
            $"heli={Helicopter.Value} vol={HeliVolume.Value} speed={HeliSpeed.Value} arrow={GuideArrow.Value} key={(AllowCallKey.Value ? CallDropKey.Value.ToString() : "off")}";
    }
}
