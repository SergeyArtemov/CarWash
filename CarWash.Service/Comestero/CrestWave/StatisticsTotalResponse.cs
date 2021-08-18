namespace CarWash.Service.Comestero.CrestWave
{
	public class StatisticsTotalResponse : CrestWaveResponse
	{
		public string Station { get; set; }

		public decimal CashTotal { get; set; }

		public decimal CardTotal { get; set; }

		public string DateFrom { get; set; }

		public string DateTo { get; set; }

		public decimal ChangeTotal { get; set; }

		public decimal SalesTotal { get; set; }

		public string DeviceId { get; internal set; }
	}
}
