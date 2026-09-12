# Lur

Hildir sells a horn. Sound it inside one of her three dungeons and the mini-boss you already
killed there comes back, so you can fight it again.

## Features

- A horn on Hildir's shelf, 500 coins by default.
- Sounding it inside one of her three dungeons clears the spent mark on the mini-boss's
  spawner. Vanilla's own spawner code does the spawning a second later.
- Wakes the mini-boss and nothing else. Looted chests stay looted, smashed pots stay smashed,
  mined veins stay mined.
- A cooldown per dungeon, measured in in-game days. Five by default.
- The horn is only consumed if something actually wakes. Every refusal names its reason on
  screen.
- Deletes no saved objects, never regenerates a dungeon, never touches terrain and never
  writes a global key.

## The three dungeons

| Setting | Dungeon | Biome |
| --- | --- | --- |
| `WakeCrypt` | Smouldering Tomb | Black Forest |
| `WakeCave` | Howling Cavern | Mountains |
| `WakeTower` | Sealed Tower | Plains |

Lur finds them by the theme flags the game puts on the dungeon generator
(`ForestCryptHildir`, `CaveHildir`, `PlainsFortHildir`), not by prefab or location names.
Ordinary crypts, caves and Fuling camps are never matched.

## Using the horn

Put it on the hotbar and use it there. Using it from the inventory window does nothing; only
the hotbar path is hooked.

You have to be standing inside the dungeon. "Inside" means inside one of the generator's own
room boxes, plus `RoomPadding` metres of slack.

When the horn is accepted:

1. The spent mark is cleared on the mini-boss's spawner, and on the other spawners in its
   spawn group if it has one. A grouped spawner counts spawns across the whole group, so a
   spent neighbour would keep the group shut on its own.
2. Lur watches those spawners for up to `WatchSeconds` (30 by default) and checks what
   actually appears against the creature the boss spawner holds.
3. On a match the horn is consumed and the dungeon is stamped with the current world time,
   which starts the cooldown.
4. On a timeout every spawner is put back exactly as it was and you keep the horn.
5. If something spawns but it is not the boss, the horn is not consumed and the log explains
   it. The spawners are not rolled back in that case, since the spawn has already happened
   and restoring the old mark could leave a live creature beside a spawner free to make
   another.

### What it says and what it means

| Message | Meaning |
| --- | --- |
| Nothing sleeps here. | Not inside one of the three dungeons, that dungeon is switched off in config, two dungeons overlap at this point, or no boss spawner was found in it. |
| The stone is still settling. | The dungeon's rooms are still streaming in. Wait a second and sound it again. |
| This place has not settled. N days yet. | Cooldown. Counted in in-game days. |
| Someone lies unclaimed here. | There is a tombstone inside the dungeon. Collect it first. |
| It is already awake. | The spawner never fired, or the creature it made is still alive. |
| Something built here keeps it away. | Someone has built inside the dungeon and the spawner now sits in a player base area, where vanilla will not spawn. |
| Nothing here answers the horn. | A global key on the spawner blocks it. |
| Something wakes. | Success. The horn is spent. |
| Nothing stirred. | Nothing spawned in time. The horn is not spent. |
| Something else stirs. | Something spawned, but not the boss. The horn is not spent; turn on `Diagnose` and report the census. |

## What it does not restore

Only the mini-boss. Chests you emptied stay empty, props you broke stay broken, veins you
mined stay mined. A repeat run buys the fight, its drops and its trophy, and nothing you
already took. If you want a dungeon that restocks itself,
[Dvala](https://github.com/Ezomic/valheim-dvala) is the mod for that.

Hildir will not accept a second quest chest. Her turn-in is recorded on the world and her own
code refuses an offering whose key the world already holds, before the item is taken. So the
cosmetic reward happens once per world however many times you fight for it.

## Installation

Requires [BepInEx 5.4.2350](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
BepInEx 5 API only; this is not compatible with BepInEx 6.

Through a mod manager it is one install. By hand, put `Lur.dll`, `lur.obj` and `lur.png` in
`BepInEx/plugins/Lur/`. The `.obj` and `.png` are the horn's model and icon and are read off
disk at runtime, so the item cannot be built without them.

Then start the game once and quit. That first run writes the config file, which does not
exist before the mod has loaded.

Version 1.1.0 is built for Valheim 1.0.7 and does not run on pre-1.0 Valheim. 1.0.1 is the
last version for older game builds.

## Configuration

The file is `BepInEx/config/ezomic.valheim.lur.cfg`. Every entry has its reasoning in a
comment above it.

BepInEx writes every entry on first run and the saved value beats a new default in code, so
changing a default in a new version does nothing on a machine that has already run the mod.
If a setting looks like it is being ignored, read the cfg before anything else.

### Lur

| Setting | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | Off leaves the plugin loaded, drops the horn from Hildir's stock and makes sounding it do nothing. Horns already in an inventory stay there. |
| `ItemName` | `Lur` | Display name only. The prefab name is fixed in code, because renaming it would orphan every horn already saved in a world. |
| `ItemDonor` | `SurtlingCore` | Vanilla item the horn borrows its machinery from: ZNetView, ItemDrop, Rigidbody, colliders, floating in water. Its appearance is not used. `SurtlingCore` and `Wood` are tried as fallbacks if the named one is missing. |
| `SoundEffectPrefab` | empty | Vanilla effect prefab played where the horn is sounded. Empty for none. A name that does not resolve is logged once and skipped. |

### Dungeons

| Setting | Default | Effect |
| --- | --- | --- |
| `WakeCrypt` | `true` | Allow waking the Smouldering Tomb. |
| `WakeCave` | `true` | Allow waking the Howling Cavern. |
| `WakeTower` | `true` | Allow waking the Sealed Tower. |
| `CooldownDays` | `5` | In-game days before the same dungeon can be woken again. `0` leaves the horn's price as the only cost. Measured on the world clock, so time with nobody online does not count and a server that sat empty for a week does not come back with everything due at once. |
| `RoomPadding` | `1` | Metres of slack added to each room's box when deciding what counts as inside the dungeon. Raise it if a boss refuses to wake and the log shows its spawner just outside. |
| `UseLocationRadiusFallback` | `false` | Also count anything inside the location's own radius as part of the dungeon, not just the generator's rooms. Looser, and it can reach a spawner standing outside the tower wall. Turn it on only if the diagnostic log shows a boss spawner that is in no room at all. |
| `WatchSeconds` | `30` | How long to wait for something to spawn before giving up and putting the spawners back. On a timeout the horn is not consumed. Must comfortably exceed the spawners' own interval. |

### Store

| Setting | Default | Effect |
| --- | --- | --- |
| `Price` | `500` | What Hildir charges, in coins. Her own stock runs from a few hundred to somewhat over a thousand. |
| `Stack` | `1` | How many horns one purchase buys. Vanilla clamps this to the item's max stack size, which is 10. |
| `SoldAfterKey` | empty | A global key Hildir requires before she stocks the horn. Empty means always on sale, which is the recommended setting, since sounding it already refuses in a dungeon whose boss is still alive. `bosshildir1` is the value for a host who wants the shop gated too. An unknown key is not an error: the row simply never appears. |

### Diagnostics

| Setting | Default | Effect |
| --- | --- | --- |
| `Diagnose` | `false` | Log a full census of every Hildir dungeon as it loads: its generator, its themes, its rooms, and every spawner near it with an inside-or-outside verdict. The first thing to turn on if the horn does nothing. |
| `Verbose` | `false` | Currently reads as no change. The lines its comment describes are written to the log either way. |

## Multiplayer

Install it on every client and on the server, at the same version and build. It registers an
item prefab, and a client that cannot resolve that prefab does not fail loudly: the game
discards the saved object as junk, and a horn sitting in somebody's chest is gone.

With [Longhouse Core](https://github.com/Ezomic/valheim-core) installed, Lur registers with
the version check and the server rejects clients whose version or build id does not match.
Core also applies the host's config values on connected clients in memory only; your own
config file is never written to, and your values are back the moment you disconnect.
`Diagnose` and `Verbose` stay yours. Everything else is the host's, because two clients
disagreeing about what may be woken is two clients disagreeing about the world's ZDOs.

Without Core the mod still runs. What is lost is the enforcement: nothing refuses a client
that does not have it.

Not tested with more than one player. Everything it does has been tested in single player,
where you are also the server.

## Compatibility

Three Harmony patches, all on seams the game already provides: a prefix on `Humanoid.UseItem`
for the hotbar, a postfix on `DungeonGenerator.Awake` that only logs, and a postfix on
`Trader.GetAvailableItems` for the store row. Nothing patches movement, spawning or the zone
system.

The store row is appended to the list Hildir hands out rather than added to the `Trader`
prefab, so other mods that add trader stock are unaffected. Haldor is not touched.

## Troubleshooting

**Hildir does not stock the horn.** Look for `Hildir will stock Lur at 500 coins` in
`BepInEx/LogOutput.log`. If that line is missing the item has not registered, and the usual
cause is `lur.obj` not being in the plugin folder. If the line is there but the row is not,
check `SoldAfterKey`: a key the world does not hold reads as "she does not sell it".

**The horn refuses in a dungeon it should work in.** Turn on `Diagnose`, re-enter the
dungeon, and read the census in `BepInEx/LogOutput.log`. It lists the generator, its themes,
its rooms and every spawner with an inside-or-outside verdict. A boss spawner that reads as
just outside wants a higher `RoomPadding`. One that is in no room at all is the case
`UseLocationRadiusFallback` exists for.

**A vanilla mechanic broke.** Gameplay exceptions land in
`AppData\LocalLow\IronGate\Valheim\Player.log`, not in BepInEx's log.

## Known limitations

- Not tested with more than one player.
- The Sealed Tower is the least proven of the three in play. Its dungeon sits on the ground
  rather than in the sky, and whether its boss spawner is inside a generated room is the
  question `UseLocationRadiusFallback` exists to answer.
- `Verbose` is bound but nothing reads it.

## Bug reports

Report in the [Discord](https://discord.gg/hJzAVaZ5wb) or on the
[issue tracker](https://github.com/Ezomic/valheim-lur/issues). Useful things to attach:

- `BepInEx/LogOutput.log`, ideally with `Diagnose` turned on and after re-entering the
  dungeon.
- Whether you were on a server or in single player.
- Which of the three dungeons it was.
- `AppData\LocalLow\IronGate\Valheim\Player.log` if a vanilla mechanic broke.
- Your `ezomic.valheim.lur.cfg` if you changed anything in it.

## Discord

[discord.gg/hJzAVaZ5wb](https://discord.gg/hJzAVaZ5wb) for mod information, updates, support,
bug reports and compatibility questions.

## Server

There is also a small EU server running the pack if you want somewhere to play. Details are
in the Discord.

## Building

net462, no NuGet. It references Valheim's managed DLLs from the game folder and BepInEx's
from whichever profile it deploys to, so a fresh clone builds with nothing to restore.
`ProfileDir` defaults to the repo's own `testprofile`.

The six horn shapes this one was chosen against are kept in `assets/variants/`, with the
reasoning for each in `tools/lur_designs.py`. The one that ships is `scroll`.

## Part of Longhouse

Lur is in the [Longhouse](https://thunderstore.io/c/valheim/p/Ezomic/Longhouse/) modpack,
which pins the exact versions it ships with. It behaves the same installed on its own.

## Licence

MIT. See [LICENSE](LICENSE). By Robbin Thijssen, Thijssen Software.
