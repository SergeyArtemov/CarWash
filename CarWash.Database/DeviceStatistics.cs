using System;
using System.ComponentModel.DataAnnotations;

namespace CarWash.Database
{
	public class DeviceStatistics
	{
		public int Id { get; set; }

		public string Station { get; set; }

		public string DeviceId { get; set; }

		[DataType("datetime2")]
		public string DateFrom { get; set; }

		[DataType("datetime2")]
		public string DateTo { get; set; }

		public decimal ChangeTotal { get; set; }

		public decimal SalesTotal { get; set; }

		public decimal CashTotal { get; set; }

		public decimal CardTotal { get; set; }

		public DateTime CreationDate { get; set; } = DateTime.Now;

		public override string ToString() => DeviceId;
	}
}
