using Newtonsoft.Json;

namespace CarWash.Service.Comestero.CrestWave
{
	public class WasherCell
	{
		public decimal Price { get; set; }

		[JsonProperty("cell_id")]
		public string CellId { get; set; }

		[JsonProperty("cell_name")]
		public string CellName { get; set; }
	}
}
