using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{
    public class ShrunkenCreature : Item
    {
        private BaseCreature m_Creature;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Creature { get { return m_Creature; } set { m_Creature = value; } }

        public ShrunkenCreature(BaseCreature creature)
            : base(ShrinkTable.Lookup(creature))
        {
            m_Creature = creature;

            Weight = 10.0;
            LootType = LootType.Regular;

            Hue = creature.Hue & 0x0FFF;
        }

        public ShrunkenCreature(Serial serial)
            : base(serial)
        {
        }

        // The item holds a live creature. Decay would delete the item on the next world
        // save, and OnAfterDelete would then delete the creature with it.
        public override bool Decays
        {
            get { return false; }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Creature != null && !m_Creature.Deleted)
            {
                list.Add(1041601, m_Creature.Name); // Pet Name: ~1_val~

                if (m_Creature.ControlMaster != null)
                {
                    list.Add(1041602, m_Creature.ControlMaster.Name); // Owner: ~1_val~
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted || m_Creature == null || m_Creature.Deleted)
            {
                return;
            }

            Point3D loc;
            Map map;

            var root = RootParent;

            if (root is Mobile)
            {
                loc = ((Mobile)root).Location;
                map = ((Mobile)root).Map;
            }
            else
            {
                loc = GetWorldLocation();
                map = Map;
            }

            if (map == null || map == Map.Internal)
            {
                return;
            }

            BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

            if (house != null && !house.IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("You may not release a creature inside a house you do not own.");
                return;
            }

            m_Creature.MoveToWorld(loc, map);

            Delete();
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Creature != null && !m_Creature.Deleted && m_Creature.Map == Map.Internal)
            {
                m_Creature.Delete();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Creature);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Creature = reader.ReadMobile<BaseCreature>();

            if (m_Creature == null || m_Creature.Deleted)
            {
                Delete();
            }
        }
    }
}
