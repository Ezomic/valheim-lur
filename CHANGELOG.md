# Changelog

Notable changes to Lur. Format follows [Keep a Changelog](https://keepachangelog.com),
and the mod uses [semantic versioning](https://semver.org).

## [1.1.1] - 2026-09-12

### Changed

- Rewritten README. Same mod, clearer documentation: what it does and how to install it come
  first, then configuration, multiplayer behaviour, compatibility and troubleshooting. Every
  config table was checked against the plugin's own Config.Bind calls, so the settings,
  sections and defaults listed are the ones actually bound. No code changed in this release.

## [1.1.0] - 2026-09-10

Rebuilt for Valheim 1.0. This version does not run on pre-1.0 Valheim, and the previous one
does not run on 1.0.

### Fixed

- **Hildir offered the horn at 12345 coins, and would not sell it.** Valheim 1.0 grew
  Trader.TradeItem from four fields to eleven, and the store reads one of the new ones without
  a guard - `if (tradeItem.m_tooltip.Length > 0)`. A row built in code leaves unset strings
  null where Unity's own rows come from an asset and hold "", so drawing this row threw. The
  price label is written below the throw, so it kept the list template's placeholder text; the
  click listener is added lower still, so the row could not be bought at all. Every string on
  the row is now set explicitly, including the ones this mod has no use for, so the next
  release that adds a field does not repeat it.

## [1.0.1] - 2026-09-09

### Fixed

- **The readme explained itself with something untrue.** It said most of what a dungeon loses
  is deleted rather than changed, so only regenerating a whole room could put it back, and
  that Lur therefore *could not* restock a crypt even in principle. Reading the game says
  otherwise: a looted chest, a picked bush, a part-mined vein and a spent spawner all still
  exist, each carrying a flag or a value recording what happened to it, and writing those back
  restores them without destroying anything.

  Nothing in the mod changes. Lur still wakes the boss and nothing else, and that is a choice
  rather than a limit - the horn is sold as the fight again, not the crypt again. But the
  reason given for it was wrong, and somebody reading it to decide whether a restocking mod
  was even possible would have been told no. It points at Dvala now, which is that mod.

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
