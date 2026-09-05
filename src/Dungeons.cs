using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lur
{
    /// <summary>
    /// Which dungeon a point is in, and what sleeps inside it.
    ///
    /// Everything here reads. Nothing in this file writes a ZDO, moves an object or spawns
    /// anything - that is <see cref="Sounding"/>'s job, and keeping the split sharp is what
    /// lets the diagnostic census run on every dungeon load without being able to do harm.
    ///
    /// Two decisions worth having in view while reading it.
    ///
    /// <b>Selection is three bit tests, not a name list.</b> Room.Theme carries a dedicated
    /// flag per Hildir dungeon - ForestCryptHildir, CaveHildir, PlainsFortHildir - separate
    /// from plain Crypt, Cave and GoblinCamp. So the trio is addressable off the generator
    /// alone, with no location lookup, no prefab names and no hash table. Note that Room.Theme
    /// is not decorated [Flags], so a multi-theme value stringifies to a plain number: Tekla's
    /// DungeonReset tests <c>m_themes.ToString()</c> against a config string, which is a
    /// substring match and is wrong in both directions - a config listing only "SunkenCrypt"
    /// silently enables plain "Crypt" too. Bit tests are the only correct form.
    ///
    /// <b>Membership is the generator's own room boxes, not a sphere around it.</b> For a sky
    /// crypt any bounding volume would do; its rooms are kilometres from anything else. The
    /// Sealed Tower is on the ground, where a box of m_zoneSize centred on the generator can
    /// reach a neighbouring Fuling camp's spawners, and re-arming those would be a real bug
    /// that only ever appears in one biome. The rooms exclude them by construction.
    /// </summary>
    internal static class Dungeons
    {
        /// <summary>
        /// The three themes, as a mask. Values are Room.Theme's own: ForestCryptHildir 0x200,
        /// CaveHildir 0x400, PlainsFortHildir 0x800.
        /// </summary>
        private const Room.Theme Trio =
            Room.Theme.ForestCryptHildir | Room.Theme.CaveHildir | Room.Theme.PlainsFortHildir;

        /// <summary>
        /// The generator whose rooms contain <paramref name="point"/>, or null.
        ///
        /// Returns null rather than a best guess when two dungeons both match, because the
        /// only thing to do with an ambiguous answer is refuse: waking the wrong dungeon is
        /// not recoverable by the player noticing.
        /// </summary>
        internal static DungeonGenerator Containing(Vector3 point)
        {
            DungeonGenerator found = null;

            foreach (DungeonGenerator dg in Live())
            {
                if (!Enabled(dg.m_themes)) continue;
                if (!Inside(dg, point)) continue;

                // Two dungeons claiming one point. Refuse rather than pick.
                if (found != null)
                {
                    LurPlugin.Log.LogWarning(
                        "Two dungeons overlap at " + point + " - refusing to guess which.");
                    return null;
                }

                found = dg;
            }

            return found;
        }

        /// <summary>
        /// Every live, network-backed dungeon generator in the scene.
        ///
        /// The ZNetView guard is load-bearing rather than defensive. A client-side location
        /// instance carries an inactive duplicate of every networked child, including its
        /// generator: ZoneSystem.SpawnLocation's client branch disables them on the prefab
        /// before instantiating it. So a plain component scan finds shells with no ZDO and no
        /// rooms, and a mod that reads one finds nothing wrong - it finds nothing at all, and
        /// reports the dungeon as empty.
        ///
        /// Root objects, because that is where ZNetScene instantiates. Rooms are the exception
        /// and are reached through the generator, since PlaceRoom parents them to it.
        /// </summary>
        internal static IEnumerable<DungeonGenerator> Live()
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null) continue;

                DungeonGenerator dg;
                if (!root.TryGetComponent(out dg)) continue;
                if (dg == null) continue;

                ZNetView nview;
                if (!root.TryGetComponent(out nview)) continue;
                if (nview == null || !nview.IsValid()) continue;

                yield return dg;
            }
        }

        /// <summary>Whether this generator is one of the trio and switched on in config.</summary>
        internal static bool Enabled(Room.Theme themes)
        {
            if ((themes & Trio) == 0) return false;

            if ((themes & Room.Theme.ForestCryptHildir) != 0) return LurConfig.WakeCrypt.Value;
            if ((themes & Room.Theme.CaveHildir) != 0) return LurConfig.WakeCave.Value;
            if ((themes & Room.Theme.PlainsFortHildir) != 0) return LurConfig.WakeTower.Value;

            return false;
        }

        /// <summary>A readable name for logs. The theme, not the prefab, because that is what
        /// selection actually keys on and so what a confusing log needs to show.</summary>
        internal static string Describe(DungeonGenerator dg)
        {
            if (dg == null) return "<none>";
            return Utils.GetPrefabName(dg.gameObject) + " [" + dg.m_themes + "]";
        }

        /// <summary>
        /// Whether a point is inside this dungeon.
        ///
        /// The test is vanilla's own: rotate the offset into room space and compare against
        /// half the room's size. Deliberately <b>not</b> Transform.InverseTransformPoint,
        /// which also divides by lossy scale - a non-unit scale anywhere in the parent chain
        /// would then silently resize the box. DungeonGenerator.IsInsideDungeon and
        /// Room.OnDrawGizmos both do it rotation-only, and matching them is free.
        ///
        /// Room.m_size is a Vector3Int, so the cast is required and is not noise.
        /// </summary>
        internal static bool Inside(DungeonGenerator dg, Vector3 point)
        {
            if (dg == null) return false;

            float pad = Mathf.Max(0f, LurConfig.RoomPadding.Value);

            Room[] rooms = dg.GetComponentsInChildren<Room>();
            for (int i = 0; i < rooms.Length; i++)
            {
                Room room = rooms[i];
                if (room == null) continue;

                Vector3 local = Quaternion.Inverse(room.transform.rotation)
                                * (point - room.transform.position);
                Vector3 half = (Vector3)room.m_size * 0.5f;

                if (Mathf.Abs(local.x) <= half.x + pad
                    && Mathf.Abs(local.y) <= half.y + pad
                    && Mathf.Abs(local.z) <= half.z + pad)
                {
                    return true;
                }
            }

            // The fallback exists for exactly one unproven case - a Sealed Tower whose boss
            // spawner turns out not to be a child of any generated room. Location.IsInside is
            // an XZ distance test against the exterior radius, so it stays height-independent
            // like the room test and still needs no height constant. Off by default because
            // it is looser and can reach just outside the tower wall.
            if (!LurConfig.UseLocationRadiusFallback.Value) return false;

            Location location = Location.GetLocation(dg.transform.position);
            if (location == null) return false;

            return location.IsInside(point, 0f);
        }

        /// <summary>Whether this dungeon has finished loading its rooms.</summary>
        internal static bool Ready(DungeonGenerator dg)
        {
            if (dg == null) return false;

            // Zero rooms is not an empty dungeon, it is one whose rooms are still coming in
            // asynchronously - Awake, Load, LoadRoomPrefabsAsync, then Spawn. Reading it in
            // that window finds nothing and would report "nothing sleeps here" for a dungeon
            // full of sleepers. Never read m_placedRooms instead: it is static, and it is not
            // filled at all on the client path.
            return dg.GetComponentsInChildren<Room>().Length > 0;
        }

        /// <summary>
        /// Every live creature spawner inside this dungeon.
        ///
        /// Same root scan and same ZNetView guard as <see cref="Live"/>, for the same reason.
        /// </summary>
        internal static List<CreatureSpawner> Spawners(DungeonGenerator dg)
        {
            var found = new List<CreatureSpawner>();
            if (dg == null) return found;

            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null) continue;

                CreatureSpawner spawner;
                if (!root.TryGetComponent(out spawner)) continue;
                if (spawner == null) continue;

                ZNetView nview;
                if (!root.TryGetComponent(out nview)) continue;
                if (nview == null || !nview.IsValid()) continue;

                if (!Inside(dg, root.transform.position)) continue;

                found.Add(spawner);
            }

            return found;
        }

        /// <summary>
        /// Whether anything inside this dungeon is a tombstone.
        ///
        /// A refusal rather than a filter. Waking a dungeon around somebody's unrecovered
        /// grave is cheap to avoid and there is no version of it a player would want.
        /// </summary>
        internal static bool HoldsTombstone(DungeonGenerator dg)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null) continue;

                TombStone stone;
                if (!root.TryGetComponent(out stone)) continue;
                if (stone == null) continue;

                if (Inside(dg, root.transform.position)) return true;
            }

            return false;
        }
    }
}
