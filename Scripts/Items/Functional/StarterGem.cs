using System;

namespace Server.Items
{
    public class StarterGem : Item
    {
        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [Constructable]
        public StarterGem()
            : base(0xF26)
        {
            Name = "Gemma Iniziale";
            Hue = 1161;
            Weight = 1.0;
            Movable = false;
            LootType = LootType.Blessed;
        }

        public StarterGem(Serial serial)
            : base(serial)
        {
        }

        public override bool Nontransferable
        {
            get { return true; }
        }

        public override bool Decays
        {
            get { return false; }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted)
            {
                return;
            }

            if (from != m_Owner && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("Questa gemma non ti appartiene.");
                return;
            }

            StarterKit.Apply(from);

            from.SendMessage("Sei stato benedetto! Controlla la tua banca.");

            Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Owner = reader.ReadMobile();
        }
    }
}
