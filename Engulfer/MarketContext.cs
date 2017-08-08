using System;
using System.Data.Entity;
using MySql.Data.Entity;

namespace Engulfer
{
	[DbConfigurationType(typeof(MySqlEFConfiguration))]
	public class MarketContext : DbContext
	{
		public MarketContext() : base("name=MarketContext")
		{
		}

		public DbSet<EodEntry> EodEntries { get; set; }
	}
}

