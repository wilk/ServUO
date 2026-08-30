using System;

namespace Server.Mobiles
{
    [CorpseName("a polar bear corpse")]
    [TypeAlias("Server.Mobiles.Polarbear")]
    public class PolarBear : BaseCreature, IMount
    {
        private Mobile m_Rider;
        private Item m_MountItem;

        [Constructable]
        public PolarBear()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            this.Name = "a polar bear";
            this.Body = 213;
            this.BaseSoundID = 0xA3;

            m_MountItem = new CreatureMountItem(this, MountableCreature.GetMountItemID(this.Body));

            this.SetStr(116, 140);
            this.SetDex(81, 105);
            this.SetInt(26, 50);

            this.SetHits(70, 84);
            this.SetMana(0);

            this.SetDamage(7, 12);

            this.SetDamageType(ResistanceType.Physical, 100);

            this.SetResistance(ResistanceType.Physical, 25, 35);
            this.SetResistance(ResistanceType.Cold, 60, 80);
            this.SetResistance(ResistanceType.Poison, 15, 25);
            this.SetResistance(ResistanceType.Energy, 10, 15);

            this.SetSkill(SkillName.MagicResist, 45.1, 60.0);
            this.SetSkill(SkillName.Tactics, 60.1, 90.0);
            this.SetSkill(SkillName.Wrestling, 45.1, 70.0);

            this.Fame = 1500;
            this.Karma = 0;

            this.VirtualArmor = 18;

            this.Tamable = true;
            this.ControlSlots = 1;
            this.MinTameSkill = 35.1;
        }

        public PolarBear(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Rider
        {
            get
            {
                return m_Rider;
            }
            set
            {
                MountableCreature.SetRider(this, value, ref m_Rider, m_MountItem);
            }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                base.Hue = value;
                MountableCreature.SyncHue(this, m_MountItem);
            }
        }

        public void OnRiderDamaged(Mobile from, ref int amount, bool willKill)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            MountableCreature.TryMount(this, from);
        }

        public override bool OnBeforeDeath()
        {
            Rider = null;

            return base.OnBeforeDeath();
        }

        public override void OnDelete()
        {
            Rider = null;

            base.OnDelete();
        }

        public override void OnAfterDelete()
        {
            if (m_MountItem != null)
                m_MountItem.Delete();

            m_MountItem = null;

            base.OnAfterDelete();
        }

        public override int Meat
        {
            get
            {
                return 2;
            }
        }
        public override int Hides
        {
            get
            {
                return 16;
            }
        }
        public override FoodType FavoriteFood
        {
            get
            {
                return FoodType.Fish | FoodType.FruitsAndVegies | FoodType.Meat;
            }
        }
        public override PackInstinct PackInstinct
        {
            get
            {
                return PackInstinct.Bear;
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.Write(m_Rider);
            writer.Write(m_MountItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version >= 1)
            {
                m_Rider = reader.ReadMobile();
                m_MountItem = reader.ReadItem();
            }

            if (m_MountItem == null)
                m_MountItem = new CreatureMountItem(this, MountableCreature.GetMountItemID(this.Body));

            MountableCreature.SyncHue(this, m_MountItem);
        }
    }
}