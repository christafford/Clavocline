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
				var basicData = new BasicMLData(12)
				{
					[0] = data.TickerCloseChangePastDay,
					[1] = data.TickerCloseChangePast2Days,
					[2] = data.TickerCloseChangePast4Days,
					[3] = data.AverageRelationCloseChangePastDay,
					[4] = data.AverageRelationCloseChangePast2Days,
					[5] = data.AverageRelationCloseChangePast4Days,
					[6] = data.TickerVolToday,
					[7] = data.TickerVolYesterday,
					[8] = data.TickerVolPast2Days,
					[9] = data.AverageRelationVolToday,
					[10] = data.AverageRelationVolYesterday,
					[11] = data.AverageRelationVolPast2Days
				};

				basicMLDataSet.Add(basicData, new BasicMLData(1)
				{
					[0] = data.TickerCloseChangeNext
				});
			});

			EncogUtility.SaveEGB(new FileInfo("/home/chriss/Projects/Clavocline/Data/training.clav"), basicMLDataSet);
			
			var network = EncogUtility.SimpleFeedForward(12, 12, 6, 1, true);			
			
			EncogDirectoryPersistence.SaveObject(new FileInfo("/home/chriss/Projects/Clavocline/Data/network.clav"), network);
		}
	}
}