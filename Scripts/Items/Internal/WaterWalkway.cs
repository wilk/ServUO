using System;

namespace Server.Items
{
    /// <summary>
    /// No-draw surface item used by WaterWalkManager. The classic client refuses to
    /// send a movement packet onto a tile with no walkable surface, even when the
    /// mover has Mobile.CanSwim. Parking one of these on a wet tile gives the client
    /// a surface to walk onto, so the swim-capable mobile can actually step there.
    ///
    /// ItemID 0x2198 ("nodraw") carries only TileData.Surface: no Impassable, no
    /// draw. It is the no-draw floor already used in this repo for exactly this
    /// purpose (see Scripts/Items/Internal/InvisibleTile.cs). ItemID 0x21A4, used
    /// elsewhere in this repo (MedusaPlatform, WaterVat) under a "blockers" comment,
    /// carries Impassable and no Surface: verified against tiledata.mul, it blocks
    /// movement rather than enabling it, so it cannot serve as a walkway.
    ///
    /// Transient: these are pooled and moved around at runtime by WaterWalkManager,
    /// never meant to persist. Deserialize deletes any that make it into a save, so
    /// a stale one from an old session can never linger after a restart.
    /// </summary>
    public class WaterWalkway : Item
    {
        [Constructable]
        public WaterWalkway()
            : base(0x2198)
        {
            Movable = false;
            Name = "water walkway";
        }

        public WaterWalkway(Serial serial)
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

            Delete();
        }
    }
}
