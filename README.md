# Edge Map Marker

Never lose your way home. When a tracked place is off your minimap, Edge Map Marker pins its icon to the edge of the minimap in that direction, with an arrow and the distance.

## Features

- **Home by default.** Your bed (or the world spawn if you have no bed) is tracked out of the box.
- **Track any map pin.** Your own markers, discovered locations, boss pins and shared markers.
- **Track other players.** Their marker shows their name and distance, in a colour picked from their character name, so everyone with the mod sees the same player in the same colour.
- **Track as many as you like.** Every tracked target that's off the minimap gets its own edge marker.
- **HUD markers.** Each tracked target also gets a ping-style label on screen with its icon, name and distance, held at the screen edge when it's behind you.
- **Tracked pins are marked** with a small badge (or a ring) on both the large map and the minimap.
- **Saved per character and per world.**
- **Client-side only.** Nothing is synced. Install it on the clients that want it; the server and other players don't need it.

## How to use

Open the large map and hold **Left Alt**:

| Click | On | Does |
| --- | --- | --- |
| Left-click | A pin, another player, or your bed / spawn point | Track it (shown on the minimap and the HUD), or stop tracking it |
| Right-click | A tracked target | Cycle where it shows: minimap + HUD, minimap only, HUD only |

When several are under the cursor, the closest one wins. Alt + right-click never deletes the pin.

Left Alt + H shows or hides all HUD markers, and Left Alt + I shows or hides their icons.

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
| Tracking | Tracked highlight color | green | Lower the alpha to make it fainter |
| HUD | Show HUD markers | on | Ping-style on-screen label for each tracked target |
| HUD | Show icon | on | Map icon above the HUD label |
| HUD | Hide within (m) | 20 | Hide a target's HUD label when you're this close |
| HUD | Toggle HUD markers key | Left Alt + H | Show or hide all HUD markers |
| HUD | Toggle HUD icons key | Left Alt + I | Show or hide the icons above HUD markers |
| Players | Show player names | on | Name next to the distance |
| Players | Color players | on | Per-player colour on the arrow, name and badge |

## Compatibility

- Works alongside **SharedMap**: public markers can be tracked, and stay tracked while the shared map is hidden.
- Uses BepInEx only (no Jotunn).

## Feedback

This mod was written with AI assistance (Claude). Bug reports and suggestions are welcome.
