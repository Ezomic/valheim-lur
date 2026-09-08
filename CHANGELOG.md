# Changelog

Notable changes to Lur. Format follows [Keep a Changelog](https://keepachangelog.com),
and the mod uses [semantic versioning](https://semver.org).

## [1.0.0] - 2026-09-08

First release.

Hildir sells a horn. Sound it inside one of her three dungeons and the mini-boss you already
killed there wakes up again, so you can fight it again.

### What it does

- **The boss, and nothing else.** Not the draugr on the way in, not the chests, not the pots.
  Looted chests stay looted, smashed props stay smashed, mined veins stay mined. A repeat run
  buys the fight, its drops and its trophy, and nothing you already took.
- **Proven in all three** - the Sealed Tower, the Howling Cavern and the Smouldering Tomb -
  and in none of them does anything come back except the fight.
- **The horn is only spent if something actually wakes.** If the boss is still alive, or
  somebody has built inside the dungeon, it refuses and you keep it.
- **Hildir will not accept a second quest chest.** Her turn-in is recorded on the world and
  her own code refuses an offering whose key the world already holds, before the item is even
  taken. So the cosmetic reward happens once per world however many times you fight for it.
  Sold as "fight it again", never as "farm Hildir".

### Known limits

- **Untested with more than one player.** Everything it does has been proven in singleplayer,
  where you are also the server. The reasoning for why it holds with several people connected
  is written down in the README; reasoning is not a test.
- **The horn's model and icon have never been seen in a running game.** They were changed on
  the day of release - the horn was a single curved blowing horn until 2026-09-07 and is now a
  coiled one - and the mechanic they hang off is unchanged and tested, but how a dropped horn
  rests on the ground is not something the build can check.

### Note for anyone reading the source

The six horns this one was chosen against are kept in `assets/variants/`, each with the
reasoning for its shape - and for its rejection - in `tools/lur_designs.py`. The one that
ships is `scroll`.
