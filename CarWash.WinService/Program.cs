using System;
using CarWash.Reporting;
using CarWash.Database;
using CarWash.Reporting.Helpers;
using CarWash.Reporting.Model;
using CarWash.Service.Comestero;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

using CarWash.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using System.IO;
using Nm.Logging;
using Nm.Constants;
using Microsoft.Extensions.Logging;

namespace CarWash.WinService
{
	class Program
	{
		static void Main(string[] args)
		{

			Console.WriteLine(DateTime.Now.ToString() + "..CarWash.WinService strats.");

			CarWashStat cws = new CarWashStat();

			Count();

		}

		public static void Count()
		{

			for (Int64 i = 0; ; i++)
			{

				if (DateTime.Now.Hour == Convert.ToInt32(ConfigurationManager.AppSetting["hourForUpdatingData"]) || i == 0)  // обновляем в X часов утра, а также при первом запуске программы.
				{
					CarWashStat cws = new CarWashStat();

					Console.WriteLine(DateTime.Now.ToString() + "...START updating of the Machine list .");
					Task t = new Task(() => { cws.CheckNewMachines(); });
					t.Start();

					Thread.Sleep(1000 * 10);  // Ожидаем  пока прогрузятся новые машины. Потом подумать о замене на семафор.

					Console.WriteLine(DateTime.Now.ToString() + "...START updating data from the Comestero and from the Nmlos.");
					Task t2 = new Task(() => { cws.OnPostAsync(i); });
					t2.Start();

					Thread.Sleep(1000 * 60 * 60);   // каждый час просыпаемся и проверяем - а не время ли сейчас для обновления.
				}
			}
		}

	}

	public class CarWashStat
	{
		public MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
		private const string DevicesCacheKey = "Devices";
		private const int ComesteroRetryCount = 1;
		private int CountUpd = 0;
		private int countdevices = 0;

		private readonly INmLogger _logger;
		private readonly ComesteroWashClient _comesteroWashClient;
		//public IMemoryCache _memoryCache;
		private readonly IServiceProvider _serviceProvider;
		private readonly string _crmConnectionString;

		private static readonly object locker = new object();

		private SemaphoreSlim inProgressSemaphore = new SemaphoreSlim(1);

		public bool InProgress => inProgressSemaphore.CurrentCount > 0;

		public CarWashStat(/*INmLogger logger, IConfiguration configuration, IMemoryCache memoryCache, IServiceProvider serviceProvider*/)
		{
			string xauth = ConfigurationManager.AppSetting["xauth"];
			_crmConnectionString = ConfigurationManager.AppSetting["crmConnectionString"];
			_comesteroWashClient = new ComesteroWashClient(xauth);
			_serviceProvider = (IServiceProvider)ServiceProviderFactory.ServiceProvider.GetService(typeof(IServiceProvider)); // asa
		}

		public async Task OnGetAsync()
		{
			_memoryCache.TryGetValue(DevicesCacheKey, out Device[] devices);

			if (devices == null)
				await CheckNewMachines();
		}

		public async Task<int> OnPostAsync(Int64 i)  // было Task<IActionResult>  asa
		{

			//Console.WriteLine("******Start task OnPostAsync");
			if (inProgressSemaphore.CurrentCount == 0)
				return 0;

			CountUpd = 0;

			inProgressSemaphore.Wait();

			try
			{

				var devices = _memoryCache.Get<Device[]>(DevicesCacheKey);
				countdevices = devices.Length;

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


				DateTime dateStart1, dateStart2, dateStart, dateEnd;

				if (i == 0 && Convert.ToInt32(ConfigurationManager.AppSetting["forceUpdatingYear"]) > 0)  // при первом проходе обновляем указанный месяц
				{

					int m1, m2;
					if (Convert.ToInt32(ConfigurationManager.AppSetting["forceUpdatingMonth"]) > 0)
					{
						m1 = Convert.ToInt32(ConfigurationManager.AppSetting["forceUpdatingMonth"]);
						m2 = Convert.ToInt32(ConfigurationManager.AppSetting["forceUpdatingMonth"]);
					}
					else { m1 = 1; m2 = 12; }  //обновляем за весь год

					dateStart = new DateTime(Convert.ToInt32(ConfigurationManager.AppSetting["forceUpdatingYear"]), m1, 1);
					dateEnd = new DateTime(Convert.ToInt32(ConfigurationManager.AppSetting["forceUpdatingYear"]), m2, 1).AddMonths(1);
				}
				else
				{
					dateStart1 = Convert.ToDateTime(allDeviceStats.Max(x => x.DateTo));
					dateStart2 = allCrmData.Max(x => x.DateEnd);
					dateStart = dateStart1.CompareTo(dateStart2) > 0 ? dateStart2 : dateStart1;
					dateStart = dateStart.CompareTo(DateTime.Now.AddDays((-1) * Convert.ToInt32(ConfigurationManager.AppSetting["updateDaysAgo"]))) < 0
													? DateTime.Now.AddDays((-1) * Convert.ToInt32(ConfigurationManager.AppSetting["updateDaysAgo"]))
													: dateStart; // Обновлаяем с глубиной не более "updateDaysAgo" дней
					dateEnd = DateTime.Now.AddDays(-1);
				}


				var intervals = new List<SearchPeriod>();
				var t = dateStart;

				while (t < dateEnd)
				{
					intervals.Add(new SearchPeriod()
					{
						DateStart = t.ToString("yyyy-MM-ddT00:00"),
						DateEnd = t.AddDays(1).ToString("yyyy-MM-ddT00:00") 
					});

					t = t.AddDays(1);  // asa было: t.AddMonths(1);
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
					//	await WriteErrorAsync(nameof(IndexModel), ex.ToString(), nameof(OnPostAsync)); asa
					}

					

				}

				var result = new PivotTable()
				{
					Name = "table1",
					Records = records.OrderBy(r => r.StationNumber).Where(r => r.HasData)
				};

				_memoryCache.Set("Results", result);
				Console.WriteLine(DateTime.Now.ToString() + "...FINISHED updating data from Comestero and Nmlos.");
				return 100; //Redirect("/Preview");
			}
			catch (Exception ex)
			{
				return 100; 
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

				if (stats == null) 
				{
					CountUpd++;
					if (CountUpd%300 ==  0)Console.WriteLine(DateTime.Now.ToString() + "..."+(CountUpd * 100 / (intervals.Count * countdevices+20) + " % " ).ToString());  //124 
					stats = LoadStatisticsAsync(device, interval.DateStart, interval.DateEnd, logger).GetAwaiter().GetResult();  // asa 123
				} // asa

				lock (locker)
				{
					if (stats != null)
					{
						statistics.Add(stats);
						
					}
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

				}
			}

			if (!isDone)
			{

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

		public async Task CheckNewMachines()
		{

			var machinesResponse = await _comesteroWashClient.GetMashines();  // asa
			var machines = machinesResponse.Machines;

			using (var context = (CarWashContext)_serviceProvider.GetService(typeof(CarWashContext)))
			{
				foreach (var machine in machines)
				{
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

				var count = await context.SaveChangesAsync();   // asa

				if (count > 0)
					Console.WriteLine(DateTime.Now.ToString() + $" New Machines count: {count}", nameof(OnPostAsync));
			 _memoryCache.Set(DevicesCacheKey, context.Devices.ToArray(), TimeSpan.FromMinutes(60));  
			}

			Console.WriteLine(DateTime.Now.ToString() + "...FINISHED updating Machine list.");
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
	


	public static class ServiceProviderFactory   
	{
		public static IServiceProvider ServiceProvider { get; }

		static ServiceProviderFactory()
		{
			HostingEnvironment env = new HostingEnvironment();
			env.ContentRootPath = Directory.GetCurrentDirectory();
			env.EnvironmentName = "Development";

			Startup startup = new Startup(null);
			ServiceCollection sc = new ServiceCollection();
			startup.ConfigureServices(sc);
			ServiceProvider = sc.BuildServiceProvider();
		}
	}

	static class ConfigurationManager
	{
		public static IConfiguration AppSetting { get; }
		static ConfigurationManager()
		{
			AppSetting = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("WinServiceSettings.json")
					.Build();
		}
	}
}
