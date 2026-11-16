# Changelog

## 1.1.0 (2026-11-16)
- Random loot: each crate gets 5-9 random items from every item that can spawn in the world (always a gun with a magazine and ammo, then a mix of ammo, gear, food, materials and more). `Loot.Mode = Tables` brings back the game's supply-crate tables.
- Random location: the crate lands at a random spot 40-90 units from a random player (was 18-32), set in the new `[Location]` section.
- Map marker: the drop is set as your waypoint, so it shows on the map, the minimap and as an arrow with distance at the screen edge; teammates see it too. Your old waypoint comes back once you reach the crate (or after 10 minutes).

## 1.0.1 (2026-11-15)
- Call-drop key moved from F8 to End: F8 is DogShare's take-back-dog key. At startup the log warns if any other mod's config uses the same key.

## 1.0.0 (2026-11-15)
- First version: timed supply helicopter drops with a flyover (sprite, shadow, rotor sound), parachute crates using the game's supply packs and loot tables, a guide arrow, on-screen messages, and a beacon that draws a zombie horde to the crate.
- Co-op: host runs the drops; guests with the mod see the helicopter, message and arrow.
- End (host) calls a drop right away for testing.
