using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using Dapper;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;

namespace Engulfer
{
	[DbConfigurationType(typeof(MySqlEFConfiguration))]
	public class MarketContext : DbContext
	{
		public MySqlConnection GetConnection()
		{
			return new MySqlConnection("server=localhost;user id=root;password=iu45lkjsdf;database=MarketMadness");
		}

		public MarketContext() : base("name=MarketContext")
		{
		}

		public DbSet<EodEntry> EodEntries { get; set; }

		public void Insert(EodEntry entry, MySqlConnection connection, bool initialRun = false)
		{
			if (initialRun)
			{
				connection.ExecuteScalar(
					@"insert into EodEntry (Ticker, Per, Date, Open, High, Low, Close, Vol, OI)
values (@Ticker, @Per, @Date, @Open, @High, @Low, @Close, @Vol, @OI)",
					entry);
			}
			else
			{
				connection.ExecuteScalar(@"insert into EodEntry (Ticker, Per, Date, Open, High, Low, Close, Vol, OI)
select * from (select 	@Ticker as Ticker,
						@Per as Per,
						@Date as Date,
						@Open as Open,
						@High as High,
						@Low as Low,
						@Close as Close,
						@Vol as Vol,
						@OI as OI) as tmp
where not exists (	select null from EodEntry
					where ticker = @ticker and Date = @Date);", entry);
			}
		}

		public HashSet<string> GetMarketplaceTickers(MySqlConnection connection)
		{
			return new HashSet<string>(connection.Query<string>("SELECT DISTINCT Ticker FROM EodEntry").ToList());
		}

		public void AddRelationship(string parent, string child, MySqlConnection connection)
		{
			connection.ExecuteScalar(
				@"insert into TickerRelationships (ParentTicker, RelatedTicker, Date)
values (@ParentTicker, @RelatedTicker, @Date)",
				new {ParentTicker = parent, RelatedTicker = child, Date = DateTime.Now});
		}

		public List<TickerRelationships> GetRelations(string parent, MySqlConnection connection)
		{
			return connection.Query<TickerRelationships>(
				"select ParentTicker, RelatedTicker from TickerRelationships where ParentTicker = @ParentTicker",
				new {ParentTicker = parent}).ToList();
		}

		public List<EodEntry> GetAllEodEntries(MySqlConnection connection)
		{
			return connection.Query<EodEntry>("select * from EodEntry").ToList();
		}
	}
}
