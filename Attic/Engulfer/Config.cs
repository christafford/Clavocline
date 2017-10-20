using System.Configuration;
using System.IO;

namespace Engulfer
{
	public class Config
	{
		public static FileInfo TrainingFile => new FileInfo($"{ConfigurationManager.AppSettings["DataDirectory"]}training.clav");
		public static FileInfo NetworkFile => new FileInfo($"{ConfigurationManager.AppSettings["DataDirectory"]}network.clav");
	}
}