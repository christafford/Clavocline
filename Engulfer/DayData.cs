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
	
		public double TickerVolToday { get; set; }
		
		public double TickerVolYesterday { get; set; }
		
		public double TickerVolPast2Days { get; set; }
		
		public double AverageRelationVolToday { get; set; }
		
		public double AverageRelationVolYesterday { get; set; }
		
		public double AverageRelationVolPast2Days { get; set; }
	
		// output
		public double TickerCloseChangeNext { get; set; }
	}
}