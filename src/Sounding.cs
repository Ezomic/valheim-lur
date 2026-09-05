using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lur
{
    /// <summary>
    /// Sounding the horn: the only place in this mod that writes anything.
    ///
    /// What it writes is deliberately tiny. Two fields on each spent spawner's ZDO and one
    /// stamp on the generator's, and that is the whole of it. Lur never destroys a ZDO, never
    /// calls DungeonGenerator.Generate, never touches terrain and never walks the scene to
    /// delete things. The boss does not appear because Lur spawned it - it appears because
    /// vanilla's own CreatureSpawner.UpdateSpawner runs a second later and finds a spawner
    /// that has not fired yet.
    ///
    /// Why the whole set of spawners and not just the boss: CheckGroupSpawnBlocked counts
    /// "spawnedEver" across every member of a spawn group, so clearing one spawner and leaving
    /// its neighbours spent leaves the group blocked on a neighbour. Clearing them all is what
    /// makes it work, and it is also why this code needs to know nothing about which of them
    /// is the boss.
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

            LurPlugin.Log.LogInfo("Sounded in " + Dungeons.Describe(dg) + " - re-armed "
                                  + cleared.Count + " spawner(s), watching.");

            Effect(at);

            Watch watch = dg.gameObject.AddComponent<Watch>();
            watch.Begin(cleared, generator, player);
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

            if (now - last >= wait) return true;

            Refuse(player, "This place has not settled since it was last woken.");
            return false;
        }

        /// <summary>
        /// Clears the spent mark on every spawner in the dungeon that can take it.
        ///
        /// Reads first, writes second, and never interleaves them: a spawner's "already fired"
        /// record and its "the creature it made is still alive" record are the same field, so
        /// clearing one before reading the other would defeat both tests at once and duplicate
        /// the boss.
        /// </summary>
        private static List<Cleared> Wake(DungeonGenerator dg, Player player)
        {
            var cleared = new List<Cleared>();

            List<CreatureSpawner> spawners = Dungeons.Spawners(dg);
            int alive = 0;
            int armed = 0;
            int built = 0;
            int keyed = 0;

            foreach (CreatureSpawner spawner in spawners)
            {
                ZNetView nview = spawner.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) continue;

                ZDO zdo = nview.GetZDO();
                if (zdo == null) continue;

                // Read both facts before touching anything.
                ZDOExtraData.ConnectionType type = zdo.GetConnectionType();
                ZDOID target = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);

                if (type == ZDOExtraData.ConnectionType.None)
                {
                    armed++;
                    continue;
                }

                if (!target.IsNone() && ZDOMan.instance.GetZDO(target) != null)
                {
                    alive++;
                    continue;
                }

                // Vanilla's own remaining gates. Honour them and say so rather than override
                // them: a spawner inside a player base will not fire however this mod marks
                // it, and silently re-arming it would spend the horn for nothing.
                if (!spawner.m_spawnInPlayerBase
                    && EffectArea.IsPointInsideArea(
                        spawner.transform.position, EffectArea.Type.PlayerBase) != null)
                {
                    built++;
                    continue;
                }

                if (Blocked(spawner))
                {
                    keyed++;
                    continue;
                }

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
                    "{0}: {1} spawner(s), cleared {2}, {3} still alive, {4} already armed, "
                    + "{5} inside a player base, {6} blocked by a global key.",
                    Dungeons.Describe(dg), spawners.Count, cleared.Count, alive, armed, built,
                    keyed));
            }

            if (cleared.Count != 0) return cleared;

            // Every reason for an empty set reads the same to a player standing in an empty
            // room, so name the ones that are not "you already did this".
            if (built > 0) Refuse(player, "Something built here keeps them away.");
            else if (alive > 0) Refuse(player, "They are already awake.");
            else Refuse(player, "Nothing sleeps here.");

            return cleared;
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

        internal void Begin(List<Sounding.Cleared> cleared, ZDO generator, Player player)
        {
            _cleared = cleared;
            _generator = generator;
            _player = player;
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

                    Sounding.Succeeded(_generator, _player);
                    Done();
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            Sounding.TimedOut(_cleared, _player);
            Done();
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
