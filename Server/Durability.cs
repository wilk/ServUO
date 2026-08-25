using System;

namespace Server
{
	public static class Durability
	{
		public static readonly int WearFactor = Math.Max(1, Config.Get("General.DurabilityWearFactor", 20));

		public static bool CheckWear()
		{
			return WearFactor <= 1 || Utility.Random(WearFactor) == 0;
		}
	}
}
