# To do

## Next session

- [ ] **Test the edge marker in game.** Built and deployed but not yet seen. Check edge placement, size, direction while turning, minimap zoom, and the hand-off to the real bed pin when back in range. Commit `EdgeMarker.cs` once it looks right.
- [ ] **Check how it works on a server / in multiplayer.**
  - Dedicated server and someone else's hosted world: does the bed/spawn lookup still work as a client?
  - Does the mod need to be on the server at all, or only on clients?
  - Shared map pins (from cartography table): can they be tracked?
  - Players without the mod joining: no errors or version-mismatch kicks.
- [ ] **Add context for how I plan to use this.** Who plays with it, on what kind of server, and what I mainly want to track, so the design and defaults fit.

## Later

- [ ] Arrow pointing outward from the edge icon
- [ ] Distance label (m / km)
- [ ] Track any pin (modifier + click on the large map), saved per character and world
- [ ] Config toggles: icon, arrow, distance, size, opacity, color, units, track key, bed fallback, hide in dungeons
- [ ] Thunderstore packaging: manifest, README, icon, zip
- [ ] Settle on the mod name ("Edge Map Marker" is a working name)
