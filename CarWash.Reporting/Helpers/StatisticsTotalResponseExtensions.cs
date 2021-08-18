using CarWash.Database;
using CarWash.Service.Comestero.CrestWave;

namespace CarWash.Reporting.Helpers
{
	public static class StatisticsTotalResponseExtensions
	{
		public static DeviceStatistics ToDeviceStatistics(this StatisticsTotalResponse statistics)
		{
			return new DeviceStatistics()
			{
				CardTotal = statistics.CardTotal,
				CashTotal = statistics.CashTotal,
				ChangeTotal = statistics.ChangeTotal,
				DateFrom = statistics.DateFrom,
				DateTo = statistics.DateTo,
				DeviceId = statistics.DeviceId,
				SalesTotal = statistics.SalesTotal,
				Station = statistics.Station
			};
		}
	}
}
