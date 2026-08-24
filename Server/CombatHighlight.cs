namespace Server
{
	public static class CombatHighlight
	{
		public static bool Enabled = Config.Get("General.CombatHighlightEnabled", true);
		public static int Hue = Config.Get("General.CombatHighlightHue", 34);

		// True when beholder started a fight with beheld and the fight is still hot.
		public static bool Applies(Mobile beholder, Mobile beheld)
		{
			if (!Enabled || beholder == null || beheld == null || beholder == beheld)
			{
				return false;
			}

			var list = beholder.Aggressed;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Defender == beheld && !info.Expired && info.LastCombatTime >= beholder.LastPeaceTime)
				{
					return true;
				}
			}

			return false;
		}

		public static int Apply(Mobile beholder, Mobile beheld, int hue)
		{
			return Applies(beholder, beheld) ? Hue : hue;
		}
	}
}
