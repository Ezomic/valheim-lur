using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lur
{
    /// <summary>
    /// Sounding the horn: the only place in this mod that writes anything.
    ///
    /// What it writes is deliberately tiny. Two fields on one spawner's ZDO and one stamp on
    /// the generator's, and in the ordinary case that is the whole of it. Lur never destroys a
    /// ZDO, never calls DungeonGenerator.Generate, never touches terrain and never walks the
    /// scene to delete things. The boss does not appear because Lur spawned it - it appears
    /// because vanilla's own CreatureSpawner.UpdateSpawner runs a second later and finds a
    /// spawner that has not fired yet.
    ///
    /// <b>The boss and nothing else.</b> The escort on the way in belongs to whatever mod
    /// repopulates ordinary dungeons, and that is a different mechanism rather than a smaller
    /// version of this one: most of what a crypt loses is destroyed ZDOs, which only
    /// regeneration restores. See <see cref="Wake"/> for the single case where a groupmate has
    /// to be woken alongside the boss, and why it is forced rather than chosen.
    ///
    /// <b>The horn is taken on success, not on use.</b> If nothing stirs within WatchSeconds
    /// the connections are put back and the player keeps it. That ordering is the reason the
    /// watch exists at all - without it, a refusal that happened for a reason Lur cannot see
    /// would cost a purchase.
    /// </summary>
    internal static class Sounding
    {
        /// <summary>
        /// Our one custom ZDO key, on the generator's own ZDO. An unrecognised key hash on a
        /// vanilla ZDO is an ignored dictionary entry, so uninstalling Lur leaves nothing
        /// behind that matters and costs no ZDOs.
        /// </summary>
        internal static readonly int SoundedKey = "ezomic_lur_sounded".GetStableHashCode();

        /// <summary>What was cleared, and what it was before, so a timeout can undo it.</summary>
        internal struct Cleared
        {
            internal ZDO Zdo;
            internal ZDOExtraData.ConnectionType Type;
            internal ZDOID Target;
        }

        /// <summary>
        /// Runs the whole gesture. Returns true if the horn was accepted and something is now
        /// being watched for; false means nothing was written and nothing consumed.
        /// </summary>
        internal static bool Sound(Player player)
        {
            if (player == null) return false;

            Vector3 at = player.transform.position;

            DungeonGenerator dg = Dungeons.Containing(at);
            if (dg == null)
            {
                Refuse(player, "Nothing sleeps here.");
                return false;
            }

            // Rooms still streaming in. Refusing costs the player a second and consumes
            // nothing; reporting "nothing sleeps here" for a dungeon that is merely still
            // loading would be a lie they could not tell from the truth.
            if (!Dungeons.Ready(dg))
            {
                Refuse(player, "The stone is still settling.");
                return false;
            }

            ZNetView nview = dg.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                Refuse(player, "Nothing sleeps here.");
                return false;
            }

            ZDO generator = nview.GetZDO();
            if (generator == null)
            {
                Refuse(player, "Nothing sleeps here.");
                return false;
            }

            if (!OffCooldown(generator, player)) return false;

            if (Dungeons.HoldsTombstone(dg))
            {
                Refuse(player, "Someone lies unclaimed here.");
                return false;
            }

            List<Cleared> cleared = Wake(dg, player);
            if (cleared.Count == 0) return false;

            // Name what was woken, always, not behind Verbose. A horn spent on the wrong
            // spawner and a horn spent on the right one produced identical logs, which is how
            // an evening went on "the boss did not appear" with no way to tell whether the mod
            // picked wrong or the creature walked off.
            CreatureSpawner boss = Dungeons.Boss(dg);
            GameObject wanted = boss != null ? boss.m_creaturePrefab : null;

            LurPlugin.Log.LogInfo("Sounded in " + Dungeons.Describe(dg) + " - re-armed "
                + cleared.Count + " spawner(s), boss spawner "
                + (boss != null ? Utils.GetPrefabName(boss.gameObject) : "<none>")
                + " at " + (boss != null ? boss.transform.position.ToString("F1") : "-")
                + " holding " + (wanted != null ? wanted.name : "<none>") + ", watching.");

            Effect(at);

            Watch watch = dg.gameObject.AddComponent<Watch>();
            watch.Begin(cleared, generator, player,
                wanted != null ? wanted.name.GetStableHashCode() : 0);
            return true;
        }

        // ------------------------------------------------------------------ the gates

        /// <summary>
        /// In-game days since this dungeon was last woken.
        ///
        /// The world clock, not the wall clock. A server that sits empty for a week must not
        /// come back with every dungeon due at once, which is exactly what real seconds would
        /// do. ZNet's time is saved with the world, so this survives a restart too.
        ///
        /// EnvMan.GetCurrentDay() would read better and is not available: it is private in
        /// this build. Deriving the same number from the two public values it uses costs one
        /// line and no reflection.
        /// </summary>
        private static bool OffCooldown(ZDO generator, Player player)
        {
            float days = Mathf.Max(0f, LurConfig.CooldownDays.Value);
            if (days <= 0f) return true;

            long last = generator.GetLong(SoundedKey, 0L);
            if (last == 0L) return true;

            long now = (long)ZNet.instance.GetTimeSeconds();
            long dayLength = EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : 1200L;
            long wait = (long)(days * dayLength);
            long left = wait - (now - last);

            if (left <= 0L) return true;

            // Say how long. "This place has not settled" reads identically at four minutes
            // and four days, and a refusal a player cannot act on is the one that gets read
            // as a broken mod - which is the whole reason every other message here names its
            // cause rather than just declining.
            //
            // Rounded up and counted in the game's days, not the wall clock's, because that is
            // the unit the cooldown is set in: telling somebody "100 minutes" when the config
            // says 5 would be answering a question they did not ask, in units the setting does
            // not use, and it would be wrong the moment a mod changes day length.
            long days_left = (left + dayLength - 1) / dayLength;

            Refuse(player, days_left <= 1L
                ? "This place has not settled. Not long now."
                : "This place has not settled. " + days_left + " days yet.");

            return false;
        }

        /// <summary>
        /// Clears the spent mark on the mini-boss, and on nothing else that can be avoided.
        ///
        /// <b>The boss alone is the whole feature.</b> The draugr and skeletons on the way in
        /// are an ordinary dungeon's population, and repopulating an ordinary dungeon is a
        /// different mod with a different mechanism - it has to regenerate, because most of
        /// what a crypt loses is destroyed ZDOs rather than modified ones. Lur waking the
        /// escort as well would be that mod doing half its job badly, in a DLL whose entire
        /// safety argument is that it deletes nothing.
        ///
        /// The one exception is not a choice. CheckGroupSpawnBlocked returns false immediately
        /// unless the spawner is grouped, so an ungrouped boss is cleared entirely on its own
        /// - which is the expected case and touches exactly one ZDO. A <i>grouped</i> boss
        /// counts spawnedEver across every member, so a spent neighbour holds the count at the
        /// maximum and vanilla refuses to spawn it at all. Waking the group with it is the
        /// minimum that makes the boss reachable, and the log says when that happened so it is
        /// never a silent widening.
        ///
        /// Reads first, writes second, and never interleaved: a spawner's "already fired"
        /// record and its "the creature it made is still alive" record are the same field, so
        /// clearing one before reading the other would defeat both tests at once and duplicate
        /// the boss.
        /// </summary>
        private static List<Cleared> Wake(DungeonGenerator dg, Player player)
        {
            var cleared = new List<Cleared>();

            List<CreatureSpawner> spawners = Dungeons.Spawners(dg);
            CreatureSpawner boss = Dungeons.Boss(dg);

            if (boss == null)
            {
                // Not the same as "already woken", and worth saying loudly in the log: it
                // means no spawner in this dungeon holds a creature whose death the world
                // records, which is either a dungeon Lur should not be in or a reading the
                // diagnostic pass needs to explain.
                LurPlugin.Log.LogWarning(Dungeons.Describe(dg) + ": no boss spawner found "
                    + "among " + spawners.Count + " spawner(s). Turn on Diagnose and revisit "
                    + "to see what each of them holds.");
                Refuse(player, "Nothing sleeps here.");
                return cleared;
            }

            // Judge the boss before writing anything at all. Clearing its groupmates first and
            // then discovering the boss itself is blocked would leave the dungeon's population
            // altered for nothing - the one outcome this mod must never produce.
            string refusal;
            if (!Clearable(boss, out refusal))
            {
                LurPlugin.Log.LogInfo(Dungeons.Describe(dg) + ": boss spawner not clearable - "
                                      + refusal);
                Refuse(player, refusal);
                return cleared;
            }

            var targets = new List<CreatureSpawner> { boss };

            List<CreatureSpawner> mates = Dungeons.Groupmates(boss, spawners);
            if (mates.Count > 0)
            {
                LurPlugin.Log.LogInfo(string.Format(
                    "{0}: the boss shares spawn group {1} with {2} other spawner(s), so they "
                    + "are woken with it - the group counts spawnedEver across all members and "
                    + "a spent neighbour would keep the boss blocked.",
                    Dungeons.Describe(dg), boss.m_spawnGroupID, mates.Count));

                targets.AddRange(mates);
            }

            foreach (CreatureSpawner spawner in targets)
            {
                ZDO zdo = ZdoOf(spawner);
                if (zdo == null) continue;

                // Re-read rather than carrying the values from Clearable: the judgement above
                // and the write below are separate moments, and the connection is the field
                // both tests key on.
                ZDOExtraData.ConnectionType type = zdo.GetConnectionType();
                ZDOID target = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);

                string ignored;
                if (!Clearable(spawner, out ignored)) continue;

                // Ownership first: UpdateSpawner opens with an IsOwner check, and
                // SetOwnerInternal takes effect synchronously, so this is what makes the
                // spawner ours to run this frame.
                zdo.SetOwner(ZDOMan.GetSessionID());

                // alive_time before the connection, so an abort between the two leaves the
                // spawner still gated rather than half-armed.
                zdo.Set(ZDOVars.s_aliveTime, 0L);
                zdo.SetConnection(ZDOExtraData.ConnectionType.None, ZDOID.None);

                cleared.Add(new Cleared { Zdo = zdo, Type = type, Target = target });
            }

            if (LurConfig.Verbose.Value || cleared.Count == 0)
            {
                LurPlugin.Log.LogInfo(string.Format(
                    "{0}: {1} spawner(s) present, boss is {2}, cleared {3}.",
                    Dungeons.Describe(dg), spawners.Count,
                    Utils.GetPrefabName(boss.gameObject), cleared.Count));
            }

            if (cleared.Count == 0) Refuse(player, "Nothing sleeps here.");

            return cleared;
        }

        /// <summary>
        /// Whether this spawner can be woken, and if not, what to tell the player.
        ///
        /// Every gate here is vanilla's own. Honouring them and reporting them is the point:
        /// a spawner inside a player base will not fire however this mod marks it, so
        /// re-arming it anyway would spend the horn and produce nothing, which is exactly the
        /// failure the watch and this check exist to prevent between them.
        /// </summary>
        private static bool Clearable(CreatureSpawner spawner, out string refusal)
        {
            refusal = "Nothing sleeps here.";

            ZDO zdo = ZdoOf(spawner);
            if (zdo == null) return false;

            if (zdo.GetConnectionType() == ZDOExtraData.ConnectionType.None)
            {
                // Never fired, so there is nothing to undo. In a Hildir dungeon this almost
                // always means the boss is standing in the next room.
                refusal = "It is already awake.";
                return false;
            }

            ZDOID target = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
            if (!target.IsNone() && ZDOMan.instance.GetZDO(target) != null)
            {
                refusal = "It is already awake.";
                return false;
            }

            if (!spawner.m_spawnInPlayerBase
                && EffectArea.IsPointInsideArea(
                    spawner.transform.position, EffectArea.Type.PlayerBase) != null)
            {
                refusal = "Something built here keeps it away.";
                return false;
            }

            if (Blocked(spawner))
            {
                refusal = "Nothing here answers the horn.";
                return false;
            }

            return true;
        }

        private static ZDO ZdoOf(CreatureSpawner spawner)
        {
            if (spawner == null) return null;

            ZNetView nview = spawner.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return null;

            return nview.GetZDO();
        }

        private static bool Blocked(CreatureSpawner spawner)
        {
            List<string> keys = ZoneSystem.instance.GetGlobalKeys();

            if (!string.IsNullOrEmpty(spawner.m_blockingGlobalKey)
                && keys.Contains(spawner.m_blockingGlobalKey))
            {
                return true;
            }

            return !string.IsNullOrEmpty(spawner.m_requiredGlobalKey)
                   && !keys.Contains(spawner.m_requiredGlobalKey);
        }

        // ------------------------------------------------------------------ the outcome

        /// <summary>Called by the watch when something actually spawned.</summary>
        internal static void Succeeded(ZDO generator, Player player)
        {
            if (generator != null)
            {
                generator.Set(SoundedKey, (long)ZNet.instance.GetTimeSeconds());
            }

            LurPlugin.Log.LogInfo("Something woke - the horn is spent.");

            if (player == null) return;

            Consume(player);
            player.Message(MessageHud.MessageType.Center, "Something wakes.");
        }

        /// <summary>Called by the watch when nothing spawned in time. Puts it all back.</summary>
        internal static void TimedOut(List<Cleared> cleared, Player player)
        {
            foreach (Cleared record in cleared)
            {
                if (record.Zdo == null) continue;
                record.Zdo.SetConnection(record.Type, record.Target);
            }

            LurPlugin.Log.LogInfo("Nothing stirred - put " + cleared.Count
                                  + " spawner(s) back as they were. The horn was not spent.");

            if (player == null) return;

            player.Message(MessageHud.MessageType.Center, "Nothing stirred.");
        }

        private static void Consume(Player player)
        {
            Inventory inventory = player.GetInventory();
            if (inventory == null) return;

            // isPrefabName, because the display name is a config entry and a player who
            // renames it must not end up with a horn that cannot be spent.
            ItemDrop.ItemData held = inventory.GetItem(LurItem.Name, -1, true);
            if (held == null) return;

            inventory.RemoveOneItem(held);
        }

        private static void Effect(Vector3 at)
        {
            string name = LurConfig.SoundEffectPrefab.Value;
            if (string.IsNullOrEmpty(name)) return;

            GameObject prefab = ZNetScene.instance.GetPrefab(name);
            if (prefab == null)
            {
                LurPlugin.LogOnce("Sound effect prefab '" + name + "' does not exist - the "
                                  + "horn is silent. Anything else about it is unaffected.");
                return;
            }

            Object.Instantiate(prefab, at, Quaternion.identity);
        }

        private static void Refuse(Player player, string message)
        {
            if (player == null) return;
            player.Message(MessageHud.MessageType.Center, message);
        }
    }

    /// <summary>
    /// Waits to see whether vanilla actually spawns something, then either stamps the dungeon
    /// and takes the horn, or puts every connection back.
    ///
    /// It lives on the generator's own GameObject so it dies with the dungeon. A player who
    /// walks out mid-watch takes the whole thing with them, which is the right outcome: the
    /// dungeon unloads, its ZDOs go back to whatever the owning peer holds, and nothing here
    /// is left half-applied.
    ///
    /// Success is read off the spawners rather than by looking for a creature. A spawner that
    /// has fired writes a fresh Spawned connection, and that write is vanilla's own - so this
    /// watches for the game agreeing that it worked, not for something that merely looks like
    /// it did.
    /// </summary>
    internal class Watch : MonoBehaviour
    {
        private List<Sounding.Cleared> _cleared;
        private ZDO _generator;
        private Player _player;
        private int _wanted;

        internal void Begin(List<Sounding.Cleared> cleared, ZDO generator, Player player,
            int wantedPrefabHash)
        {
            _cleared = cleared;
            _generator = generator;
            _player = player;
            _wanted = wantedPrefabHash;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            float deadline = Time.time + Mathf.Max(1f, LurConfig.WatchSeconds.Value);

            while (Time.time < deadline)
            {
                foreach (Sounding.Cleared record in _cleared)
                {
                    if (record.Zdo == null) continue;
                    if (record.Zdo.GetConnectionType() != ZDOExtraData.ConnectionType.Spawned)
                    {
                        continue;
                    }

                    // A Spawned connection proves something was created. It does not prove the
                    // boss was, and those were indistinguishable until an evening was spent on
                    // the difference. Resolve what actually appeared and check it is the
                    // creature the boss spawner holds.
                    if (!IsWanted(record)) { Done(); yield break; }

                    Sounding.Succeeded(_generator, _player);
                    Done();
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            Sounding.TimedOut(_cleared, _player);
            Done();
        }

        /// <summary>
        /// Whether the thing that spawned is the thing the horn was sounded for.
        ///
        /// On a mismatch the horn is <b>not</b> consumed and the connections are <b>not</b>
        /// rolled back. Not consuming is the honest half: the player did not get what they
        /// paid for. Not rolling back is the safe half, and it is the less obvious one - the
        /// spawn has already happened, so restoring the old spent mark would leave a live
        /// creature beside a spawner free to make another, and duplicating a boss is a worse
        /// outcome than an unexplained horn. The log carries the explanation instead.
        /// </summary>
        private bool IsWanted(Sounding.Cleared record)
        {
            if (_wanted == 0) return true;

            ZDOID target = record.Zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
            if (target.IsNone()) return true;

            ZDO made = ZDOMan.instance.GetZDO(target);
            if (made == null) return true;

            if (made.GetPrefab() == _wanted) return true;

            LurPlugin.Log.LogError("Something spawned, but not what the horn was for - "
                + "expected prefab hash " + _wanted + ", got " + made.GetPrefab()
                + " at " + made.GetPosition().ToString("F1")
                + ". The horn was not spent. This means the boss spawner was mis-identified, "
                + "so turn on Diagnose and report the census.");

            if (_player != null)
                _player.Message(MessageHud.MessageType.Center, "Something else stirs.");

            return false;
        }

        private void Done()
        {
            _cleared = null;
            _generator = null;
            _player = null;
            Destroy(this);
        }
    }
}
