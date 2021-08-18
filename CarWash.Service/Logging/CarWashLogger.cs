using CarWash.Service.Interfaces;
using Nm.Dal.Interfaces;
using Nm.Logging;

namespace CarWash.Service.Logging
{
	public class CarWashLogger : NmLogger, ICarWashLogger
	{
		public CarWashLogger(ILogRepository logRepository) : base(logRepository)
		{
		}
	}

	public class CarWashLogger<T> : NmLogger<T>, ICarWashLogger<T>
	{
		public CarWashLogger(ILogRepository logRepository) : base(logRepository)
		{
		}
	}
}
