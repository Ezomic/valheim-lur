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
                m_requiredGlobalKey = LurConfig.SoldAfterKey.Value ?? ""
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
