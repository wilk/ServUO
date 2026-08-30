using System;

namespace Server.Mobiles
{
    [CorpseName("a drake corpse")]
    public class Drake : BaseCreature, IMount
    {
        private Mobile m_Rider;
        private Item m_MountItem;

        [Constructable]
        public Drake()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a drake";
            Body = Utility.RandomList(60, 61);
            BaseSoundID = 362;

            m_MountItem = new CreatureMountItem(this, MountableCreature.GetMountItemID(Body));

            SetStr(401, 430);
            SetDex(133, 152);
            SetInt(101, 140);

            SetHits(241, 258);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Fire, 20);

            SetResistance(ResistanceType.Physical, 45, 50);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 65.1, 90.0);
            SetSkill(SkillName.Wrestling, 65.1, 80.0);

            Fame = 5500;
            Karma = -5500;

            VirtualArmor = 46;

            Tamable = true;
            ControlSlots = 2;
            MinTameSkill = 84.3;

            PackReg(3);

            SetSpecialAbility(SpecialAbility.DragonBreath);
        }

        public Drake(Serial serial)
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
                if (m_MountItem != null)
                {
                    int itemID = MountableCreature.GetMountItemID(Body);

                    if (itemID != 0)
                        m_MountItem.ItemID = itemID;
                }

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

        public override bool ReacquireOnMovement
        {
            get
            {
                return true;
            }
        }
        public override int TreasureMapLevel
        {
            get
            {
                return 2;
            }
        }
        public override int Meat
        {
            get
            {
                return 10;
            }
        }
        public override int DragonBlood
        {
            get
            {
                return 8;
            }
        }
        public override int Hides
        {
            get
            {
                return 20;
            }
        }
        public override HideType HideType
        {
            get
            {
                return HideType.Horned;
            }
        }
        public override int Scales
        {
            get
            {
                return 2;
            }
        }
        public override ScaleType ScaleType
        {
            get
            {
                return (Body == 60 ? ScaleType.Yellow : ScaleType.Red);
            }
        }
        public override FoodType FavoriteFood
        {
            get
            {
                return FoodType.Meat | FoodType.Fish;
            }
        }
        public override bool CanFly
        {
            get
            {
                return true;
            }
        }
        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.MedScrolls, 2);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)3);

            writer.Write(m_Rider);
            writer.Write(m_MountItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_Rider = reader.ReadMobile();
                    m_MountItem = reader.ReadItem();
                    break;
                case 2:
                    break;
                case 1:
                    reader.ReadMobile(); // legacy rider, from the removed mount ability
                    reader.ReadItem(); // legacy mount item, from the removed mount ability
                    goto case 0;
                case 0:
                    break;
            }

            if (m_MountItem == null)
                m_MountItem = new CreatureMountItem(this, MountableCreature.GetMountItemID(Body));

            MountableCreature.SyncHue(this, m_MountItem);
        }
    }
}
