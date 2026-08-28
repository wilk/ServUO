using System;

namespace Server.Mobiles
{
    [CorpseName("a silver steed corpse")]
    public class Thalassar : BaseMount
    {
        [Constructable]
        public Thalassar()
            : this("Thalassar")
        {
            CanSwim = true;
        }

        [Constructable]
        public Thalassar(string name)
            : base(name, 0x75, 0x3EA8, AIType.AI_Mage, FightMode.Evil, 10, 1, 0.2, 0.4)
        {
            Hue = 0x530;

            CanSwim = true;
            CantWalk = false;

            SetStr(700, 800);
            SetDex(150, 175);
            SetInt(300, 350);

            SetHits(3000);
            SetMana(500);

            SetDamage(25, 35);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Cold, 40);

            SetResistance(ResistanceType.Physical, 70, 80);
            SetResistance(ResistanceType.Fire, 70, 80);
            SetResistance(ResistanceType.Cold, 70, 80);
            SetResistance(ResistanceType.Poison, 70, 80);
            SetResistance(ResistanceType.Energy, 70, 80);

            SetSkill(SkillName.MagicResist, 120.0, 140.0);
            SetSkill(SkillName.Magery, 100.0, 110.0);
            SetSkill(SkillName.EvalInt, 100.0, 110.0);
            SetSkill(SkillName.Tactics, 110.0, 120.0);
            SetSkill(SkillName.Wrestling, 110.0, 120.0);
            SetSkill(SkillName.Meditation, 80.0, 90.0);

            Fame = 22500;
            Karma = 30000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 115.1;
        }

        public Thalassar(Serial serial)
            : base(serial)
        {
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
