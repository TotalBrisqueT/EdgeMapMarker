# To do

## How this will be used

- Three players. One hosts the world from the game; the other two join through the in-game friends list (no dedicated server).
- Built for our group first. Making it public on Thunderstore is fine, since that's where our modlist already comes from (r2modman).
- Our modlist: BepInExPack_Valheim, SharedMap (TXC), BetterNetworking_Valheim, Jotunn, MultiUserChest, CraftFromContainersEvolved, Quick_Stack_Store_Sort_Trash_Restock, AutoStore, KeepInventoryOnDeath.

What follows from that:
- **Client-side only.** Each of us installs it if we want it; the host doesn't need it to make it work for the others, and nothing is synced.
- **Test as both host and joining player.** The host is also a client, so both cases run the same minimap code, but the joining player is the one to check the bed lookup on.
- **SharedMap compatibility.** SharedMap shares pins between us, so pins from it must be trackable. Check what those pins look like to the game (own pin vs. shared pin) once tracking exists.
- **No Jotunn dependency.** It's in our list already, but the mod doesn't need it; staying plain BepInEx keeps it usable for anyone.
- **If published:** tag it Client-side and AI Generated on Thunderstore.

## Next

- [x] Test the edge marker in game (works: bed icon on the minimap edge toward spawn)
- [x] Check how it works on a server / in multiplayer (client-side only; nothing needs syncing)
- [x] Add context for how I plan to use this
- [ ] Test in a real session: host and a joining player both check the marker points at their own bed

## Later

- [x] Arrow pointing outward from the edge icon
- [x] Distance label (m / km)
- [x] Marker size setting (default 0.7)
- [ ] Make the marker easier to see (outline or tint) and keep it clear of the biome name on the top edge
- [x] Track any pin (Alt + click on the large map), saved per character and world, by position and type
- [x] Track several pins at once; home tracked alongside them and toggleable per world
- [x] Mark tracked pins on the maps (corner badge, ring as an alternative style)
- [x] Decide what happens to a tracked shared pin when the map's "shared pins" toggle is off (keeps tracking)
- [x] SharedMap compatibility checked against its code; hiding the shared map no longer drops tracking of public markers
- [ ] Test tracking a pin shared through SharedMap (public marker), including toggling "Server shared map" off and on
- [ ] Two quick Alt+clicks may open the game's "name a new pin" box; block it if it happens
- [ ] Option to show the distance only on the nearest marker, since labels overlap when pins share a direction
- [x] Track another player (their map position, when they share it), with name and a name-based colour
- [ ] Test player tracking in a group session: everyone "Visible on map", same colours on every screen
- [ ] If two of us get the same colour: per-player colour setting shared via the mod, or tweak the calculation
- [x] Config toggles: icon, arrow, distance, size
- [x] Config: track key, track home by default, highlight on/off, style and color
- [ ] More config: marker opacity and color, units, hide in dungeons
- [x] Thunderstore packaging: manifest, README, icon, zip (Release build writes dist\EdgeMapMarker-<version>.zip)
- [x] Settle on the mod name: Edge Map Marker
- [ ] Group test from the local zip (mod manager: Settings > Import local mod), then upload to Thunderstore
- [ ] Optional: README note that MapTracker also uses Alt+click, so change one mod's key if using both
- [ ] Optional: replace the placeholder icon
