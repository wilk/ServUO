using System;

using Server.Mobiles;

namespace Server.Items
{
    public static class StarterKit
    {
        public static void Apply(Mobile m)
        {
            m.StatCap = 450;
            m.StrCap = 150;
            m.DexCap = 150;
            m.IntCap = 150;

            m.RawStr = 150;
            m.RawDex = 150;
            m.RawInt = 150;

            m.SkillsCap = 58000;

            for (var i = 0; i < m.Skills.Length; i++)
            {
                if (m.Skills[i].Cap < 100.0)
                {
                    m.Skills[i].Cap = 100.0;
                }

                m.Skills[i].Base = 100.0;
            }

            FillBank(m);
        }

        private static void FillBank(Mobile m)
        {
            var bank = m.BankBox;

            bank.DropItem(BuildWizardBag());
            bank.DropItem(BuildWarriorBag());
            bank.DropItem(BuildArcherBag());
            bank.DropItem(BuildTravellerBag());
            bank.DropItem(BuildCrafterBag());
            bank.DropItem(BuildTamerBag());

            for (var i = 0; i < 3; i++)
            {
                var horse = new Horse();
                horse.Internalize();

                bank.DropItem(new ShrunkenCreature(horse));
            }

            Banker.Deposit(bank, 100000);
        }

        private static Bag BuildWizardBag()
        {
            var bag = new Bag();
            bag.Name = "Borsa del Mago";

            bag.DropItem(new BagOfAllReagents(250));

            bag.DropItem(FullBook(new Spellbook()));
            bag.DropItem(FullBook(new NecromancerSpellbook()));
            bag.DropItem(FullBook(new BookOfChivalry()));
            bag.DropItem(FullBook(new BookOfBushido()));
            bag.DropItem(FullBook(new BookOfNinjitsu()));
            bag.DropItem(FullBook(new SpellweavingBook()));
            bag.DropItem(FullBook(new MysticBook()));
            bag.DropItem(FullBook(new BookOfMasteries()));

            var scrolls = new Bag();
            scrolls.Name = "Pergamene";

            AddScrolls(scrolls, Loot.RegularScrollTypes, 20);
            AddScrolls(scrolls, Loot.SENecromancyScrollTypes, 20);
            AddScrolls(scrolls, Loot.ArcanistScrollTypes, 20);
            AddScrolls(scrolls, Loot.MysticismScrollTypes, 20);

            bag.DropItem(scrolls);

            bag.DropItem(new LeatherChest());
            bag.DropItem(new LeatherLegs());
            bag.DropItem(new LeatherArms());
            bag.DropItem(new LeatherGorget());
            bag.DropItem(new LeatherGloves());
            bag.DropItem(new LeatherCap());

            return bag;
        }

        private static Bag BuildWarriorBag()
        {
            var bag = new Bag();
            bag.Name = "Borsa del Guerriero";

            bag.DropItem(new Bandage(1000));

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new GreaterHealPotion());
            }

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new GreaterCurePotion());
            }

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new TotalRefreshPotion());
            }

            bag.DropItem(new PlateChest());
            bag.DropItem(new PlateLegs());
            bag.DropItem(new PlateArms());
            bag.DropItem(new PlateGorget());
            bag.DropItem(new PlateGloves());
            bag.DropItem(new PlateHelm());
            bag.DropItem(new HeaterShield());

            bag.DropItem(new BattleAxe());
            bag.DropItem(new DoubleAxe());
            bag.DropItem(new ExecutionersAxe());

            bag.DropItem(new Longsword());
            bag.DropItem(new Broadsword());
            bag.DropItem(new Katana());
            bag.DropItem(new VikingSword());

            return bag;
        }

        private static Bag BuildArcherBag()
        {
            var bag = new Bag();
            bag.Name = "Borsa dell'Arciere";

            bag.DropItem(new Arrow(5000));
            bag.DropItem(new Bolt(5000));

            bag.DropItem(new Bow());
            bag.DropItem(new CompositeBow());
            bag.DropItem(new Crossbow());
            bag.DropItem(new HeavyCrossbow());
            bag.DropItem(new RepeatingCrossbow());

            bag.DropItem(new LeatherChest());
            bag.DropItem(new LeatherLegs());
            bag.DropItem(new LeatherArms());
            bag.DropItem(new LeatherGorget());
            bag.DropItem(new LeatherGloves());
            bag.DropItem(new LeatherCap());

            bag.DropItem(new Bandage(1000));

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new GreaterHealPotion());
            }

            return bag;
        }

        private static Bag BuildTravellerBag()
        {
            var bag = new Bag();
            bag.Name = "Borsa del Viaggiatore";

            bag.DropItem(new Runebook());

            for (var i = 0; i < 10; i++)
            {
                bag.DropItem(new RecallRune());
            }

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new GreaterHealPotion());
            }

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new GreaterCurePotion());
            }

            for (var i = 0; i < 25; i++)
            {
                bag.DropItem(new TotalRefreshPotion());
            }

            bag.DropItem(new Bandage(1000));
            bag.DropItem(new Spyglass());

            return bag;
        }

        private static Bag BuildCrafterBag()
        {
            var bag = new Bag();
            bag.Name = "Borsa dell'Artigiano";

            bag.DropItem(new SmithHammer(1000));
            bag.DropItem(new SewingKit(1000));
            bag.DropItem(new TinkerTools(1000));
            bag.DropItem(new FletcherTools(1000));
            bag.DropItem(new MortarPestle(1000));
            bag.DropItem(new ScribesPen(1000));
            bag.DropItem(new DovetailSaw(1000));
            bag.DropItem(new Scissors());

            bag.DropItem(new IronIngot(5000));
            bag.DropItem(new Log(5000));
            bag.DropItem(new Leather(5000));
            bag.DropItem(new BoltOfCloth(1000));
            bag.DropItem(new BlankScroll(1000));
            bag.DropItem(new Bottle(1000));
            bag.DropItem(new Feather(5000));

            return bag;
        }

        private static Bag BuildTamerBag()
        {
            var bag = new Bag();
            bag.Name = "Borsa del Domatore";

            bag.DropItem(new ShepherdsCrook());
            bag.DropItem(new Bandage(1000));

            return bag;
        }

        private static Spellbook FullBook(Spellbook book)
        {
            book.Content = book.BookCount == 64 ? ulong.MaxValue : (1ul << book.BookCount) - 1;

            return book;
        }

        private static void AddScrolls(Container cont, Type[] types, int amount)
        {
            for (var i = 0; i < types.Length; i++)
            {
                var item = Loot.Construct(types[i]);

                if (item == null)
                {
                    continue;
                }

                item.Amount = amount;

                cont.DropItem(item);
            }
        }
    }
}
