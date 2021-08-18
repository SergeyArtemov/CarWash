using Newtonsoft.Json;
using System;

namespace CarWash.Service.Comestero.CrestWave
{
	public class KktStat
	{
		[JsonProperty("pos_income")]
		public decimal PosIncome { get; set; }

		[JsonProperty("cash_income")]
		public decimal CashIncome { get; set; }

		[JsonProperty("kkt_session_id")]
		public object KktSessionId { get; set; }

		[JsonProperty("edat")]
		public DateTime? Date { get; set; }

		[JsonProperty("change")]
		public decimal Change { get; set; }

		[JsonProperty("sales")]
		public decimal Sales { get; set; }
	}
}
