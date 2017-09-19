using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Engulfer
{
	[Table("EodEntry")]
	public class EodEntry
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Ticker { get; set; }

		[Required]
		public string Per { get; set; }

		[Required]
		public DateTime Date { get; set; }

		[Required]
		public decimal Open { get; set; }

		[Required]
		public decimal High { get; set; }

		[Required]
		public decimal Low { get; set; }

		[Required]
		public decimal Close { get; set; }

		[Column("Vol")]
		[Required]
		public double Vol { get; set; }

		[Required]
		public double OI { get; set; }
	}
}

