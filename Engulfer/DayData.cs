namespace Engulfer
{
	public class DayData
	{
		// all values based on EOD
		
		public double TickerChangePastDay { get; set; }
		
		public double TickerChangePast5Days { get; set; }
		
		public double TickerChangePast10Days { get; set; }
		
		public double AverageRelationChangePastDay { get; set; }
		
		public double AverageRelationChangePast5Days { get; set; }
		
		public double AverageRelationChangePast10Days { get; set; }
	
		public double TickerChangeNext { get; set; }
	}
}