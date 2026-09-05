using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Ezomic.Core;
using Ezomic.Shared;
using HarmonyLib;

namespace Lur
{
    /// <summary>
    /// Lur: a horn Hildir sells, sounded inside one of her three dungeons to wake what was
    /// put down there.
    ///
    /// The three Hildir dungeons are the only content in the game whose whole point is a
    /// single fight, and once it is won they are finished forever. That is the right default
    /// for a world's story and a poor one for a server people keep playing on, because the
    /// fight is the part worth doing twice and the cosmetic reward is the part that is not.
    /// Lur separates them: sounding the horn re-arms the dungeon's spent one-shot spawners so
    /// the boss and everything guarding it walk out again, and touches nothing else. Looted
    /// chests stay looted, smashed props stay smashed, and Hildir will not accept a second
    /// turn-in, because her own Trader refuses an offering whose key the world already holds.
    /// It repopulates; it does not reset. The mod that resets an ordinary crypt is a different
    /// one, and it deletes things, which is exactly why it is not this one.
    ///
    /// Mechanically it is smaller than it sounds. A spent one-shot CreatureSpawner has lost
    /// nothing - the record of its firing is a connection on a ZDO that is still there - so
    /// waking it is two field writes, and vanilla's own UpdateSpawner does the rest a second
    /// later. Lur never destroys a ZDO, never calls DungeonGenerator.Generate and never
    /// touches terrain, which is the whole of its safety argument and must stay true of this
    /// DLL rather than merely of one code path inside it.
    ///
    /// Not client-side, and the distinction matters here. It registers an item prefab, so a
    /// client that cannot resolve the hash does not fail loudly - ZNetScene discards the ZDO
    /// as junk and the horn in somebody's chest is simply gone. That is what makes
    /// Requirement.Everyone below load-bearing rather than a default.
    ///
    /// There is deliberately no BepInProcess attribute. A dedicated server runs
    /// valheim_server.exe, and Core's gate only refuses on the server side of RPC_PeerInfo -
    /// so a mod that must be enforced has to be allowed to load there.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Soft, not hard. A hard dependency that is absent does not degrade - the plugin never
    // loads at all - and every mod here has to be installable on its own, because a stranger
    // should not need two installs to get one mod. Soft still buys the load-order guarantee
    // when Core is present, which is all that registering with the gate needs.
    [BepInDependency(CoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class LurPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.lur";
        public const string PluginName = "Lur";
        public const string PluginVersion = "0.1.0";
        public const string PluginAuthor = "Robbin Thijssen";

        /// <summary>Core's plugin GUID. Optional - see TryRegisterWithCore.</summary>
        private const string CoreGuid = "ezomic.valheim.core";

        internal static ManualLogSource Log;

        /// <summary>
        /// Whether Core answered at load. Worth keeping even when nothing reads it yet: the
        /// difference between gated and ungated is invisible to a player otherwise, and this
        /// is what a warning on spawn would be driven by.
        /// </summary>
        internal static bool CorePresent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config first. Registering absorbs every entry the mod has bound, so anything
            // bound after this line is carried only because Core re-absorbs at manifest
            // time - and depending on the order of two lines in an Awake is not a thing
            // worth relying on.
            LurConfig.Bind(Config);

            TryRegisterWithCore();

            // Ask the world, never a flag. Prefabs re-registers into every ZNetScene and
            // ObjectDB that comes into existence - including after a logout to the menu and
            // back - by checking the live scene each time rather than remembering that it
            // once succeeded. The flag version answers yes to a scene that has never heard of
            // the prefab, registration early-returns, and every ZDO of that prefab is
            // discarded silently. That cost a built piece on 2026-08-16.
            Prefabs.Log = Logger;
            Prefabs.Keep(LurItem.Name, LurItem.Build, item: true);

            // PatchAll over a named type, never the whole assembly. A bare PatchAll() walks
            // every type in the DLL, so a half-written patch class in another file goes live
            // the moment it compiles.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(LurPatches));

            // The startup line every mod in the suite writes. It is how a log answers "which
            // build of what is actually loaded" without anyone guessing.
            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        /// <summary>
        /// Joins Core's version gate when Core is installed, and does nothing when it is not.
        ///
        /// Name here exactly what standing alone costs, because it is usually not the mod.
        /// For most of these it is the *enforcement*: without Core nothing refuses a client
        /// that lacks the plugin, so the rule becomes an agreement between players rather
        /// than a property of the server. That is a real loss and a legitimate choice, and
        /// it is the server owner's to make - which is why this logs rather than refusing
        /// to run.
        /// </summary>
        private void TryRegisterWithCore()
        {
            CorePresent = Chainloader.PluginInfos.ContainsKey(CoreGuid);

            if (!CorePresent)
            {
                Log.LogInfo("Core not installed - running standalone, without the version gate.");
                return;
            }

            RegisterWithCore();
        }

        /// <summary>
        /// Kept separate and never inlined on purpose. The JIT resolves the assemblies a
        /// method needs when it first compiles that method, so a Suite call sitting directly
        /// in Awake would drag Ezomic.Core in before the check above could prevent it - and
        /// the missing-assembly exception would land during plugin load, which is the exact
        /// failure this arrangement exists to avoid. Isolating it means the type is only
        /// ever resolved on a machine that has Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterWithCore()
        {
            // Requirement.Everyone or Requirement.HostOnly, and the choice is not a matter of
            // taste. Everyone for anything that registers a prefab or changes item data,
            // whether it looks networked or not: a client that cannot resolve a prefab hash
            // does not fail loudly, ZNetScene discards the ZDO as junk and the thing a player
            // built is simply gone. HostOnly only when a client without the mod is genuinely
            // unaffected.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config, Requirement.Everyone);

            // Registering already absorbs the whole config file, so naming entries here is a
            // formality. It is still worth writing: it says out loud that the host decides
            // these, and every one of them is a fact about the world rather than a taste.
            // Two clients disagreeing about what may be woken, or about how wide a room box
            // is, is two clients disagreeing about the world's ZDOs.
            Suite.Sync(LurConfig.Enabled, LurConfig.WakeCrypt, LurConfig.WakeCave,
                LurConfig.WakeTower, LurConfig.CooldownDays, LurConfig.RoomPadding,
                LurConfig.WatchSeconds, LurConfig.UseLocationRadiusFallback,
                LurConfig.Price, LurConfig.Stack, LurConfig.SoldAfterKey);

            // The two that are genuinely personal. Core's sync exempts only KeyCode and
            // KeyboardShortcut by default and imposes the host's value back over any local
            // write, so a diagnostic switch left synced would be a host deciding how much
            // somebody else's log file says. Neither changes anything in the world.
            Suite.Local(LurConfig.Diagnose, LurConfig.Verbose);

            // If the mod reads a data file that decides what it does, hash it too. The gate
            // catches two ends on different builds; it cannot catch two ends running the
            // same build over different text unless it is told.
            //
            //     Suite.Data(File.ReadAllText(path));
        }

        /// <summary>
        /// Drives registration. ZNetScene and ObjectDB do not exist at load and are torn down
        /// and rebuilt on every world, so there is no single moment to hook - the answer is a
        /// cheap idempotent check every frame rather than a clever one once.
        /// </summary>
        private void Update()
        {
            Prefabs.Tick();
        }

        private static readonly HashSet<string> Said = new HashSet<string>();

        /// <summary>
        /// A warning worth reading once and not once a frame. Registration retries forever by
        /// design, so anything logged from that path without this turns a missing file into
        /// tens of thousands of identical lines and buries whatever actually went wrong.
        /// </summary>
        internal static void LogOnce(string message)
        {
            if (!Said.Add(message)) return;
            Log.LogWarning(message);
        }

        private void OnDestroy()
        {
            // UnpatchSelf, never UnpatchAll(). The argumentless one unpatches every mod in
            // the process, not just this one.
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
