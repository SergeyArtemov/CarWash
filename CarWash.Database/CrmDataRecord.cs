using System;
using System.Text.RegularExpressions;

namespace CarWash.Database
{
	public class CrmDataRecord
	{
		public int Id { get; set; }

		public string CrmCode { get; set; }

		public string PayTypeName { get; set; }

		public decimal SumAmount { get; set; }

		public decimal SumTotal { get; set; }

		public string EssStationId { get; set; }

		public DateTime DateStart { get; set; }

		public DateTime DateEnd { get; set; }

		public string StationNumber => Regex.Match(CrmCode, @"\d+").Value;
	}
}
