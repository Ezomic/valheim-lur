using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Lur
{
    /// <summary>
    /// The mod's Harmony patches. One class named in the plugin's PatchAll, so nothing goes
    /// live merely by being written.
    ///
    /// Three of them, and each is on a seam vanilla already provides rather than on anything
    /// this mod would have to own: the hotbar's use path, the dungeon's own Awake, and the
    /// trader's own stock list. Nothing here patches movement, spawning or the zone system.
    /// </summary>
    internal static class LurPatches
    {
        // ------------------------------------------------------------------ sounding it

        /// <summary>
        /// Using the horn from the hotbar.
        ///
        /// Humanoid.UseItem(Inventory, ItemData, bool) is what Player.UseHotbarItem calls with
        /// (null, item, fromInventoryGui: false). It returns void, so this prefix returning
        /// false simply means vanilla's own handling is skipped - which is what we want, since
        /// vanilla's else-branch would try ToggleEquipped and then tell the player they cannot
        /// use it.
        ///
        /// The whole body is wrapped. A prefix that throws here breaks item use in general -
        /// every item, not just this one - and it presents as a vanilla bug with a clean
        /// BepInEx log, because gameplay exceptions land in Player.log instead. Catching costs
        /// the feature and never the game.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
        private static bool UseItem(Humanoid __instance, ItemDrop.ItemData item,
            bool fromInventoryGui)
        {
            try
            {
                if (!LurConfig.Enabled.Value) return true;
                if (__instance == null || __instance != Player.m_localPlayer) return true;
                if (fromInventoryGui) return true;
                if (item == null || item.m_dropPrefab == null) return true;
                if (item.m_dropPrefab.name != LurItem.Name) return true;

                Sounding.Sound(Player.m_localPlayer);
                return false;
            }
            catch (System.Exception e)
            {
                LurPlugin.Log.LogError("Sounding the horn threw, so it did nothing: " + e);
                return true;
            }
        }

        // ------------------------------------------------------------------ diagnostics

        /// <summary>
        /// One line per Hildir dungeon as it loads, and a full census when Diagnose is on.
        ///
        /// This exists because locations are SoftReferences inside ZoneSystem.m_locations and
        /// sit in neither ZNetScene nor ObjectDB, so a devkit rip answers "Not found" for all
        /// three of them however real they are. The house rule for that case is to make the
        /// mod log what it is actually using, and this is that log. It is how the open
        /// questions get answered: whether the theme-to-generator pairing is what the names
        /// imply, and whether the Sealed Tower's boss spawner sits inside a generated room at
        /// all - which is the one thing that decides whether Lur works there.
        /// </summary>
        // Awake is private, so the target is named as a string rather than with nameof. That
        // is the one place in this mod where a typo would not be caught by the compiler: a
        // wrong name here is a patch that fails to apply at load and then quietly never runs,
        // which is exactly the failure the startup log line exists to make visible.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DungeonGenerator), "Awake")]
        private static void GeneratorAwake(DungeonGenerator __instance)
        {
            try
            {
                if (__instance == null) return;

                // Themes are on the component and readable immediately; rooms are not, so the
                // census has to wait for the async load rather than reporting zero.
                __instance.StartCoroutine(Census(__instance));
            }
            catch (System.Exception e)
            {
                LurPlugin.Log.LogError("Dungeon census threw: " + e);
            }
        }

        private static IEnumerator Census(DungeonGenerator dg)
        {
            // Long enough for LoadRoomPrefabsAsync to have called Spawn on any reasonable
            // machine. Nothing depends on the number: a dungeon that is still loading logs
            // zero rooms and says so, which is itself the useful reading.
            yield return new WaitForSeconds(5f);

            if (dg == null) yield break;
            if ((dg.m_themes & (Room.Theme.ForestCryptHildir | Room.Theme.CaveHildir
                                | Room.Theme.PlainsFortHildir)) == 0)
            {
                yield break;
            }

            Room[] rooms = dg.GetComponentsInChildren<Room>();
            Vector3 at = dg.transform.position;

            LurPlugin.Log.LogInfo(string.Format(
                "Hildir dungeon: {0} themes={1} at {2} interior={3} rooms={4} enabled={5}",
                Utils.GetPrefabName(dg.gameObject), dg.m_themes, at,
                Character.InInterior(at), rooms.Length, Dungeons.Enabled(dg.m_themes)));

            if (!LurConfig.Diagnose.Value) yield break;

            for (int i = 0; i < rooms.Length; i++)
            {
                Room room = rooms[i];
                if (room == null) continue;

                LurPlugin.Log.LogInfo(string.Format("  room {0}: {1} size={2} at {3}",
                    i, room.name, room.m_size, room.transform.position));
            }

            // Every spawner near the dungeon, with the verdict that decides whether Lur can
            // ever reach it. An "outside" verdict on the tower's boss spawner is the evidence
            // that turns UseLocationRadiusFallback on.
            List<CreatureSpawner> inside = Dungeons.Spawners(dg);
            LurPlugin.Log.LogInfo("  spawners inside: " + inside.Count);

            foreach (CreatureSpawner spawner in inside)
            {
                ZNetView nview = spawner.GetComponent<ZNetView>();
                ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;

                LurPlugin.Log.LogInfo(string.Format(
                    "    {0} respawn={1} trigger={2} day={3} night={4} req='{5}' block='{6}' "
                    + "group='{7}' connection={8}",
                    Utils.GetPrefabName(spawner.gameObject), spawner.m_respawnTimeMinuts,
                    spawner.m_triggerDistance, spawner.m_spawnAtDay, spawner.m_spawnAtNight,
                    spawner.m_requiredGlobalKey, spawner.m_blockingGlobalKey,
                    spawner.m_spawnGroupID,
                    zdo != null ? zdo.GetConnectionType().ToString() : "<no zdo>"));
            }
        }

        // ------------------------------------------------------------------ the store

        /// <summary>
        /// Puts the horn on Hildir's shelf. See <see cref="Store"/> for why this is a postfix
        /// and why the row must be one cached instance.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems))]
        private static void AvailableItems(Trader __instance, List<Trader.TradeItem> __result)
        {
            try
            {
                Store.Offer(__instance, __result);
            }
            catch (System.Exception e)
            {
                LurPlugin.Log.LogError("Offering the horn threw, so it is not stocked: " + e);
            }
        }
    }
}
