using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBJunkDealer : SBInfo
    {
        private readonly List<GenericBuyInfo> m_BuyInfo = new List<GenericBuyInfo>();
        private readonly IShopSellInfo m_SellInfo = new UniversalSellInfo();
        public SBJunkDealer()
        {
        }

        public override IShopSellInfo SellInfo
        {
            get
            {
                return m_SellInfo;
            }
        }
        public override List<GenericBuyInfo> BuyInfo
        {
            get
            {
                return m_BuyInfo;
            }
        }
    }
}
