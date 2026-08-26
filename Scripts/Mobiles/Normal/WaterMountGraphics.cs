namespace Server.Mobiles
{
    // The classic client pre-validates movement client-side and has a hardcoded
    // special case for the sea horse mount graphic (0x3EB3): with that graphic worn,
    // it allows a step onto a WET tile and refuses one onto land; with any other
    // mount graphic, the opposite holds. Mobile.CanSwim is necessary on the server
    // side but not sufficient -- the client never sends the movement packet unless
    // the rider's mount graphic already matches the tile being stepped onto.
    //
    // This class keeps a water-capable mount's visible graphic in sync with the
    // tile one step ahead of the rider, so the client's own check passes.
    public static class WaterMountGraphics
    {
        // Called for every direction request the rider makes -- turns included,
        // since a pure turn changes which tile the client will next validate,
        // without the rider actually moving there.
        //
        // This runs BEFORE base.Move(d), so m is still at its pre-move location
        // and m.Direction is still the direction the rider was facing before
        // this request. Mobile.Move (Server/Mobile.cs) treats the request as a
        // real move only when that facing already equals d; otherwise it is a
        // pure turn and the location does not change.
        //
        // The classic client validated the tile the rider is standing on now
        // using the graphic worn BEFORE this packet -- it is always one packet
        // behind. So the graphic must already be correct for the NEXT tile the
        // client will check, not the one this packet is about:
        //   - Real move: after this request the rider is at current + 1. The
        //     client's next validation is for current + 2, so test two steps
        //     ahead in direction d.
        //   - Turn: the rider stays put. The client's next validation is for
        //     current + 1, so test one step ahead in direction d.
        public static void UpdateForMove(Mobile m, Direction d)
        {
            BaseMount mount = m.Mount as BaseMount;

            if (mount == null || !mount.CanSwim)
                return;

            bool isRealMove = (m.Direction & Direction.Mask) == (d & Direction.Mask);

            int x = m.X;
            int y = m.Y;

            Server.Movement.Movement.Offset(d, ref x, ref y);

            if (isRealMove)
                Server.Movement.Movement.Offset(d, ref x, ref y);

            mount.SetWaterGraphic(IsWet(m.Map, x, y));
        }

        // Called at the moment a rider mounts, so one who is already standing on
        // water sees the water graphic immediately instead of after the next move.
        public static void UpdateForMount(Mobile rider, BaseMount mount)
        {
            if (mount == null || !mount.CanSwim)
                return;

            mount.SetWaterGraphic(IsWet(rider.Map, rider.X, rider.Y));
        }

        private static bool IsWet(Map map, int x, int y)
        {
            if (map == null || map == Map.Internal)
                return false;

            LandTile tile = map.Tiles.GetLandTile(x, y);

            return (TileData.LandTable[tile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0;
        }
    }
}
