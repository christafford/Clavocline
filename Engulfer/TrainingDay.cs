using System;
using System.IO;
using Encog;
using Encog.ML.Data;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training.Propagation;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using Encog.Persist;
using Encog.Util.File;
using Encog.Util.Simple;

namespace Engulfer
{
	public class TrainingDay
	{
		public static void Run()
		{
			var network = (BasicNetwork) EncogDirectoryPersistence.LoadObject(Config.NetworkFile);
			var trainingSet = EncogUtility.LoadEGB2Memory(Config.TrainingFile);

			while (true)
			{
				Propagation train = new ResilientPropagation(
					network,
					trainingSet)
				{
					ThreadCount = 0,
					FixFlatSpot = false
				};
				
				EncogUtility.TrainConsole(train, network, trainingSet, TimeSpan.FromMinutes(10).TotalSeconds);

				Console.WriteLine("Finished. Saving network...");
				EncogDirectoryPersistence.SaveObject(Config.NetworkFile, network);

				Console.WriteLine(@"Network saved.");
			}
		}
	}
}