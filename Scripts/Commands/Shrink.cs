using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public class Shrink
    {
        public static void Initialize()
        {
            CommandSystem.Register("Shrink", AccessLevel.GameMaster, Shrink_OnCommand);
        }

        [Usage("Shrink")]
        [Description("Shrinks a targeted creature into a statuette that can be picked up and later restored.")]
        private static void Shrink_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("What creature do you wish to shrink?");
            e.Mobile.Target = new ShrinkTarget();
        }

        private class ShrinkTarget : Target
        {
            public ShrinkTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is PlayerMobile)
                {
                    from.SendMessage("You may not shrink a player.");
                    return;
                }

                if (!(targeted is BaseCreature))
                {
                    from.SendMessage("You may only shrink a creature.");
                    return;
                }

                var creature = (BaseCreature)targeted;

                // A stabled pet and a ridden mount are both already on the internal map.
                // Both checks must come before the internal-map check, or the caller gets
                // the wrong message.
                if (creature is BaseVendor)
                {
                    from.SendMessage("You may not shrink a vendor.");
                }
                else if (creature.IsStabled)
                {
                    from.SendMessage("You may not shrink a stabled creature.");
                }
                else if (creature is BaseMount && ((BaseMount)creature).Rider != null)
                {
                    from.SendMessage("You may not shrink a mounted creature.");
                }
                else if (creature.Map == Map.Internal)
                {
                    from.SendMessage("That creature is already shrunk.");
                }
                else if (creature.Summoned)
                {
                    from.SendMessage("You may not shrink a summoned creature.");
                }
                else if (creature.IsDeadPet || !creature.Alive)
                {
                    from.SendMessage("You may not shrink a dead creature.");
                }
                else if (creature.IsChampionSpawn)
                {
                    // The champion spawn counts a kill by Deleted. Internalize() never sets
                    // Deleted, so a shrunk minion or boss stops the spawn for good.
                    from.SendMessage("You may not shrink a champion spawn creature.");
                }
                else if (creature is BaseEscortable && ((BaseEscortable)creature).GetEscorter() != null)
                {
                    // An escortable that changes map loses its escorter and deletes itself.
                    from.SendMessage("You may not shrink a creature that is being escorted.");
                }
                else if (creature.Combatant != null || creature.Aggressors.Count > 0 || creature.Aggressed.Count > 0)
                {
                    from.SendMessage("That creature is busy fighting.");
                }
                else
                {
                    var item = new ShrunkenCreature(creature);

                    item.MoveToWorld(creature.Location, creature.Map);

                    // Clear the orders first, or the creature keeps the old target when it
                    // comes back. HitchingPost.EndStable does the same.
                    creature.ControlTarget = null;
                    creature.ControlOrder = OrderType.Stay;

                    creature.Internalize();

                    if (creature.Spawner != null)
                    {
                        creature.Spawner.Remove(creature);
                        creature.Spawner = null;
                    }

                    CommandLogging.WriteLine(
                        from,
                        "{0} {1} shrinking {2}",
                        from.AccessLevel,
                        CommandLogging.Format(from),
                        CommandLogging.Format(creature));

                    from.SendMessage("The creature has been shrunk.");
                }
            }
        }
    }
}
