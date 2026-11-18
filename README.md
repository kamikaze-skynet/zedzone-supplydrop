# Supply Drop

A ZED ZONE mod (BepInEx IL2CPP). Every so often a supply helicopter flies over and parachutes a crate of good loot near one of the players. The crate's beacon draws a zombie horde to it, so grab it fast.

## What happens

1. **Warning.** A message pops up: *"Supply helicopter inbound! Crate drops to the north-east (64 m) in 45 s. It's marked on your map."* The drop zone is a random spot 40-90 units from a random player. It's set as your waypoint, so it shows on the map, the minimap and as an arrow with the distance at the edge of the screen (teammates see it too). A yellow arrow next to your character also points at it. Your old waypoint comes back once you reach the crate.
2. **Flyover.** A helicopter flies across the map with its rotor sound and shadow, and passes right over the drop zone.
3. **Drop.** The crate comes down on a parachute with a blinking landing marker (the game's own supply pack, the same one horde mode uses). In co-op there's one crate per player (up to 4).
4. **Horde.** A few seconds after it lands, the beacon goes off and zombies come out around the drop zone and head for it (the game's own alarm system, like a car alarm). The horde grows with the in-game day count.

Loot is random: each crate gets 5-9 items picked from every item that can spawn in the world, including other mods' items. There's always a gun with a magazine and ammo, plus a random mix of ammo, gear, food and drink, materials, melee weapons and more. Set `Loot.Mode = Tables` to use the game's own supply-crate tables instead.

Drops only happen when someone is above ground, and not while the game says it's a bad time (in a menu, at base, in a cutscene). The first drop comes 8 minutes after loading in, then about every 20 minutes.

**Testing:** the host can press **End** to call a drop right away (turn this off with `Keys.AllowCallKey = false`).

## Co-op

The host runs everything: the timer, the crates, the loot and the zombies. Those are the game's own objects, so the game syncs them to guests. Guests who also have the mod get the message, the helicopter and the arrow. Guests without it still get the crates and zombies.

## Settings

`BepInEx\config\girthy_mods.zedzone.supplydrop.cfg`. Only the host's settings matter for timing, loot and zombies.

| Setting | Default | |
|---|---|---|
| Timing.FirstDropMinutes | 8 | minutes after loading before the first drop |
| Timing.IntervalMinutes | 20 | average minutes between drops |
| Timing.IntervalRandomMinutes | 5 | each wait is ± this much |
| Timing.WarningSeconds | 45 | warning before the crate drops |
| Location.MinDistance / MaxDistance | 40 / 90 | the crate lands at a random spot this far from a random player |
| Loot.Mode | Random | Random items, or Tables (the game's supply-crate tables) |
| Loot.ItemsMin / ItemsMax | 5 / 9 | items per crate (Random mode) |
| Loot.LootLevel | 6 | 1-15, how good the loot is (Tables mode) |
| Loot.AffixIntensity | 0.5 | strength of random weapon/armour bonuses |
| Loot.CratePerPlayer / MaxCrates | true / 4 | one crate per player |
| Zombies.Enabled | true | the beacon draws a horde |
| Zombies.Count | 10 | horde size (before day scaling) |
| Zombies.ScaleByDay | true | grow the horde with the day count |
| Zombies.DelaySeconds | 6 | seconds after landing before the horde comes |
| Zombies.BeaconSeconds | 60 | how long the beacon keeps calling zombies |
| Helicopter.Enabled / Volume / Speed / Size | true / 0.6 / 14 / 1 | the flyover |
| Helicopter.UH1Y | true | draw the UH-1Y (Blender model); false = simple code-drawn helicopter |
| UI.MapMarker / MarkerMinutes | true / 10 | mark the drop as your waypoint on the map |
| UI.GuideArrow / ArrowMinutes | true / 4 | the arrow next to your character |
| Keys.CallDropKey / AllowCallKey | End / true | call a drop now (host) |

## Log

`BepInEx\SupplyDrop.log` (previous session in `SupplyDrop.prev.log`) lists every drop: where it landed, how many crates and loot entries, and how many zombies the beacon called. Turn on `General.VerboseLog` for more.

## Install

Subscribe on the Steam Workshop, or copy `SupplyDrop.dll` and `mod.json` into `%USERPROFILE%\AppData\LocalLow\LevenLiu\ZEDZONE\Mods\SupplyDrop`.

## Build

```
dotnet build src\SupplyDrop.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\ZED ZONE"
```

`publish-workshop.ps1 -SteamUser <login>` builds and uploads to the Workshop. The helicopter, rotor and arrow are drawn in code (`src/Art.cs`), and the rotor sound is generated in code (`src/RotorSound.cs`), so the mod is a single DLL. `art/make_preview.py` renders the Workshop preview from the same pixel rules.
