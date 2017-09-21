using System.IO;
using Encog.ML.Data.Basic;
using Encog.Persist;
using Encog.Util.Simple;

namespace Engulfer
{
	public class CreateTrainingDataSet
	{
		public static void Run()
		{
			var basicMLDataSet = new BasicMLDataSet();
			
			var maker = new DayDataMaker(true);
			maker.Init();
			var dataset = maker.GetDatas();
			
			dataset.ForEach(data =>
			{
				var basicData = new BasicMLData(10)
				{
					[0] = data.TickerCloseChangePastDay,
					[1] = data.TickerCloseChangePast2Days,
					[2] = data.TickerCloseChangePast4Days,
					[3] = data.AverageRelationCloseChangePastDay,
					[4] = data.AverageRelationCloseChangePast2Days,
					[5] = data.AverageRelationCloseChangePast4Days,
					[6] = data.TickerVolTodayVsLately,
					[7] = data.TickerVolYesterdayVsLately,
					[8] = data.AverageRelationVolTodayVsLately,
					[9] = data.AverageRelationVolYesterdayVsLately
				};

				basicMLDataSet.Add(basicData, new BasicMLData(1)
				{
					[0] = data.TickerCloseChangeNext
				});
			});

			EncogUtility.SaveEGB(Config.TrainingFile, basicMLDataSet);
		}
	}
}