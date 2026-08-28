using System;

using Server.Items;
using Server.Network;
using System.Collections.Generic;
using Server.Multis;

namespace Server.Mobiles
{
    public enum BlockMountType
    {
        None = -1,
        Dazed,
        BolaRecovery,
        DismountRecovery,
        RidingSwipe,
        RidingSwipeEthereal,
        RidingSwipeFlying
    }

    public abstract class BaseMount : BaseCreature, IMount
    {
        private static Dictionary<Mobile, BlockEntry> m_Table = new Dictionary<Mobile, BlockEntry>();
        private static readonly int MountRange = Math.Max(0, Config.Get("General.MountRange", 3));
        private Mobile m_Rider;
        private bool m_GrantedSwim;

        public BaseMount(string name, int bodyID, int itemID, AIType aiType, FightMode fightMode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed)
            : base(aiType, fightMode, rangePerception, rangeFight, activeSpeed, passiveSpeed)
        {
            Name = name;
            Body = bodyID;

            InternalItem = new MountItem(this, itemID);
        }

        public BaseMount(Serial serial)
            : base(serial)
        {
        }

        public virtual TimeSpan MountAbilityDelay
        {
            get
            {
                return TimeSpan.Zero;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextMountAbility { get; set; }

        public virtual bool AllowMaleRider
        {
            get
            {
                return true;
            }
        }

        public virtual bool AllowFemaleRider
        {
            get
            {
                return true;
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

                if (InternalItem != null)
                    InternalItem.Hue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemID
        {
            get
            {
                if (InternalItem != null)
                    return InternalItem.ItemID;
                else
                    return 0;
            }
            set
            {
                if (InternalItem != null)
                    InternalItem.ItemID = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GrantedSwim
        {
            get
            {
                return m_GrantedSwim;
            }
            set
            {
                m_GrantedSwim = value;
            }
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
                if (m_Rider != value)
                {
                    if (value == null)
                    {
                        Point3D loc = m_Rider.Location;
                        Map map = m_Rider.Map;

                        if (map == null || map == Map.Internal)
                        {
                            loc = m_Rider.LogoutLocation;
                            map = m_Rider.LogoutMap;
                        }

                        Direction = m_Rider.Direction;
                        Location = loc;
                        Map = map;

                        if (m_GrantedSwim)
                        {
                            PlayerMobile pm = m_Rider as PlayerMobile;

                            if (pm != null)
                            {
                                // Do not strip CanSwim here: a rider knocked off, or whose
                                // mount dies, may be standing over deep water right now.
                                // Hand the grant to the rider as pending; it clears on the
                                // rider's first step onto a tile that is not water. See
                                // CheckSwimGrant, called from PlayerMobile.OnLocationChange.
                                pm.MountSwimGrant = true;
                            }
                            else
                            {
                                // Only players carry the pending-grant tracking; anything
                                // else keeps the old immediate-clear behaviour.
                                m_Rider.CanSwim = false;
                            }

                            m_GrantedSwim = false;
                        }

                        NetState ns = m_Rider.NetState;

                        if (ns != null && m_Rider is PlayerMobile && ns.IsEnhancedClient && Commandable)
                        {
                            ns.Send(new PetWindow((PlayerMobile)m_Rider, this));
                        }

                        if (InternalItem != null)
                            InternalItem.Internalize();
                    }
                    else
                    {
                        if (m_Rider != null)
                            Dismount(m_Rider);

                        Dismount(value);

                        if (InternalItem != null)
                            value.AddItem(InternalItem);

                        value.Direction = Direction;

                        Internalize();
                    }

                    m_Rider = value;

                    if (value != null && CanSwim && !value.CanSwim)
                    {
                        value.CanSwim = true;
                        m_GrantedSwim = true;
                    }
                }
            }
        }

        protected Item InternalItem { get; private set; }

        public static bool OnFlightPath(Mobile m)
        {
            if (!m.Flying)
                return false;

            StaticTile[] tiles = m.Map.Tiles.GetStaticTiles(m.X, m.Y, true);
            ItemData itemData;
            bool onpath = false;

            for (int i = 0; i < tiles.Length && !onpath; ++i)
            {
                itemData = TileData.ItemTable[tiles[i].ID & TileData.MaxItemValue];
                onpath = (itemData.Name == "hover over");
            }

            return onpath;
        }

        // Clears a pending mount-granted swim grant once the rider's current tile is
        // not water. Call this after a Mobile's Location has already been updated.
        // Only players carry this pending-grant flag.
        public static void CheckSwimGrant(PlayerMobile m)
        {
            if (m == null || !m.MountSwimGrant)
                return;

            if (!m.CanSwim || IsWaterTile(m.Map, m.X, m.Y, m.Z))
                return;

            m.CanSwim = false;
            m.MountSwimGrant = false;
        }

        private static bool IsWaterTile(Map map, int x, int y, int z)
        {
            if (map == null || map == Map.Internal)
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                return true;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                StaticTile tile = staticTiles[i];

                if (tile.Z == z && (TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                    return true;
            }

            return false;
        }

        public static void Dismount(Mobile dismounted)
        {
            Dismount(dismounted, dismounted, BlockMountType.None, TimeSpan.MinValue, false);
        }

        public static void Dismount(Mobile dismounter, Mobile dismounted, BlockMountType blockmounttype, TimeSpan delay)
        {
            Dismount(dismounter, dismounted, blockmounttype, TimeSpan.MinValue, true);
        }

        public static void Dismount(Mobile dismounter, Mobile dismounted, BlockMountType blockmounttype, TimeSpan delay, bool message)
        {
            if (!dismounted.Mounted && !Server.Spells.Ninjitsu.AnimalForm.UnderTransformation(dismounted) && !dismounted.Flying)
                return;

            if (dismounted is ChaosDragoonElite)
            {
                dismounter.SendLocalizedMessage(1042047); // You fail to knock the rider from its mount.
            }

            IMount mount = dismounted.Mount;

            if (mount != null)
            {
                mount.Rider = null;

                if (message)
                    dismounted.SendLocalizedMessage(1040023); // You have been knocked off of your mount!
            }
            else if (Core.ML && Spells.Ninjitsu.AnimalForm.UnderTransformation(dismounted))
            {
                Spells.Ninjitsu.AnimalForm.RemoveContext(dismounted, true);
            }
            else if (dismounted.Flying)
            {
                if (!OnFlightPath(dismounted))
                {
                    dismounted.Flying = false;
                    dismounted.Freeze(TimeSpan.FromSeconds(1));
                    dismounted.Animate(AnimationType.Land, 0);
                    BuffInfo.RemoveBuff(dismounted, BuffIcon.Fly);
                }
            }
            else
            {
                return;
            }

            if (delay != TimeSpan.MinValue)
            {
                SetMountPrevention(dismounted, mount, blockmounttype, delay);
            }
        }

        public static void SetMountPrevention(Mobile mob, BlockMountType type, TimeSpan duration)
        {
            SetMountPrevention(mob, null, type, duration);   
        }

        public static void SetMountPrevention(Mobile mob, IMount mount, BlockMountType type, TimeSpan duration)
        {
            if (mob == null)
                return;

            DateTime expiration = DateTime.UtcNow + duration;
            BlockEntry entry = null;

            if (m_Table.ContainsKey(mob))
                entry = m_Table[mob];

            if (entry != null)
            {
                entry.m_Type = type;
                entry.m_Expiration = expiration;
            }
            else
            {
                m_Table[mob] = entry = new BlockEntry(mob, mount, type, expiration);
            }

            BuffInfo.AddBuff(mob, new BuffInfo(BuffIcon.DismountPrevention, 1075635, 1075636, duration, mob));
        }

        public static void ClearMountPrevention(Mobile mob)
        {
            if (mob != null && m_Table.ContainsKey(mob))
                m_Table.Remove(mob);
        }

        public static BlockMountType GetMountPrevention(Mobile mob)
        {
            return GetMountPrevention(mob, null);
        }

        public static BlockMountType GetMountPrevention(Mobile mob, IMount mount)
        {
            if (mob == null)
                return BlockMountType.None;

            BlockEntry entry = null;

            if (m_Table.ContainsKey(mob))
                entry = m_Table[mob];

            if (entry == null)
                return BlockMountType.None;

            if (entry.IsExpired(mount))
            {
                return BlockMountType.None;
            }

            if (Core.TOL && entry.m_Type >= BlockMountType.RidingSwipe && entry.m_Expiration > DateTime.UtcNow)
            {
                return BlockMountType.DismountRecovery;
            }

            return entry.m_Type;
        }

        public static bool CheckMountAllowed(Mobile mob, bool message)
        {
            return CheckMountAllowed(mob, message, false);
        }

        public static bool CheckMountAllowed(Mobile mob, bool message, bool flying)
        {
            return CheckMountAllowed(mob, null, message, flying);
        }

        public static bool CheckMountAllowed(Mobile mob, IMount mount, bool message, bool flying)
        {
            BlockMountType type = GetMountPrevention(mob, mount);

            if (type == BlockMountType.None)
                return true;

            if (message && mob.NetState != null)
            {
                switch (type)
                {
                    case BlockMountType.RidingSwipeEthereal:
                    case BlockMountType.Dazed:
                        {
                            mob.PrivateOverheadMessage(MessageType.Regular, 0x3B2, flying ? 1112457 : 1040024, mob.NetState);
                            // You are still too dazed from being knocked off your mount to ride!
                            break;
                        }
                    case BlockMountType.BolaRecovery:
                        {
                            mob.PrivateOverheadMessage(MessageType.Regular, 0x3B2, flying ? 1112455 : 1062910, mob.NetState);
                            // You cannot mount while recovering from a bola throw.
                            break;
                        }
                    case BlockMountType.RidingSwipe:
                        {
                            mob.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1062934, mob.NetState);
                            // You must heal your mount before riding it.
                            break;
                        }
                    case BlockMountType.RidingSwipeFlying:
                        {
                            mob.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1112454, mob.NetState);
                            // You must heal your mount before riding it.
                            break;
                        }
                    case BlockMountType.DismountRecovery:
                        {
                            mob.PrivateOverheadMessage(MessageType.Regular, 0x3B2, flying ? 1112456 : 1070859, mob.NetState);
                            // You cannot mount while recovering from a dismount special maneuver.
                            break;
                        }
                }
            }

            return false;
        }

        public static void ExpireMountPrevention(Mobile m)
        {
            if(m_Table.ContainsKey(m))
                m_Table.Remove(m);

            BuffInfo.RemoveBuff(m, BuffIcon.DismountPrevention);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write(m_GrantedSwim);

            writer.Write(NextMountAbility);

            writer.Write(m_Rider);
            writer.Write(InternalItem);
        }

        public override bool OnBeforeDeath()
        {
            Rider = null;

            return base.OnBeforeDeath();
        }

        public override void OnAfterDelete()
        {
            if (InternalItem != null)
                InternalItem.Delete();

            InternalItem = null;

            base.OnAfterDelete();
        }

        public override void OnDelete()
        {
            Rider = null;

            base.OnDelete();
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            var owner = GetMaster();

            if (owner != null && m_Table.ContainsKey(owner))
            {
                var entry = m_Table[owner];

                if (entry.m_Type >= BlockMountType.RidingSwipe && entry.m_Mount == this)
                {
                    ExpireMountPrevention(owner);
                }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 2:
                    {
                        m_GrantedSwim = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        NextMountAbility = reader.ReadDateTime();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Rider = reader.ReadMobile();
                        InternalItem = reader.ReadItem();

                        if (InternalItem == null)
                            Delete();

                        break;
                    }
            }
        }

        public virtual void OnDisallowedRider(Mobile m)
        {
            m.SendMessage("You may not ride this creature.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsDeadPet)
                return;

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                if (Core.AOS) // You cannot ride a mount in your current form.
                    PrivateOverheadMessage(Network.MessageType.Regular, 0x3B2, 1062061, from.NetState);
                else
                    from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.

                return;
            }

            if (!CheckMountAllowed(from, this, true, false))
                return;

            if (from.Mount is BaseBoat)
            {
                return;
            }

            if (from.Race == Race.Gargoyle && from.IsPlayer())
            {
                from.SendLocalizedMessage(1112281);
                OnDisallowedRider(from);
                return;
            }

            if (from.Female ? !AllowFemaleRider : !AllowMaleRider)
            {
                OnDisallowedRider(from);
                return;
            }

            if (!Multis.DesignContext.Check(from))
                return;

            if (from.HasTrade)
            {
                from.SendLocalizedMessage(1042317); // You may not ride at this time
                return;
            }

            // A mount at one tile keeps the old behaviour. The extra range needs line of sight.
            if (from.InRange(this, 1) || (from.InRange(this, MountRange) && from.InLOS(this)))
            {
                bool canAccess = (from.AccessLevel >= AccessLevel.GameMaster) ||
                                 (Controlled && ControlMaster == from) ||
                                 (Summoned && SummonMaster == from);

                if (canAccess)
                {
                    if (Poisoned)
                        PrivateOverheadMessage(Network.MessageType.Regular, 0x3B2, 1049692, from.NetState); // This mount is too ill to ride.
                    else
                        Rider = from;
                }
                else if (!Controlled && !Summoned)
                {
                    // That mount does not look broken! You would have to tame it to ride it.
                    PrivateOverheadMessage(Network.MessageType.Regular, 0x3B2, 501263, from.NetState);
                }
                else
                {
                    // This isn't your mount; it refuses to let you ride.
                    PrivateOverheadMessage(Network.MessageType.Regular, 0x3B2, 501264, from.NetState);
                }
            }
            else
            {
                from.SendLocalizedMessage(500206); // That is too far away to ride.
            }
        }

        public virtual void OnRiderDamaged(Mobile from, ref int amount, bool willKill)
        {
            if (m_Rider == null)
                return;

            Mobile attacker = from;

            if (attacker == null)
                attacker = m_Rider.FindMostRecentDamager(true);

            if (!(attacker == this || attacker == m_Rider || willKill || DateTime.UtcNow > NextMountAbility))
            {
                if (DoMountAbility(amount, from))
                    NextMountAbility = DateTime.UtcNow + MountAbilityDelay;
            }
        }

        [Obsolete("Call: OnRiderDamaged(Mobile from, ref int amount, bool willKill)")]
        public virtual void OnRiderDamaged(int amount, Mobile from, bool willKill)
        {
            OnRiderDamaged(from, ref amount, willKill);
        }

        public virtual bool DoMountAbility(int damage, Mobile attacker)
        {
            return false;
        }

        private class BlockEntry
        {
            public Mobile m_Mobile;
            public IMount m_Mount;
            public BlockMountType m_Type;
            public DateTime m_Expiration;

            public BlockEntry(Mobile m, IMount mount, BlockMountType type, DateTime expiration)
            {
                m_Mobile = m;
                m_Mount = mount;
                m_Type = type;
                m_Expiration = expiration;
            }

            public bool IsExpired(IMount mount)
            {
                if (m_Type >= BlockMountType.RidingSwipe)
                {
                    if (Core.SA && DateTime.UtcNow < m_Expiration)
                    {
                        return false;
                    }
                    else
                    {
                        if (mount != m_Mount)
                        {
                            return true;
                        }

                        switch (m_Type)
                        {
                            default:
                            case BlockMountType.RidingSwipe:
                                {
                                    if ((!Core.SA && m_Mount == null) || m_Mount is Mobile && ((Mobile)m_Mount).Hits >= ((Mobile)m_Mount).HitsMax)
                                    {
                                        BaseMount.ExpireMountPrevention(m_Mobile);
                                        return true;
                                    }
                                }
                                break;
                            case BlockMountType.RidingSwipeEthereal:
                                {
                                    BaseMount.ExpireMountPrevention(m_Mobile);
                                    return true;
                                }
                            case BlockMountType.RidingSwipeFlying:
                                {
                                    if (m_Mobile.Hits >= m_Mobile.HitsMax)
                                    {
                                        BaseMount.ExpireMountPrevention(m_Mobile);
                                        return true;
                                    }
                                }
                                break;
                        }
                    }

                    return false;
                }

                if (DateTime.UtcNow >= m_Expiration)
                {
                    BaseMount.ExpireMountPrevention(m_Mobile);
                    return true;
                }

                return false;
            }
        }
    }

    public class MountItem : Item, IMountItem
    {
        private BaseMount m_Mount;
        public MountItem(BaseMount mount, int itemID)
            : base(itemID)
        {
            Layer = Layer.Mount;
            Movable = false;

            m_Mount = mount;
        }

        public MountItem(Serial serial)
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
            if (m_Mount != null)
                m_Mount.Delete();

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

            writer.Write(m_Mount);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 0:
                    {
                        m_Mount = reader.ReadMobile() as BaseMount;

                        if (m_Mount == null)
                            Delete();

                        break;
                    }
            }
        }
    }
}
