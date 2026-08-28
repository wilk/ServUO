using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public class WaterWalk
    {
        public static void Initialize()
        {
            CommandSystem.Register("WaterWalk", AccessLevel.GameMaster, WaterWalk_OnCommand);
        }

        [Usage("WaterWalk")]
        [Description("Toggles the ability of a targeted creature or player to walk on water.")]
        private static void WaterWalk_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("What do you wish to toggle water walking for?");
            e.Mobile.Target = new WaterWalkTarget();
        }

        private class WaterWalkTarget : Target
        {
            public WaterWalkTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is Mobile))
                {
                    from.SendMessage("You may only use this on a creature or a player.");
                    return;
                }

                Mobile target = (Mobile)targeted;

                target.CanSwim = !target.CanSwim;

                // The GM now owns this CanSwim state directly. Clear any mount grant
                // bookkeeping so neither the mount (still ridden) nor the pending
                // land-check (already dismounted) later strips or fails to strip it
                // based on stale tracking. Only players carry the pending-grant flag.
                PlayerMobile targetPlayer = target as PlayerMobile;

                if (targetPlayer != null)
                {
                    targetPlayer.MountSwimGrant = false;
                }

                BaseMount mount = target.Mount as BaseMount;

                if (mount != null)
                {
                    mount.GrantedSwim = false;
                }

                if (target.CanSwim)
                    from.SendMessage("{0} can now walk on water.", target.Name);
                else
                    from.SendMessage("{0} can no longer walk on water.", target.Name);
            }
        }
    }
}
