using Microsoft.Extensions.Logging;
using Nm.Logging;

namespace CarWash.Service.Interfaces
{
	public interface ICarWashLogger : INmLogger, ILogger
	{
	}

	public interface ICarWashLogger<T> : ICarWashLogger, ILogger<T>
	{
	}
}
