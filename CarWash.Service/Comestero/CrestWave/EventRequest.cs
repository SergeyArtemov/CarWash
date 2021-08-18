using Newtonsoft.Json;
using Nm.Base;

namespace CarWash.Service.Comestero.CrestWave
{
	public class EventRequest : BaseRequest
	{
		public string Serial { get; set; }

		[JsonProperty("event_id")]
		public int? EventId { get; set; }
	}
}
