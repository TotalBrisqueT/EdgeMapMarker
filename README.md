# Edge Map Marker

Never lose your way home. When a tracked place is off your minimap, Edge Map Marker pins its icon to the edge of the minimap in that direction, with an arrow and the distance.

## Features

- **Home by default.** Your bed (or the world spawn if you have no bed) is tracked out of the box.
- **Track any map pin.** Your own markers, discovered locations, boss pins and shared markers.
- **Track other players.** Their marker shows their name and distance, in a colour picked from their character name, so everyone with the mod sees the same player in the same colour.
- **Track as many as you like.** Every tracked target that's off the minimap gets its own edge marker.
- **Tracked pins are marked** with a small badge (or a ring) on both the large map and the minimap.
- **Saved per character and per world.**
- **Client-side only.** Nothing is synced. Install it on the clients that want it; the server and other players don't need it.

## How to use

Open the large map, hold **Left Alt** and **left-click**:

| Click on | Does |
| --- | --- |
| A pin | Track it, or stop tracking it |
| Your bed / spawn point | Toggle home tracking for this world |
| Another player | Track them, or stop tracking them |

When several are under the cursor, the closest one wins.

Other players can only be tracked while they're visible on the map (vanilla "Visible on map" checkbox, or a server-side mod that forces it).

## Configuration

All settings can be changed in game with a configuration manager (F1), or in `BepInEx/config/SunPheonyx.EdgeMapMarker.cfg`.

| Section | Setting | Default | |
| --- | --- | --- | --- |
| Edge marker | Show icon / Show arrow / Show distance | on | Toggle each part of the edge marker |
| Edge marker | Marker size | 0.7 | 0.3 to 1.5, relative to the game's own pins |
| Tracking | Track key | LeftAlt | Hold while clicking the map to track |
| Tracking | Track home by default | on | For worlds where you haven't toggled home yourself |
| Tracking | Highlight tracked pins | on | Mark tracked pins on the maps |
| Tracking | Highlight style | Badge | Badge (corner dot) or Ring |
| Tracking | Highlight color | amber | Lower the alpha to make it fainter |
| Players | Show player names | on | Name next to the distance |
| Players | Color players | on | Per-player colour on the arrow, name and badge |

## Compatibility

- Works alongside **SharedMap**: public markers can be tracked, and stay tracked while the shared map is hidden.
- Uses BepInEx only (no Jotunn).

## Feedback

This mod was written with AI assistance (Claude). Bug reports and suggestions are welcome.
