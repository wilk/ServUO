using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.HuePickers;
using Server.Targeting;

namespace Server.Items
{
    public class CreatureDyeTub : Item
    {
        private int m_DyedHue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int DyedHue
        {
            get { return m_DyedHue; }
            set
            {
                m_DyedHue = value;
                Hue = value;
            }
        }

        [Constructable]
        public CreatureDyeTub()
            : base(0xFAB)
        {
            Name = "creature dye tub";
            Weight = 10.0;
            LootType = LootType.Blessed;
        }

        public CreatureDyeTub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write((int)m_DyedHue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_DyedHue = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!CheckAccess(from))
                return;

            if (!CheckRange(from))
                return;

            from.Target = new InternalTarget(this);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new SimpleContextMenuEntry(from, 1074255, m => // Please select a hue for your Reward:
                    {
                        if (!CheckAccess(m))
                            return;

                        m.SendHuePicker(new InternalPicker(this));
                    }));

                list.Add(new SimpleContextMenuEntry(from, 1157379, m => // Select a pet to brand.
                    {
                        if (!CheckAccess(m))
                            return;

                        if (!CheckRange(m))
                            return;

                        m.Target = new InternalTarget(this);
                    }));
            }
        }

        private bool CheckAccess(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("You are unable to use that!");
                return false;
            }

            return true;
        }

        private bool CheckRange(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return false;
            }

            return true;
        }

        private class InternalPicker : HuePicker
        {
            private readonly CreatureDyeTub m_Tub;

            public InternalPicker(CreatureDyeTub tub)
                : base(tub.ItemID)
            {
                m_Tub = tub;
            }

            public override void OnResponse(int hue)
            {
                m_Tub.DyedHue = hue;
            }
        }

        private class InternalTarget : Target
        {
            private readonly CreatureDyeTub m_Tub;

            public InternalTarget(CreatureDyeTub tub)
                : base(-1, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Tub.CheckAccess(from))
                    return;

                if (!m_Tub.CheckRange(from))
                    return;

                if (!(targeted is Mobile))
                {
                    from.SendMessage("That is not a creature.");
                    return;
                }

                Mobile mob = (Mobile)targeted;

                if (mob.Player)
                {
                    from.SendMessage("You can not dye a player.");
                    return;
                }

                mob.Hue = m_Tub.DyedHue;
                from.PlaySound(0x23E);
                from.SendMessage("You dye the creature.");
            }
        }
    }
}
