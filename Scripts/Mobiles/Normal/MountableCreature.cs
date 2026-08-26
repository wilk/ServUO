using System;

using Server.Items;
using Server.Network;
using Server.Multis;

namespace Server.Mobiles
{
    // Issue #43: some tamed creatures (dragon, greater dragon, drake, serpentine dragon,
    // polar bear, boura) become mountable without becoming a BaseMount. A BaseMount base
    // class change would break the save format, because BaseMount.Deserialize reads its
    // own version field before the creature reads its own, and old streams do not hold it.
    // This helper carries the shared IMount logic that BaseMount.cs already has, so each
    // creature keeps BaseCreature as its base class and only adds IMount.
    public static class MountableCreature
    {
        private static readonly int MountRange = Math.Max(0, Config.Get("General.MountRange", 3));

        public static void SetRider(BaseCreature creature, Mobile value, ref Mobile riderField, Item mountItem)
        {
            if (riderField != value)
            {
                if (value == null)
                {
                    Point3D loc = riderField.Location;
                    Map map = riderField.Map;

                    if (map == null || map == Map.Internal)
                    {
                        loc = riderField.LogoutLocation;
                        map = riderField.LogoutMap;
                    }

                    creature.Direction = riderField.Direction;
                    creature.Location = loc;
                    creature.Map = map;

                    NetState ns = riderField.NetState;

                    if (ns != null && riderField is PlayerMobile && ns.IsEnhancedClient && creature.Commandable)
                    {
                        ns.Send(new PetWindow((PlayerMobile)riderField, creature));
                    }

                    if (mountItem != null)
                        mountItem.Internalize();
                }
                else
                {
                    if (riderField != null)
                        BaseMount.Dismount(riderField);

                    BaseMount.Dismount(value);

                    if (mountItem != null)
                        value.AddItem(mountItem);

                    value.Direction = creature.Direction;

                    creature.Internalize();
                }

                riderField = value;
            }
        }

        public static void TryMount(BaseCreature creature, Mobile from)
        {
            if (creature.IsDeadPet)
                return;

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                if (Core.AOS) // You cannot ride a mount in your current form.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1062061, from.NetState);
                else
                    from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.

                return;
            }

            if (!BaseMount.CheckMountAllowed(from, (IMount)creature, true, false))
                return;

            if (from.Mount is BaseBoat)
            {
                return;
            }

            if (from.Race == Race.Gargoyle && from.IsPlayer())
            {
                from.SendLocalizedMessage(1112281);
                from.SendMessage("You may not ride this creature.");
                return;
            }

            if (!DesignContext.Check(from))
                return;

            if (from.HasTrade)
            {
                from.SendLocalizedMessage(1042317); // You may not ride at this time
                return;
            }

            // A mount at one tile keeps the old behaviour. The extra range needs line of sight.
            if (from.InRange(creature, 1) || (from.InRange(creature, MountRange) && from.InLOS(creature)))
            {
                bool canAccess = (from.AccessLevel >= AccessLevel.GameMaster) ||
                                 (creature.Controlled && creature.ControlMaster == from) ||
                                 (creature.Summoned && creature.SummonMaster == from);

                if (canAccess)
                {
                    if (creature.Poisoned)
                        creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1049692, from.NetState); // This mount is too ill to ride.
                    else
                        ((IMount)creature).Rider = from;
                }
                else if (!creature.Controlled && !creature.Summoned)
                {
                    // That mount does not look broken! You would have to tame it to ride it.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 501263, from.NetState);
                }
                else
                {
                    // This isn't your mount; it refuses to let you ride.
                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 501264, from.NetState);
                }
            }
            else
            {
                from.SendLocalizedMessage(500206); // That is too far away to ride.
            }
        }
    }

    public class CreatureMountItem : Item, IMountItem
    {
        private IMount m_Mount;

        public CreatureMountItem(IMount mount, int itemID)
            : base(itemID)
        {
            Layer = Layer.Mount;
            Movable = false;

            m_Mount = mount;
        }

        public CreatureMountItem(Serial serial)
            : base(serial)
        {
        }

        public override double DefaultWeight
        {
            get
            {
                return 0;
            }
        }

        public IMount Mount
        {
            get
            {
                return m_Mount;
            }
        }

        public override void OnAfterDelete()
        {
            Mobile mount = m_Mount as Mobile;

            if (mount != null)
                mount.Delete();

            m_Mount = null;

            base.OnAfterDelete();
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (m_Mount != null)
                m_Mount.Rider = null;

            return DeathMoveResult.RemainEquiped;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Mount as Mobile);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Mount = reader.ReadMobile() as IMount;

                        if (m_Mount == null)
                            Delete();

                        break;
                    }
            }
        }
    }
}
