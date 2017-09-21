using System;
using System.IO;
using Encog;
using Encog.Engine.Network.Activation;
using Encog.ML.Data;
using Encog.Neural.Pattern;
using Encog.Neural.Prune;
using Encog.Persist;
using Encog.Util.Simple;

namespace Engulfer
{
	public class Pruner
	{
		public static void Run()
		{
			var training = EncogUtility.LoadEGB2Memory(Config.TrainingFile);
         

			var pattern = new FeedForwardPattern
			{
				InputNeurons = training.InputSize,
				OutputNeurons = training.IdealSize,
				ActivationFunction = new ActivationTANH()
			};

			var prune = new PruneIncremental(training, pattern, 100, 1, 10,
				new ConsoleStatusReportable());

			prune.AddHiddenLayer(5, 50);
			prune.AddHiddenLayer(0, 50);

			Console.WriteLine("Starting prune process");
			
			prune.Process();

			EncogDirectoryPersistence.SaveObject(Config.NetworkFile, prune.BestNetwork);
		}
	}
}