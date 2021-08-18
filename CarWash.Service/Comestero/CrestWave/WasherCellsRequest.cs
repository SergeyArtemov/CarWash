using Newtonsoft.Json;

namespace CarWash.Service.Comestero.CrestWave
{
	public class WasherCellsRequest
	{
		[JsonProperty("serial")]
		public string Serial { get; set; }
	}
}
