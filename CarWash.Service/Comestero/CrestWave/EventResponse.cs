using Newtonsoft.Json;

namespace CarWash.Service.Comestero.CrestWave
{
	public class EventResponse : CrestWaveResponse
	{
		[JsonProperty("event_id")]
		public int EventId { get; set; }

		[JsonProperty("event_status")]
		public string EventStatus { get; set; }
	}
}
