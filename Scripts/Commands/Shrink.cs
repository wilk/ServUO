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
                if (!(targeted is BaseCreature))
                {
                    from.SendMessage("You may only shrink a creature.");
                    return;
                }

                var creature = (BaseCreature)targeted;

                if (targeted is PlayerMobile)
                {
                    from.SendMessage("You may not shrink a player.");
                }
                else if (creature is BaseVendor)
                {
                    from.SendMessage("You may not shrink a vendor.");
                }
                else if (creature.Map == Map.Internal)
                {
                    from.SendMessage("That creature is already shrunk.");
                }
                else if (creature.IsStabled)
                {
                    from.SendMessage("You may not shrink a stabled creature.");
                }
                else if (creature.Summoned)
                {
                    from.SendMessage("You may not shrink a summoned creature.");
                }
                else if (creature.IsDeadPet || !creature.Alive)
                {
                    from.SendMessage("You may not shrink a dead creature.");
                }
                else if (creature is BaseMount && ((BaseMount)creature).Rider != null)
                {
                    from.SendMessage("You may not shrink a mounted creature.");
                }
                else
                {
                    var item = new ShrunkenCreature(creature);

                    item.MoveToWorld(creature.Location, creature.Map);

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
