using Encog.ML.Data.Basic;

namespace Engulfer
{
	public class CreateTrainingDataSet
	{
		public static void Run()
		{
			var basicMLDataSet = new BasicMLDataSet();
			
			var maker = new DayDataMaker();
			maker.Init();
			var dataset = maker.GetDatas();
			
			dataset.ForEach(data =>
			{
				var basicData = new BasicMLData(6)
				{
					[0] = data.TickerChangePastDay,
					[1] = data.TickerChangePast5Days,
					[2] = data.TickerChangePast10Days,
					[3] = data.AverageRelationChangePastDay,
					[4] = data.AverageRelationChangePast5Days,
					[5] = data.AverageRelationChangePast10Days
				};

				basicMLDataSet.Add(basicData, new BasicMLData(1)
				{
					[0] = data.TickerChangeNext
				});
			});
			
			util
		}
	}
}