using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Server.Items;

namespace Server.Mobiles
{
    public class UniversalSellInfo : IShopSellInfo
    {
        private static GenericSellInfo m_Catalog;

        public UniversalSellInfo()
        {
        }

        private static GenericSellInfo Catalog
        {
            get
            {
                if (m_Catalog == null)
                    m_Catalog = BuildCatalog();

                return m_Catalog;
            }
        }

        private static GenericSellInfo BuildCatalog()
        {
            Dictionary<Type, int> prices = new Dictionary<Type, int>();

            Type[] types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && typeof(SBInfo).IsAssignableFrom(t) && t != typeof(SBJunkDealer))
                .ToArray();

            foreach (Type type in types)
            {
                SBInfo info;

                try
                {
                    info = (SBInfo)Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                GenericSellInfo sellInfo = info.SellInfo as GenericSellInfo;

                if (sellInfo == null)
                    continue;

                foreach (KeyValuePair<Type, int> entry in sellInfo.Entries)
                {
                    int price;

                    if (!prices.TryGetValue(entry.Key, out price) || entry.Value > price)
                        prices[entry.Key] = entry.Value;
                }
            }

            GenericSellInfo catalog = new GenericSellInfo();

            foreach (KeyValuePair<Type, int> entry in prices)
                catalog.Add(entry.Key, entry.Value);

            return catalog;
        }

        public Type[] Types
        {
            get
            {
                return new[] { typeof(Item) };
            }
        }

        public string GetNameFor(Item item)
        {
            if (item.Name != null)
                return item.Name;
            else
                return item.LabelNumber.ToString();
        }

        public int GetSellPriceFor(Item item)
        {
            return GetSellPriceFor(item, null);
        }

        public int GetSellPriceFor(Item item, BaseVendor vendor)
        {
            if (Catalog.IsInList(item.GetType()))
                return Catalog.GetSellPriceFor(item, vendor);

            return 1;
        }

        public int GetBuyPriceFor(Item item)
        {
            return GetBuyPriceFor(item, null);
        }

        public int GetBuyPriceFor(Item item, BaseVendor vendor)
        {
            if (Catalog.IsInList(item.GetType()))
                return Catalog.GetBuyPriceFor(item, vendor);

            return 1;
        }

        public bool IsSellable(Item item)
        {
            if (item.QuestItem)
                return false;

            if (item is Gold || item is BankCheck)
                return false;

            return true;
        }

        public bool IsResellable(Item item)
        {
            return false;
        }
    }
}
