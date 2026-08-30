using System;

namespace Server.Mobiles
{
    [CorpseName("a dragon corpse")]
    public class SerpentineDragon : BaseCreature, IMount
    {
        private Mobile m_Rider;
        private Item m_MountItem;

        [Constructable]
        public SerpentineDragon()
            : base(AIType.AI_Mage, FightMode.Evil, 10, 1, 0.2, 0.4)
        {
            Name = "a serpentine dragon";
            Body = 103;
            BaseSoundID = 362;

            m_MountItem = new CreatureMountItem(this, MountableCreature.GetMountItemID(Body));

            SetStr(111, 140);
            SetDex(201, 220);
            SetInt(1001, 1040);

            SetHits(480);

            SetDamage(5, 12);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Poison, 25);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.EvalInt, 100.1, 110.0);
            SetSkill(SkillName.Magery, 110.1, 120.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 50.1, 60.0);
            SetSkill(SkillName.Wrestling, 30.1, 100.0);
            SetSkill(SkillName.DetectHidden, 100.0);

            Fame = 15000;
            Karma = 15000;

            VirtualArmor = 36;

            if (Core.ML && Utility.RandomDouble() < .33)
                PackItem(Engines.Plants.Seed.RandomPeculiarSeed(3));

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 108.0;

            SetSpecialAbility(SpecialAbility.DragonBreath);
        }

        public SerpentineDragon(Serial serial)
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

        public override bool ReacquireOnMovement { get { return !Controlled; } }
        
        public override double BonusPetDamageScalar { get { return Controlled ? 1.0 : (Core.SE) ? 3.0 : 1.0; } }
        public override bool AutoDispel { get { return !Controlled; } }
        public override HideType HideType { get { return HideType.Barbed; } }
        public override int Hides { get { return 20; } }
        public override int Meat { get { return 19; } }
        public override int Scales { get { return 6; } }

        public override ScaleType ScaleType
        {
            get
            {
                return (Utility.RandomBool() ? ScaleType.Black : ScaleType.White);
            }
        }
        public override int TreasureMapLevel { get { return 4; } }
        public override bool CanAngerOnTame { get { return true; } }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Gems, 2);
        }

        public override int GetIdleSound()
        {
            return 0x2C4;
        }

        public override int GetAttackSound()
        {
            return 0x2C0;
        }

        public override int GetDeathSound()
        {
            return 0x2C1;
        }

        public override int GetAngerSound()
        {
            return 0x2C4;
        }

        public override int GetHurtSound()
        {
            return 0x2C3;
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (!Core.SE && 0.2 > Utility.RandomDouble() && attacker is BaseCreature)
            {
                BaseCreature c = (BaseCreature)attacker;

                if (c.Controlled && c.ControlMaster != null)
                {
                    c.ControlTarget = c.ControlMaster;
                    c.ControlOrder = OrderType.Attack;
                    c.Combatant = c.ControlMaster;
                }
            }
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
