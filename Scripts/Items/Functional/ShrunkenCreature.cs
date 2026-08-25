using System;

using Server.Commands;
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

        // Zero-parameter constructor for Dupe.Activator.CreateInstance only. No
        // [Constructable] attribute, so [add ShrunkenCreature] stays unavailable.
        // Dupe.OnAfterDuped fills in a real creature right after this call.
        public ShrunkenCreature()
            : base(ShrinkTable.DefaultItemID)
        {
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

            // Staff can restore a creature past the follower limit. A normal player cannot.
            bool staffBypass = from.AccessLevel >= AccessLevel.GameMaster;

            if (!staffBypass && from.Followers + m_Creature.ControlSlots > from.FollowersMax)
            {
                from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                return;
            }

            // Read the old master before the claim. A player who restores his own pet keeps
            // the bond. Only a change of master breaks it.
            Mobile oldMaster = m_Creature.ControlMaster;

            m_Creature.MoveToWorld(loc, map);

            if (!m_Creature.SetControlMaster(from))
            {
                if (staffBypass)
                {
                    // SetControlMaster refuses on the follower limit only. Staff pass that
                    // limit, so claim the creature by hand. Repeat the side effects of
                    // SetControlMaster that change how the pet behaves.
                    m_Creature.CurrentWayPoint = null;
                    m_Creature.Home = Point3D.Zero;

                    m_Creature.ControlMaster = from;
                    m_Creature.Controlled = true;
                    m_Creature.ControlTarget = null;
                    m_Creature.ControlOrder = OrderType.Come;
                    m_Creature.Guild = null;

                    m_Creature.UpdateMasteryInfo();

                    m_Creature.AdjustSpeeds();
                    m_Creature.CurrentSpeed = m_Creature.ActiveSpeed;

                    // The new master cannot hurt the pet for three seconds, as after a tame.
                    m_Creature.RecentSetControl = true;
                    Timer.DelayCall(TimeSpan.FromSeconds(3), () => m_Creature.RecentSetControl = false);

                    m_Creature.InvalidateProperties();
                }
                else
                {
                    // A safety net. Today SetControlMaster refuses on the follower limit only,
                    // and the check above already covers that. The branch stays, so a new
                    // refusal condition inside SetControlMaster cannot leave a loose creature.
                    m_Creature.ControlOrder = OrderType.Stay;
                    m_Creature.Internalize();
                    from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                    return;
                }
            }

            // The claim above never touches the tame skill or the loyalty. Without this, a
            // finder with no taming skill fails every order of a creature with a tame skill
            // above 29.1, and CheckControlChance keeps dropping the loyalty on every failure.
            // MinTameSkill stays untouched: its setter would overwrite CurrentTameSkill.
            m_Creature.CurrentTameSkill = 0.0;
            m_Creature.Loyalty = BaseCreature.MaxLoyalty;

            if (oldMaster != from)
            {
                m_Creature.IsBonded = false;
            }

            if (!m_Creature.Owners.Contains(from) && m_Creature.Owners.Count < BaseCreature.MaxOwners)
            {
                m_Creature.Owners.Add(from);
            }

            m_Creature.ControlOrder = OrderType.Come;

            CommandLogging.WriteLine(
                from,
                "{0} {1} restoring {2}",
                from.AccessLevel,
                CommandLogging.Format(from),
                CommandLogging.Format(m_Creature));

            Delete();
        }

        public override void OnAfterDuped(Item newItem)
        {
            var copy = newItem as ShrunkenCreature;

            if (copy == null)
            {
                return;
            }

            // Dupe.CopyProperties already copied our Creature reference onto the copy, so the
            // copy points at OUR live creature. Clear it first. Without this, copy.Delete()
            // below reaches OnAfterDelete and deletes the original creature.
            copy.Creature = null;

            if (m_Creature == null || m_Creature.Deleted)
            {
                copy.Delete();
                return;
            }

            var clone = CloneCreature(m_Creature);

            if (clone == null)
            {
                // The creature type has no constructor with zero parameters. Drop the copy.
                // Dupe.cs still reports "Done", because this method cannot answer the caller.
                Console.WriteLine(
                    "ShrunkenCreature: cannot dupe. {0} has no constructor with zero parameters.",
                    m_Creature.GetType().Name);

                copy.Delete();
                return;
            }

            copy.Creature = clone;
            copy.InvalidateProperties();
        }

        // Builds a new, unowned creature of the same type as source, with the same name,
        // hue, stats and skills. Fails cleanly when the creature type has no zero-parameter
        // constructor.
        private static BaseCreature CloneCreature(BaseCreature source)
        {
            BaseCreature clone;

            try
            {
                clone = Activator.CreateInstance(source.GetType()) as BaseCreature;
            }
            catch
            {
                return null;
            }

            if (clone == null)
            {
                return null;
            }

            // The Mobile constructor already ran World.AddMobile(clone). A throw below would
            // leave the clone in the world with nothing pointing at it, and every later save
            // would keep it. Delete the clone on any failure.
            try
            {
                clone.Name = source.Name;
                clone.Hue = source.Hue;
                clone.Body = source.Body;
                clone.BaseSoundID = source.BaseSoundID;

                clone.RawStr = source.RawStr;
                clone.RawDex = source.RawDex;
                clone.RawInt = source.RawInt;

                clone.HitsMaxSeed = source.HitsMaxSeed;
                clone.StamMaxSeed = source.StamMaxSeed;
                clone.ManaMaxSeed = source.ManaMaxSeed;

                for (var i = 0; i < source.Skills.Length; i++)
                {
                    // Set the cap first. The clone must keep the growth room of the source.
                    clone.Skills[i].Cap = source.Skills[i].Cap;
                    clone.Skills[i].Base = source.Skills[i].Base;
                }

                clone.DamageMin = source.DamageMin;
                clone.DamageMax = source.DamageMax;

                clone.PhysicalResistanceSeed = source.PhysicalResistanceSeed;
                clone.FireResistSeed = source.FireResistSeed;
                clone.ColdResistSeed = source.ColdResistSeed;
                clone.PoisonResistSeed = source.PoisonResistSeed;
                clone.EnergyResistSeed = source.EnergyResistSeed;

                clone.ControlSlots = source.ControlSlots;
                clone.Tamable = source.Tamable;
                clone.MinTameSkill = source.MinTameSkill;

                // A new Mobile has Map == null. OnAfterDelete only deletes a creature that sits
                // on Map.Internal, so without this call the clone would leak when its statuette
                // is deleted before ever being restored.
                clone.Internalize();

                clone.Hits = clone.HitsMax;
                clone.Stam = clone.StamMax;
                clone.Mana = clone.ManaMax;
            }
            catch
            {
                clone.Delete();
                return null;
            }

            return clone;
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
