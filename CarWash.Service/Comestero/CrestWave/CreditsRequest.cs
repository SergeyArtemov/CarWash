using Newtonsoft.Json;

namespace CarWash.Service.Comestero.CrestWave
{
	public class CreditsRequest
	{
		[JsonProperty("serial")]
		public string Serial { get; set; }

		[JsonProperty("credit_amount")]
		public decimal CreditAmount { get; set; }

		[JsonProperty("bonus_amount")]
		public decimal BonusAmount { get; set; }

		public string PostId { get; set; }

		[JsonProperty("cell_id")]
		public string CellId { get; set; }
	}
}
