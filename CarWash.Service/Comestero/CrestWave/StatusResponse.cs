namespace CarWash.Service.Comestero.CrestWave
{
	public class StatusResponse : CrestWaveResponse
	{
		public int Id { get; set; }

		public string Serial { get; set; }

		public string Address { get; set; }

		public string Status { get; set; }
	}
}
