using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public class BlessingGem
    {
        public static void Initialize()
        {
            CommandSystem.Register("blessing-gem", AccessLevel.Administrator, BlessingGem_OnCommand);
        }

        [Usage("blessing-gem")]
        [Description("Gives a Gemma Iniziale to a targeted player.")]
        private static void BlessingGem_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the player to give a gem.");
            e.Mobile.Target = new BlessingGemTarget();
        }

        private class BlessingGemTarget : Target
        {
            public BlessingGemTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is PlayerMobile))
                {
                    from.SendMessage("That is not a player.");
                    return;
                }

                var player = (PlayerMobile)targeted;

                if (player.Backpack != null && player.Backpack.FindItemByType(typeof(StarterGem)) != null)
                {
                    from.SendMessage("{0} already has a gem.", player.Name);
                    return;
                }

                var gem = new StarterGem();
                gem.Owner = player;

                player.AddToBackpack(gem);

                from.SendMessage("Gem given to {0}.", player.Name);

                CommandLogging.WriteLine(
                    from,
                    "{0} {1} giving a starter gem to {2}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    CommandLogging.Format(player));
            }
        }
    }
}
