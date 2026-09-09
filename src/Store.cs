using UnityEngine;

namespace Lur
{
    /// <summary>
    /// Hildir's stock row for the horn.
    ///
    /// She gave the quest, so she sells the rematch. Beyond the fiction this is the cheapest
    /// seam in the game for the job: Trader.TradeItem is {m_prefab, m_stack, m_price,
    /// m_requiredGlobalKey} and StoreGui.BuySelectedItem is entirely generic - it resolves the
    /// purchase by prefab name through ObjectDB, deducts the coins and shows the pickup
    /// message. One appended row gets the store entry, the price, the affordability check and
    /// the whole transaction from vanilla, and this mod writes none of it.
    ///
    /// <b>Why a postfix rather than adding to Trader.m_items on the prefab.</b> Mutating a
    /// vanilla prefab's list is shared state with the usual hazards, and it is an ordering bet
    /// besides: a Hildir already instantiated when the item finishes registering would never
    /// see the edit. A postfix is deterministic and correct whenever it runs.
    ///
    /// <b>Why the row is cached rather than built per call.</b> StoreGui.GetSelectedItemIndex
    /// matches the player's selection by reference - <c>availableItems[i] == m_selectedItem</c>
    /// - so a fresh allocation every call makes the selected row un-buyable while still
    /// rendering perfectly. That failure looks like a UI bug and would be hunted in the wrong
    /// place entirely.
    /// </summary>
    internal static class Store
    {
        /// <summary>Hildir's prefab name. Haldor carries his own Trader and must not be
        /// touched, so the row is keyed on the prefab rather than on the component.</summary>
        private const string Merchant = "Hildir";

        private static Trader.TradeItem _row;
        private static ItemDrop _drop;

        /// <summary>
        /// The one row instance, or null while the horn has not registered yet.
        ///
        /// Rebuilt only when the ItemDrop it points at is gone, which happens when ZNetScene
        /// is torn down and rebuilt between worlds. Asking the live scene rather than a flag
        /// is the same rule that governs registration, for the same reason.
        /// </summary>
        private static Trader.TradeItem Row()
        {
            if (_row != null && _drop != null) return _row;

            if (ZNetScene.instance == null) return null;

            GameObject prefab = ZNetScene.instance.GetPrefab(LurItem.Name);
            if (prefab == null) return null;

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop == null) return null;

            _drop = drop;
            _row = new Trader.TradeItem
            {
                m_prefab = drop,
                m_stack = Mathf.Max(1, LurConfig.Stack.Value),
                m_price = Mathf.Max(0, LurConfig.Price.Value),
                m_requiredGlobalKey = LurConfig.SoldAfterKey.Value ?? "",

                // Every string field, set explicitly, including the ones this mod has no use
                // for. Valheim 1.0 grew TradeItem from four fields to eleven, and StoreGui.FillList
                // reads one of the new ones without a guard:
                //
                //     if (tradeItem.m_tooltip.Length > 0)
                //
                // A TradeItem built from a C# object initialiser leaves unnamed strings null -
                // Unity's own rows come from a serialised asset, where an unset string is "" -
                // so that line threw a NullReferenceException on this row and took the rest of
                // the loop with it. The damage was not an error message: the price label is
                // written a few lines LOWER, so it kept the list-element prefab's placeholder and
                // Hildir advertised the horn at 12345 coins. The click listener is added lower
                // still, so the row could not be bought at all.
                //
                // It was invisible from the mod's side too. The exception lands in Player.log
                // rather than BepInEx's LogOutput, and this mod's own line said "Hildir will
                // stock Lur at 500 coins" - which was true of the object and false of the shop.
                //
                // So the rule is the general one rather than a patch for m_tooltip: when the game
                // hands out a plain class to fill in, fill in all of it. The next release that
                // adds a field will otherwise do this again.
                m_name = "",
                m_tooltip = "",
                m_buyKey = "",
                m_incrementKey = ""
            };

            LurPlugin.Log.LogInfo("Hildir will stock " + LurItem.Name + " at " + _row.m_price
                                  + " coins.");
            return _row;
        }

        /// <summary>
        /// Appends the row to what a trader offers, when that trader is Hildir.
        ///
        /// The key is checked here rather than left to vanilla because vanilla filtered
        /// m_items before this list was handed over; a row appended afterwards has not been
        /// through that gate. An unknown key is not an error - the row simply never appears,
        /// so a typo in the config reads as "she does not sell it", which is what the config
        /// comment warns about.
        /// </summary>
        internal static void Offer(Trader trader, System.Collections.Generic.List<Trader.TradeItem> items)
        {
            if (!LurConfig.Enabled.Value) return;
            if (trader == null || items == null) return;
            if (Utils.GetPrefabName(trader.gameObject) != Merchant) return;

            Trader.TradeItem row = Row();
            if (row == null) return;

            // Price and stack are host-authoritative and can change under a live session, so
            // they are refreshed on the cached instance rather than by making a new one.
            row.m_price = Mathf.Max(0, LurConfig.Price.Value);
            row.m_stack = Mathf.Max(1, LurConfig.Stack.Value);
            row.m_requiredGlobalKey = LurConfig.SoldAfterKey.Value ?? "";

            if (!string.IsNullOrEmpty(row.m_requiredGlobalKey)
                && !ZoneSystem.instance.GetGlobalKey(row.m_requiredGlobalKey))
            {
                return;
            }

            if (items.Contains(row)) return;

            items.Add(row);
        }
    }
}
