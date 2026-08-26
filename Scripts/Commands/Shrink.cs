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

        // The refusal ladder for a shrink attempt. targeted is checked against every rule
        // [shrink applies. On refusal, reason carries the message the caller should send.
        public static bool CanShrink(Mobile from, object targeted, out string reason)
        {
            reason = null;

            if (targeted is PlayerMobile)
            {
                reason = "You may not shrink a player.";
                return false;
            }

            if (!(targeted is BaseCreature))
            {
                reason = "You may only shrink a creature.";
                return false;
            }

            var creature = (BaseCreature)targeted;

            // A stabled pet and a ridden mount are both already on the internal map.
            // Both checks must come before the internal-map check, or the caller gets
            // the wrong message.
            if (creature is BaseVendor)
            {
                reason = "You may not shrink a vendor.";
            }
            else if (creature.IsStabled)
            {
                reason = "You may not shrink a stabled creature.";
            }
            else if (creature is IMount && ((IMount)creature).Rider != null)
            {
                reason = "You may not shrink a mounted creature.";
            }
            else if (creature.Map == Map.Internal)
            {
                reason = "That creature is already shrunk.";
            }
            else if (creature.Summoned)
            {
                reason = "You may not shrink a summoned creature.";
            }
            else if (creature.IsDeadPet || !creature.Alive)
            {
                reason = "You may not shrink a dead creature.";
            }
            else if (creature.IsChampionSpawn)
            {
                // The champion spawn counts a kill by Deleted. Internalize() never sets
                // Deleted, so a shrunk minion or boss stops the spawn for good.
                reason = "You may not shrink a champion spawn creature.";
            }
            else if (creature is BaseEscortable && ((BaseEscortable)creature).GetEscorter() != null)
            {
                // An escortable that changes map loses its escorter and deletes itself.
                reason = "You may not shrink a creature that is being escorted.";
            }

            return reason == null;
        }

        // The success body of a shrink. Builds the statuette and internalizes creature, but
        // leaves placement to the caller: the command drops it on the ground, the potion
        // puts it in the drinker's backpack.
        public static ShrunkenCreature DoShrink(Mobile from, BaseCreature creature)
        {
            var item = new ShrunkenCreature(creature);

            // Clear the orders first, or the creature keeps the old target when it
            // comes back. HitchingPost.EndStable does the same.
            creature.ControlTarget = null;
            creature.ControlOrder = OrderType.Stay;

            // [shrink accepts a creature that fights. Clear the fight on both sides
            // before the creature leaves the world, or a mobile on the live map keeps
            // a Combatant or an aggression entry that points at a creature it can no
            // longer reach. Copy the lists first: the removal below changes them.
            var aggressors = new AggressorInfo[creature.Aggressors.Count];
            creature.Aggressors.CopyTo(aggressors);

            var aggressed = new AggressorInfo[creature.Aggressed.Count];
            creature.Aggressed.CopyTo(aggressed);

            foreach (var info in aggressors)
            {
                ClearFight(creature, info.Attacker);
            }

            foreach (var info in aggressed)
            {
                ClearFight(creature, info.Defender);
            }

            creature.Combatant = null;
            creature.Warmode = false;

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

            return item;
        }

        // Drops the link between creature and other on both sides, in the pattern of
        // BaseCreature.SetControlMaster. other keeps fighting nothing once the creature
        // goes to the internal map, instead of a Combatant or an aggression entry that
        // points at a mobile it can no longer reach.
        private static void ClearFight(BaseCreature creature, Mobile other)
        {
            creature.RemoveAggressor(other);
            creature.RemoveAggressed(other);
            other.RemoveAggressor(creature);
            other.RemoveAggressed(creature);

            if (other.Combatant == creature)
            {
                other.Combatant = null;
            }
        }

        private class ShrinkTarget : Target
        {
            public ShrinkTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                string reason;

                if (!CanShrink(from, targeted, out reason))
                {
                    from.SendMessage(reason);
                    return;
                }

                var creature = (BaseCreature)targeted;

                var item = DoShrink(from, creature);

                item.MoveToWorld(creature.Location, creature.Map);

                from.SendMessage("The creature has been shrunk.");
            }
        }
    }
}
