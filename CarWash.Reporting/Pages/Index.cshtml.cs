using CarWash.Database;
using CarWash.Reporting.Helpers;
using CarWash.Reporting.Model;
using CarWash.Service.Comestero;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nm.Constants;
using Nm.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CarWash.Reporting.Pages
{
	[IgnoreAntiforgeryToken()]
	public class IndexModel : PageModel
	{
		private const string DevicesCacheKey = "Devices";
		private const int ComesteroRetryCount = 1;

		private readonly INmLogger _logger;
		private readonly ComesteroWashClient _comesteroWashClient;
		private readonly IMemoryCache _memoryCache;
		private readonly IServiceProvider _serviceProvider;
		private readonly string _crmConnectionString;

		private static readonly object locker = new object();

		private SemaphoreSlim inProgressSemaphore = new SemaphoreSlim(1);

		public bool InProgress => inProgressSemaphore.CurrentCount > 0;

		public IndexModel(INmLogger logger, IConfiguration configuration, IMemoryCache memoryCache, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_memoryCache = memoryCache;
			_serviceProvider = serviceProvider;

			var xauth = configuration.GetValue<string>("Comestero:XAuth");
			_comesteroWashClient = new ComesteroWashClient(xauth);
			_crmConnectionString = configuration.GetConnectionString("CRM");
		}

		public async Task OnGetAsync()
		{
			_memoryCache.TryGetValue(DevicesCacheKey, out Device[] devices);

			if (devices == null)
				await CheckNewMachines();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (inProgressSemaphore.CurrentCount == 0)
				return Page();

			inProgressSemaphore.Wait();

			try
			{
				var dateStart = DateTime.Parse(Request.Form["date-start"]);
				var dateEnd = DateTime.Parse(Request.Form["date-end"]);

				var intervals = new List<SearchPeriod>();
				var t = dateStart;

				while (t < dateEnd.AddMonths(1))  // asa было: <= dateEnd
				{
					intervals.Add(new SearchPeriod()
					{
						DateStart = t.ToString("yyyy-MM-ddT00:00"),
						//DateEnd = t.AddMonths(1).ToString("yyyy-MM-ddT00:00")
						DateEnd = t.AddDays(1).ToString("yyyy-MM-ddT00:00") // asa 13.08.2021
					});

					t = t.AddDays(1);  // asa было: t.AddMonths(1);
				}

				var devices = _memoryCache.Get<Device[]>(DevicesCacheKey);

				List<DeviceStatistics> deviceStatistics = new List<DeviceStatistics>();
				List<CrmDataRecord> crmDataRecords = new List<CrmDataRecord>();
				DeviceStatistics[] allDeviceStats = null;
				CrmDataRecord[] allCrmData = null;
				GasStation[] stations = null;

				var records = new List<PivotDataRecord>();

				using (var context = (CarWashContext)_serviceProvider.GetService(typeof(CarWashContext)))
				{
					allDeviceStats = context.DeviceStatistics.OrderBy(d => d.DeviceId).ToArray();
					allCrmData = context.CrmData.OrderBy(d => d.CrmCode).ToArray();
					stations = context.GasStations.OrderBy(s => s.CrmCode).ToArray();
				}

				Parallel.ForEach(devices, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, (device) =>
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						var logger = scope.ServiceProvider.GetService<INmLogger>();
						GetDeviceDataComestero(logger, allDeviceStats, device, intervals, deviceStatistics);
					}
				});

				await GetDataCrmAsync(intervals, allCrmData, crmDataRecords);

				foreach (var station in stations.Where(s => s.Number.EndsWith("М") || s.Number.All(c => char.IsDigit(c))))
				{
					try
					{
						var record = new PivotDataRecord()
						{
							StationNumber = station.Number
						};

						foreach (var interval in intervals)
						{
							var dtBegin = DateTime.ParseExact(interval.DateStart, "yyyy-MM-ddT00:00", CultureInfo.InvariantCulture);
							var dtEnd = DateTime.ParseExact(interval.DateEnd, "yyyy-MM-ddT00:00", CultureInfo.InvariantCulture);

							var ids = devices.Where(d => d.StationNumber.Equals(station.Number)).Select(d => d.DeviceId.ToString());
							var stats = deviceStatistics.Where(s => s.DateFrom.Equals(interval.DateStart) && s.DateTo.Equals(interval.DateEnd)).Where(s => ids.Contains(s.DeviceId));
							var crm = crmDataRecords.Where(c => c.DateStart >= dtBegin && c.DateEnd <= dtEnd).Where(c => c.CrmCode.Equals(station.CrmCode));

							var entry = new PivotDataEntry()
							{
								MonthNumber = dtBegin.Month,
								Interval = dtBegin.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU")),
								BankTerminal = stats.Sum(s => s.CardTotal),
								Cash = stats.Sum(s => s.CashTotal),
								Change = stats.Sum(s => s.ChangeTotal),
								BankMobile = crm.Where(c => c.PayTypeName.Equals("БАНК")).Sum(c => c.SumAmount),
								BonusPoints = crm.Where(c => c.PayTypeName.Equals("Баллы")).Sum(c => c.SumAmount)
							};

							record.RecordData.Add(entry);
						}

						records.Add(record);
					}
					catch (Exception ex)
					{
						await WriteErrorAsync(nameof(IndexModel), ex.ToString(), nameof(OnPostAsync));
					}
				}

				var result = new PivotTable()
				{
					Name = "table1",
					Records = records.OrderBy(r => r.StationNumber).Where(r => r.HasData)
				};

				//var stringResponse = JsonConvert.SerializeObject(result);
				//await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(stringResponse));

				_memoryCache.Set("Results", result);
				return Redirect("/Preview");
			}
			catch (Exception ex)
			{
				await WriteErrorAsync(nameof(IndexModel), ex.ToString(), nameof(OnPostAsync));
				return Page(); // todo
			}
			finally
			{
				inProgressSemaphore.Release();
			}
		}

		private async Task GetDataCrmAsync(List<SearchPeriod> intervals, CrmDataRecord[] allCrmData, List<CrmDataRecord> crmDataRecords)
		{
			using (var context = (CarWashContext)_serviceProvider.GetService(typeof(CarWashContext)))
			{
				foreach (var interval in intervals)
				{
					var dateStart = DateTime.ParseExact(interval.DateStart, "yyyy-MM-ddT00:00", CultureInfo.InvariantCulture);
					var dateEnd = DateTime.ParseExact(interval.DateEnd, "yyyy-MM-ddT00:00", CultureInfo.InvariantCulture);

					var data = allCrmData.Where(s => s.DateStart.Equals(dateStart) && s.DateEnd.Equals(dateEnd)).ToArray();

					if (data.Length > 0)
					{
						crmDataRecords.AddRange(data);
						continue;
					}

					await LoadCrmDataAsync(context, dateStart, dateEnd);

					crmDataRecords.AddRange(context
						.CrmData
						.Where(s => s.DateStart.Equals(dateStart) && s.DateEnd.Equals(dateEnd))
						.ToArray());
				}
			}
		}

		private async Task LoadCrmDataAsync(CarWashContext context, DateTime dateStart, DateTime dateEnd)
		{
			var data = await CrmHelper.GetCrmDataAsync(dateStart, dateEnd, _crmConnectionString);

			await context.CrmData.AddRangeAsync(data);
			await context.SaveChangesAsync();
		}

		private void GetDeviceDataComestero(INmLogger logger, DeviceStatistics[] allDeviceStats, Device device, List<SearchPeriod> intervals, List<DeviceStatistics> statistics)
		{
			foreach (var interval in intervals)
			{
				var stats = allDeviceStats
					.FirstOrDefault(s => s.DeviceId.Equals(device.DeviceId.ToString())
					&& s.DateFrom.Equals(interval.DateStart)
					&& s.DateTo.Equals(interval.DateEnd));

				if (stats == null)  // asa 12.08.2021 раскомментил
				{
					stats = LoadStatisticsAsync(device, interval.DateStart, interval.DateEnd, logger).GetAwaiter().GetResult();
				} // asa

				lock (locker)
				{
					if (stats != null)
						statistics.Add(stats);
				}
			}
		}

		private async Task<DeviceStatistics> LoadStatisticsAsync(Device device, string dateStart, string dateEnd, INmLogger logger)
		{
			var isDone = false;
			var attemptNo = 0;

			DeviceStatistics deviceStatistics = null;

			while (!isDone && attemptNo < ComesteroRetryCount)
			{
				try
				{
					attemptNo++;
					var results = await _comesteroWashClient.GetStatisticsTotalAsync(device.DeviceId.ToString(), dateStart, dateEnd);
					results.Station = device.Name; //todo: to be renamed

					deviceStatistics = results.ToDeviceStatistics();
					await SaveStatistics(deviceStatistics);

					isDone = true;
				}
				catch (Exception ex)
				{
					await WriteErrorAsync(nameof(IndexModel), $"Machine: {device.DeviceId} ({device.Name}). Attempt: {attemptNo}. Error: {ex.Message}", nameof(OnPostAsync), logger);
				}
			}

			if (!isDone)
			{
				await WriteErrorAsync(nameof(IndexModel), $"NOT AVAILABLE Machine: {device.DeviceId} ({device.Name}). IS NOT AVAILABLE", nameof(OnPostAsync), logger);
			}

			return deviceStatistics;
		}

		private async Task SaveStatistics(DeviceStatistics deviceStatistics)
		{
			using (var context = (CarWashContext)_serviceProvider.GetService(typeof(CarWashContext)))
			{
				await context.DeviceStatistics.AddAsync(deviceStatistics);
				await context.SaveChangesAsync();
			}
		}

		private async Task CheckNewMachines()
		{
			var machinesResponse = await _comesteroWashClient.GetMashines();
			var machines = machinesResponse.Machines;

			using (var context = (CarWashContext)_serviceProvider.GetService(typeof(CarWashContext)))
			{
				foreach (var machine in machines)
				{
					//todo: maybe that sould be treatened as update every time for each device
					if (!context.Devices.Any(device => device.DeviceId == machine.Id))
					{
						context.Devices.Update(new Device()
						{
							AddressComestero = machine.Address,
							DeviceId = machine.Id,
							SerialNumber = machine.Serial,
							Name = machine.Name
						});
					}
				}

				var count = await context.SaveChangesAsync();

				if (count > 0)
					await WriteInfoAsync(nameof(IndexModel), $"New devices count: {count}", nameof(OnPostAsync));

				_memoryCache.Set(DevicesCacheKey, context.Devices.ToArray(), TimeSpan.FromMinutes(60));
			}
		}

		private async Task WriteInfoAsync(string category, string message, string method, INmLogger logger = null, string result = MethodResults.Success)
		{
			var logEntry = new CarWashLogEntry()
			{
				Level = (int)LogLevel.Information,
				Message = message,
				Method = method,
				Result = result,
				Category = category
			};

			if (logger == null)
				logger = _logger;

			await logger.WriteAsync(logEntry);
		}

		private async Task WriteErrorAsync(string category, string message, string method, INmLogger logger = null, string result = MethodResults.Failed)
		{
			var logEntry = new CarWashLogEntry()
			{
				Level = (int)LogLevel.Error,
				Message = message,
				Method = method,
				Result = result,
				Category = category
			};

			if (logger == null)
				logger = _logger;

			await logger.WriteAsync(logEntry);
		}
	}
}
