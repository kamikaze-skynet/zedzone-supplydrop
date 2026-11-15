# Supply Drop

A ZED ZONE mod (BepInEx IL2CPP). Every so often a supply helicopter flies over and parachutes a crate of good loot near one of the players. The crate's beacon draws a zombie horde to it, so grab it fast.

## What happens

1. **Warning.** A message pops up: *"Supply helicopter inbound! Crate drops to the north-east (25 m) in 45 s. Its beacon will draw zombies."* A yellow arrow next to your character points at the drop zone.
2. **Flyover.** A helicopter flies across the map with its rotor sound and shadow, and passes right over the drop zone.
3. **Drop.** The crate comes down on a parachute with a blinking landing marker (the game's own supply pack, the same one horde mode uses). In co-op there's one crate per player (up to 4).
4. **Horde.** A few seconds after it lands, the beacon goes off and zombies come out around the drop zone and head for it (the game's own alarm system, like a car alarm). The horde grows with the in-game day count.

Loot comes from the game's own supply-crate tables: the first crate is an elite box (weapons, ammo, armour), the others are a mix of armoury, personal and flare-gun airdrop loot.

Drops only happen when someone is above ground, and not while the game says it's a bad time (in a menu, at base, in a cutscene). The first drop comes 8 minutes after loading in, then about every 20 minutes.

**Testing:** the host can press **F8** to call a drop right away (turn this off with `Keys.AllowCallKey = false`).

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
| Drop.DistanceMin / DistanceMax | 18 / 32 | how far from the chosen player the crate lands |
| Loot.LootLevel | 6 | 1-15, how good the loot is |
| Loot.AffixIntensity | 0.5 | strength of random weapon/armour bonuses |
| Loot.CratePerPlayer / MaxCrates | true / 4 | one crate per player |
| Zombies.Enabled | true | the beacon draws a horde |
| Zombies.Count | 10 | horde size (before day scaling) |
| Zombies.ScaleByDay | true | grow the horde with the day count |
| Zombies.DelaySeconds | 6 | seconds after landing before the horde comes |
| Zombies.BeaconSeconds | 60 | how long the beacon keeps calling zombies |
| Helicopter.Enabled / Volume / Speed / Size | true / 0.6 / 14 / 1 | the flyover |
| UI.GuideArrow / ArrowMinutes | true / 4 | the arrow pointing at the crate |
| Keys.CallDropKey / AllowCallKey | F8 / true | call a drop now (host) |

## Log

`BepInEx\SupplyDrop.log` (previous session in `SupplyDrop.prev.log`) lists every drop: where it landed, how many crates and loot entries, and how many zombies the beacon called. Turn on `General.VerboseLog` for more.

## Install

Subscribe on the Steam Workshop, or copy `SupplyDrop.dll` and `mod.json` into `%USERPROFILE%\AppData\LocalLow\LevenLiu\ZEDZONE\Mods\SupplyDrop`.

## Build

```
dotnet build src\SupplyDrop.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\ZED ZONE"
```

`publish-workshop.ps1 -SteamUser <login>` builds and uploads to the Workshop. The helicopter, rotor and arrow are drawn in code (`src/Art.cs`), and the rotor sound is generated in code (`src/RotorSound.cs`), so the mod is a single DLL. `art/make_preview.py` renders the Workshop preview from the same pixel rules.
