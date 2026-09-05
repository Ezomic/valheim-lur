using BepInEx.Configuration;

namespace Lur
{
    /// <summary>
    /// Everything tunable, bound in one place so the .cfg reads as a document rather than as
    /// whatever order the code happened to need things in.
    ///
    /// Note the standing BepInEx trap: every entry is written to disk on first run and the
    /// saved value beats a new default in code. Changing a default here does nothing on a
    /// machine that has already run the plugin - edit
    /// <c>&lt;profile&gt;\BepInEx\config\ezomic.valheim.lur.cfg</c> as part of the same
    /// change. When a config-driven change appears to do nothing in game, read the cfg
    /// before reading any code.
    ///
    /// Almost everything here is host-authoritative, and that is not caution. Core's sync
    /// exempts only KeyCode and KeyboardShortcut, and the default is right: two clients
    /// disagreeing about what may be woken, or about how wide a room box is, is two clients
    /// disagreeing about the world's ZDOs. Only the two diagnostic switches are personal, and
    /// they are declared so explicitly with Suite.Local in the plugin.
    /// </summary>
    internal static class LurConfig
    {
        internal static ConfigEntry<bool> Enabled;

        internal static ConfigEntry<bool> WakeCrypt;
        internal static ConfigEntry<bool> WakeCave;
        internal static ConfigEntry<bool> WakeTower;

        internal static ConfigEntry<float> CooldownDays;
        internal static ConfigEntry<float> RoomPadding;
        internal static ConfigEntry<float> WatchSeconds;
        internal static ConfigEntry<bool> UseLocationRadiusFallback;

        internal static ConfigEntry<int> Price;
        internal static ConfigEntry<int> Stack;
        internal static ConfigEntry<string> SoldAfterKey;

        internal static ConfigEntry<string> ItemName;
        internal static ConfigEntry<string> ItemDonor;
        internal static ConfigEntry<string> SoundEffectPrefab;

        internal static ConfigEntry<bool> Diagnose;
        internal static ConfigEntry<bool> Verbose;

        internal static void Bind(ConfigFile cfg)
        {
            const string general = "Lur";
            const string dungeons = "Dungeons";
            const string store = "Store";
            const string diagnostics = "Diagnostics";

            // Every mod here has one, and it means the same thing every time: loaded, bound,
            // patched, and deciding nothing. Not "unloaded" - a plugin cannot unload itself,
            // and a switch that pretends otherwise is a lie somebody will debug.
            Enabled = cfg.Bind(general, "Enabled", true,
                "Off leaves the plugin loaded and changing nothing. The horn still exists in "
                + "any inventory that holds one; sounding it simply does nothing.");

            ItemName = cfg.Bind(general, "ItemName", "Lur",
                "The horn's display name. This is the label only. The prefab name is fixed in "
                + "code and can never change: ZNetScene keys on it and discards the ZDO of "
                + "anything it cannot resolve, so a rename would delete every horn already "
                + "sitting in a chest, silently.");

            // One per dungeon rather than one list, so the Sealed Tower can be held back on
            // its own while the question in the design doc's section 7 is still open - whether
            // its boss spawner sits inside a generator room at all. If it does not, Lur does
            // nothing there and this is the switch that says so out loud.
            WakeCrypt = cfg.Bind(dungeons, "WakeCrypt", true,
                "Wake the Smouldering Tomb (Hildir's Black Forest crypt).");

            WakeCave = cfg.Bind(dungeons, "WakeCave", true,
                "Wake the Howling Cavern (Hildir's mountain cave).");

            WakeTower = cfg.Bind(dungeons, "WakeTower", true,
                "Wake the Sealed Tower (Hildir's plains fortress). This one is a ground "
                + "location rather than a sky interior. It needs no separate code path - Lur "
                + "deletes nothing and so has no height test anywhere - but it is the one of "
                + "the three least proven in play.");

            // In-game days, not real time. A server that sits empty for a week must not come
            // back with every dungeon due at once, which is what a wall clock would do. It is
            // read off ZNet.GetTime(), which is saved with the world and survives a restart.
            CooldownDays = cfg.Bind(dungeons, "CooldownDays", 5f,
                "In-game days before the same dungeon can be woken again. 0 leaves the horn's "
                + "price as the only cost. Measured on the world clock, so time spent with "
                + "nobody online does not count.");

            // Slack on each room box. A spawner set flush against a wall can sit a few
            // centimetres outside the room's own bounds, and the cost of being slightly
            // generous here is nothing: Lur only ever re-arms spawners, so the worst case of
            // an over-wide box is waking one more sleeper than intended, in a dungeon the
            // player is already standing in.
            RoomPadding = cfg.Bind(dungeons, "RoomPadding", 1f,
                "Metres of slack added to each room's box when deciding what is inside the "
                + "dungeon. Raise it if a boss refuses to wake and the log shows its spawner "
                + "just outside.");

            UseLocationRadiusFallback = cfg.Bind(dungeons, "UseLocationRadiusFallback", false,
                "Also count anything inside the Location's own radius as part of the dungeon, "
                + "not just the generator's rooms. Off by default because it is looser and can "
                + "reach a spawner standing outside the tower wall. Turn it on only if the "
                + "diagnostic log shows the tower's boss spawner is not in any room - that is "
                + "the single case it exists for.");

            WatchSeconds = cfg.Bind(dungeons, "WatchSeconds", 30f,
                "How long to wait for something to actually spawn before giving up and putting "
                + "the spawners back as they were. On a timeout the horn is not consumed. Must "
                + "comfortably exceed the spawners' own interval.");

            Price = cfg.Bind(store, "Price", 500,
                "What Hildir charges, in coins. Her own stock runs a few hundred to somewhat "
                + "over a thousand, so this sits mid-range. Together with CooldownDays it is "
                + "the entire cost of a rematch.");

            Stack = cfg.Bind(store, "Stack", 1,
                "How many horns one purchase buys. Vanilla clamps this to the item's own max "
                + "stack size at the till.");

            // Deliberately empty. If the boss is alive its spawner was never spent, so
            // sounding the horn there refuses on its own - a horn cannot be used on a fight
            // that was never won. A key on the store row would restate in the shop what the
            // dungeon already enforces. It exists because a host may want the row hidden too.
            SoldAfterKey = cfg.Bind(store, "SoldAfterKey", "",
                "A global key Hildir requires before she will stock the horn. Empty means "
                + "always on sale, which is the recommended setting: sounding it already "
                + "refuses in a dungeon whose boss is still alive. 'bosshildir1' is the value "
                + "for a host who wants the shop gated as well. An unknown key is not an "
                + "error - the row simply never appears, so a typo reads as 'she does not "
                + "sell it'.");

            // Only its machinery is borrowed - the ZNetView, the ItemDrop, the Rigidbody, the
            // colliders and the float-in-water behaviour. The mesh and icon are the mod's own.
            // Configurable because which vanilla item yields a clean material donor is a
            // question for the game rather than a guess, and because a game update can retire
            // a prefab. SurtlingCore and Wood are tried after this one.
            ItemDonor = cfg.Bind(general, "ItemDonor", "SurtlingCore",
                "Vanilla item to take the horn's machinery from. Its appearance is not used.");

            SoundEffectPrefab = cfg.Bind(general, "SoundEffectPrefab", "",
                "Name of a vanilla effect prefab to play where the horn is sounded. Empty for "
                + "none. Resolved through ZNetScene; a name that does not exist is logged and "
                + "skipped, so a rename in a game update costs the noise and nothing else.");

            // Not synced, by intent. A host turning on somebody else's logging is not a thing
            // anybody asked for, and neither of these changes anything in the world.
            Diagnose = cfg.Bind(diagnostics, "Diagnose", false,
                "Log a full census of every Hildir dungeon as it loads: its generator, its "
                + "themes, its rooms, and every spawner near it with an inside-or-outside "
                + "verdict. This is how the open questions about the Sealed Tower get "
                + "answered, and it is the first thing to turn on if the horn does nothing.");

            Verbose = cfg.Bind(diagnostics, "Verbose", false,
                "Write what was found and what was changed to BepInEx/LogOutput.log. Note that "
                + "exceptions land in Player.log instead, not here.");
        }
    }
}
