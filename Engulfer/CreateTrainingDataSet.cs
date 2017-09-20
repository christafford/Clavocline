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

			EncogUtility.SaveEGB(new FileInfo("/home/chriss/Projects/Clavocline/Data/training.clav"), basicMLDataSet);
			
			var network = EncogUtility.SimpleFeedForward(6, 6, 6, 1, true);			
			
			EncogDirectoryPersistence.SaveObject(new FileInfo("/home/chriss/Projects/Clavocline/Data/network.clav"), network);
		}
	}
}