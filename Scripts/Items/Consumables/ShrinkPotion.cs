using System;
using Server.Commands;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class ShrinkPotion : BasePotion
    {
        [Constructable]
        public ShrinkPotion()
            : base(0xF06, PotionEffect.Shrink)
        {
            Name = "Shrink Potion";
            Hue = 0x455;
        }

        public ShrinkPotion(Serial serial)
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

        public override void Drink(Mobile from)
        {
            from.SendMessage("What creature do you wish to shrink?");
            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private readonly ShrinkPotion m_Potion;

            public InternalTarget(ShrinkPotion potion)
                : base(12, false, TargetFlags.None)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Potion.Deleted || !m_Potion.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
                    return;
                }

                if (!(targeted is BaseCreature))
                {
                    from.SendMessage("You may only shrink a creature.");
                    return;
                }

                var creature = (BaseCreature)targeted;

                if (!creature.Controlled || creature.ControlMaster != from)
                {
                    from.SendMessage("You may only shrink your own creature.");
                    return;
                }

                if (creature.Summoned)
                {
                    from.SendMessage("You may not shrink a summoned creature.");
                    return;
                }

                if (creature.Warmode || creature.Combatant != null || creature.ControlOrder == OrderType.Attack)
                {
                    from.SendMessage("You may not shrink a creature that fights.");
                    return;
                }

                string reason;

                if (!Shrink.CanShrink(from, creature, out reason))
                {
                    from.SendMessage(reason);
                    return;
                }

                Point3D location;
                Map map;
                var item = Shrink.DoShrink(from, creature, out location, out map);

                from.AddToBackpack(item);

                BasePotion.PlayDrinkEffect(from);

                m_Potion.Consume();
            }
        }
    }
}
