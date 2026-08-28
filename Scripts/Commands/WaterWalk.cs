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

                if (target.CanSwim)
                    from.SendMessage("{0} can now walk on water.", target.Name);
                else
                    from.SendMessage("{0} can no longer walk on water.", target.Name);
            }
        }
    }
}
