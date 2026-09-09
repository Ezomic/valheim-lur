# Lur

Hildir sells you a horn. Sound it inside one of her three dungeons and the mini-boss you
already killed there wakes up again, so you can fight it again.

Her three tombs are the only content in Valheim whose whole point is a single fight, and
once it is won they are finished forever. That is the right default for a story and a poor
one for a server people keep playing on, because the fight is the part worth doing twice
and the cosmetic reward is the part that is not. Lur separates them.

**It wakes the boss and nothing else.** Not the draugr on the way in, not the chests, not
the pots. Looted chests stay looted, smashed props stay smashed, mined veins stay mined -
so a repeat run buys the fight, its drops and its trophy, and nothing you already took.

That is a choice and not a limit, and this readme used to claim otherwise. It said most of
what a dungeon loses is deleted rather than changed, so only regenerating a whole room could
put it back. That is wrong. A looted chest, a picked bush, a part-mined vein and a spent
spawner all still exist, each carrying a flag or a value recording what happened to it, and
writing those back restores them without destroying anything. So an emptied crypt filling up
again is perfectly possible - it is simply a different mod with a different argument, and
[Dvala](https://github.com/Ezomic/valheim-dvala) is that mod. Lur still does none of it, on
purpose: the horn is sold as the fight again, not the crypt again.

**Hildir will not accept a second chest.** Her turn-in is recorded on the world, and her own
code refuses an offering whose key the world already holds - before the item is even taken.
So the cosmetic reward happens once per world however many times you fight for it. Sold as
"fight it again", never as "farm Hildir".

Mechanically it is smaller than it sounds, and deliberately so. A spent one-shot spawner has
not lost anything: the record of its firing is a field on an object that is still there. Lur
clears that field and vanilla's own spawner code does the rest a second later. It never
destroys a saved object, never regenerates a dungeon, never touches terrain and never edits
the world's global keys. The horn is taken from you only if something actually wakes.

## Installing

Needs BepInEx. Nothing else. Through a mod manager it is one install. By hand, put
`Lur.dll` in `BepInEx/plugins/Lur/`.

Then start the game once and quit. That first run writes the config file. It does not exist
before the mod has loaded, which is the usual reason people think it is broken.

## Settings

The file is `BepInEx/config/ezomic.valheim.lur.cfg`. Open it in any text editor. Every
setting has a comment above it, so the file explains itself.

Note that changing a default in a new version does nothing on a machine that has already run
the mod. BepInEx writes every entry on first run and the saved value wins.

## Multiplayer

**Everyone needs it.** The server refuses a client that does not have it, at the same build.
That is not caution: Lur registers an item prefab, and a client that cannot resolve the
prefab does not fail loudly - the game discards the saved object as junk, and a horn sitting
in somebody's chest is simply gone.

Untested in multiplayer at the time of writing. Everything it does has been proven in
singleplayer, where you are also the server; the reasoning for why it holds with several
people connected is written down, but reasoning is not a test.

If [Core](https://github.com/Ezomic/valheim-core) is installed, this mod registers with its
version gate and the host's settings apply to everyone connected to it, in memory only -
your own config file is never written to and comes back the moment you disconnect. Keybinds
stay yours. Without Core the mod still runs; what is lost is the enforcement.

## Licence

MIT. See `LICENSE`.
