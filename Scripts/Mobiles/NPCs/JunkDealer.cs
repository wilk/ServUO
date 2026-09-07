using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class JunkDealer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        [Constructable]
        public JunkDealer()
            : base("the junk dealer")
        {
        }

        public JunkDealer(Serial serial)
            : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos
        {
            get
            {
                return m_SBInfos;
            }
        }
        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBJunkDealer());
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
