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
        [Description("Toggles the ability to walk on water for a targeted creature or player.")]
        private static void WaterWalk_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("What do you wish to toggle water walking on?");
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

                var mobile = (Mobile)targeted;

                mobile.CanSwim = !mobile.CanSwim;

                var mount = mobile.Mount as BaseMount;

                if (mount != null && mount.GrantedSwim)
                {
                    // The GM override replaces the mount's own swim grant, so the
                    // mount must not revert CanSwim again when the rider dismounts.
                    mount.GrantedSwim = false;
                }

                if (mobile.CanSwim)
                    from.SendMessage("{0} can now walk on water.", mobile.Name);
                else
                    from.SendMessage("{0} can no longer walk on water.", mobile.Name);
            }
        }
    }
}
