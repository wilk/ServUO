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
            this.CanSwim = true;
            this.CantWalk = false;
        }

        [Constructable]
        public Thalassar(string name)
            : base(name, 0x75, 0x3EA8, AIType.AI_Mage, FightMode.Evil, 10, 1, 0.2, 0.4)
        {
            this.Hue = 0x530;

            this.CanSwim = true;
            this.CantWalk = false;

            this.SetStr(700, 800);
            this.SetDex(150, 175);
            this.SetInt(300, 350);

            this.SetHits(3000);
            this.SetMana(500);

            this.SetDamage(25, 35);

            this.SetDamageType(ResistanceType.Physical, 60);
            this.SetDamageType(ResistanceType.Cold, 40);

            this.SetResistance(ResistanceType.Physical, 70, 80);
            this.SetResistance(ResistanceType.Fire, 70, 80);
            this.SetResistance(ResistanceType.Cold, 70, 80);
            this.SetResistance(ResistanceType.Poison, 70, 80);
            this.SetResistance(ResistanceType.Energy, 70, 80);

            this.SetSkill(SkillName.MagicResist, 120.0, 140.0);
            this.SetSkill(SkillName.Magery, 100.0, 110.0);
            this.SetSkill(SkillName.EvalInt, 100.0, 110.0);
            this.SetSkill(SkillName.Tactics, 110.0, 120.0);
            this.SetSkill(SkillName.Wrestling, 110.0, 120.0);
            this.SetSkill(SkillName.Meditation, 80.0, 90.0);

            this.Fame = 22500;
            this.Karma = 30000;

            this.Tamable = true;
            this.MinTameSkill = 115.1;
            this.ControlSlots = 3;
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
