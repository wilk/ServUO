using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// Keeps a ring of 8 WaterWalkway items around every Mobile.CanSwim player mobile
    /// that stands next to water, so the classic client sees a walkable surface on each
    /// wet neighbour tile and will actually send the move packet onto it.
    ///
    /// The ring only works around CLIENT-side movement pre-validation, so it is
    /// restricted to Mobile.Player mobiles -- a creature is moved by server-side AI
    /// and never needs one. The pool is keyed per mobile: a player on foot via
    /// [waterwalk and a mounted rider are both just a Mobile with CanSwim == true, so
    /// both are handled the same way. One WaterWalkway per one of the 8 neighbour
    /// offsets is created the first time it is needed and then reused for the
    /// mobile's whole session -- moved with MoveToWorld on every step, never deleted
    /// and recreated.
    /// </summary>
    public static class WaterWalkManager
    {
        private const int PoolSize = 8;

        private static readonly int[] OffsetX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] OffsetY = { -1, -1, -1, 0, 0, 1, 1, 1 };

        private static readonly Dictionary<Mobile, WaterWalkway[]> m_Pools = new Dictionary<Mobile, WaterWalkway[]>();

        public static void Configure()
        {
            EventSink.Movement += EventSink_Movement;
            EventSink.Logout += EventSink_Logout;
        }

        public static void Initialize()
        {
            // Safety net for the cases plain movement can't see: CanSwim revoked, the
            // mobile deleted, or teleported away from water while standing still.
            Timer.DelayCall(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), Sweep);
        }

        private static void EventSink_Movement(MovementEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m == null || e.Blocked || !m.Player)
                return;

            if (!m.CanSwim && !m_Pools.ContainsKey(m))
                return;

            // Mobile.Location is only updated after this event returns, so the ring
            // is rebuilt one tick later, once the mobile has actually arrived.
            Timer.DelayCall(TimeSpan.Zero, () => UpdateRing(m));
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            TearDown(e.Mobile);
        }

        /// <summary>
        /// Immediate entry point for the moment a mobile gains or loses CanSwim
        /// without moving -- mounting a water-walking mount while standing still,
        /// or [waterwalk toggling the ability directly. Waiting for the periodic
        /// Sweep would leave the client's first step blocked, since a blocked step
        /// never sends a Movement event to bootstrap the ring.
        /// </summary>
        public static void Update(Mobile m)
        {
            UpdateRing(m);
        }

        private static void Sweep()
        {
            // Connected players are the only mobiles that can ever need a pool
            // created, so scan them to catch one gaining CanSwim while standing
            // still (no Movement event fires in that case). Pools that already
            // exist are refreshed too, so one is torn down properly on logout or
            // loss of CanSwim even if the mobile is no longer connected.
            var mobiles = new HashSet<Mobile>(m_Pools.Keys);

            foreach (NetState ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                    mobiles.Add(ns.Mobile);
            }

            foreach (Mobile m in mobiles)
                UpdateRing(m);
        }

        private static void UpdateRing(Mobile m)
        {
            if (m == null || m.Deleted || m.Map == null || m.Map == Map.Internal || !m.Player || !m.CanSwim)
            {
                TearDown(m);
                return;
            }

            Map map = m.Map;
            int x = m.X, y = m.Y;

            bool anyWet = false;

            for (int i = 0; i < PoolSize; ++i)
            {
                if (IsWetLandTile(map, x + OffsetX[i], y + OffsetY[i]))
                {
                    anyWet = true;
                    break;
                }
            }

            if (!anyWet)
            {
                TearDown(m);
                return;
            }

            WaterWalkway[] pool;

            if (!m_Pools.TryGetValue(m, out pool))
            {
                pool = new WaterWalkway[PoolSize];
                m_Pools[m] = pool;
            }

            for (int i = 0; i < PoolSize; ++i)
            {
                int tx = x + OffsetX[i];
                int ty = y + OffsetY[i];

                WaterWalkway way = pool[i];

                if (way == null || way.Deleted)
                {
                    way = new WaterWalkway();
                    pool[i] = way;
                }

                if (IsWetLandTile(map, tx, ty) && !HasWalkableSurface(map, tx, ty, way))
                {
                    int tz = map.GetAverageZ(tx, ty);

                    if (way.Map != map || way.X != tx || way.Y != ty || way.Z != tz)
                        way.MoveToWorld(new Point3D(tx, ty, tz), map);
                }
                else if (way.Map != Map.Internal)
                {
                    way.MoveToWorld(Point3D.Zero, Map.Internal);
                }
            }
        }

        private static void TearDown(Mobile m)
        {
            if (m == null)
                return;

            WaterWalkway[] pool;

            if (!m_Pools.TryGetValue(m, out pool))
                return;

            m_Pools.Remove(m);

            foreach (WaterWalkway way in pool)
            {
                if (way != null && !way.Deleted)
                    way.Delete();
            }
        }

        private static bool IsWetLandTile(Map map, int x, int y)
        {
            if (map == null || map == Map.Internal)
                return false;

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Ignored)
                return false;

            return (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0;
        }

        private static bool HasWalkableSurface(Map map, int x, int y, Item ignore)
        {
            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                ItemData id = TileData.ItemTable[tiles[i].ID & TileData.MaxItemValue];

                if ((id.Flags & TileFlag.Surface) != 0 && (id.Flags & TileFlag.Impassable) == 0)
                    return true;
            }

            IPooledEnumerable<Item> eable = map.GetItemsInRange(new Point3D(x, y, 0), 0);
            bool found = false;

            foreach (Item item in eable)
            {
                if (item == ignore || item.X != x || item.Y != y)
                    continue;

                ItemData id = item.ItemData;

                if ((id.Flags & TileFlag.Surface) != 0 && (id.Flags & TileFlag.Impassable) == 0)
                {
                    found = true;
                    break;
                }
            }

            eable.Free();

            return found;
        }
    }
}
