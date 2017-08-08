using System;
using System.Linq;

namespace Engulfer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			using (var context = new MarketContext())
			{
				
				context.EodEntries.RemoveRange (context.EodEntries.ToList());
				context.SaveChanges();
			}
		}
	}
}
