using System;
using System.IO;
using Encog;
using Encog.ML.Data;
using Encog.Neural.Networks;
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

			// train the neural network
			EncogUtility.TrainConsole(network, trainingSet, 60*4);

			Console.WriteLine(@"Final Error: " + network.CalculateError(trainingSet));
			Console.WriteLine(@"Training complete, saving network.");
			EncogDirectoryPersistence.SaveObject(Config.NetworkFile, network);
			Console.WriteLine(@"Network saved.");

			EncogFramework.Instance.Shutdown();
		}
	}
}