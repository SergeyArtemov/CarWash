namespace CarWash.Service.Comestero.CrestWave
{
	public class WasherCellsResponse : CrestWaveResponse
	{
		public string Serial { get; set; }

		public WasherCell[] Cells { get; set; }
	}
}
