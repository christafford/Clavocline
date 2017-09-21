namespace Engulfer
{
	public class DayData
	{
		// inputs
		public double TickerCloseChangePastDay { get; set; }
		
		public double TickerCloseChangePast2Days { get; set; }
		
		public double TickerCloseChangePast4Days { get; set; }
		
		public double AverageRelationCloseChangePastDay { get; set; }
		
		public double AverageRelationCloseChangePast2Days { get; set; }
		
		public double AverageRelationCloseChangePast4Days { get; set; }
	
		public double TickerVolTodayVsLately { get; set; }
		
		public double TickerVolYesterdayVsLately { get; set; }
		
		public double AverageRelationVolTodayVsLately { get; set; }
		
		public double AverageRelationVolYesterdayVsLately { get; set; }
		
		// output
		public double TickerCloseChangeNext { get; set; }
	}
}