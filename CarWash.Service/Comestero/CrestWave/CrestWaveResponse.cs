using Nm.Base;

namespace CarWash.Service.Comestero.CrestWave
{
	public class CrestWaveResponse: Successable, IBaseResponse
	{
		public int Code { get; set; }
		public string Message { get; set; }

		public string ErrorCode { get; set; }
		public string ErrorDescription { get; set; }
		public string ErrorMessage { get; set; }

		public int? HttpStatusCode { get; set; }
	}
}
