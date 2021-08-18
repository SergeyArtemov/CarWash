using CarWash.Database;
using CarWash.Service;
using CarWash.Service.Comestero;
using CarWash.Service.Comestero.CrestWave;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;//asa
using System.Text.Unicode;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace CarWash.Test
{
	public class Tests
	{

		// asa
		class MachineNameStnd : Machine, IComparable   // класс для группировки и сортировки Macnhine
		{
			public string NameStnd { get; set; }
			public int CompareTo(object o)
            {
				MachineNameStnd mn = (MachineNameStnd)o;

				if (Int32.Parse(mn.NameStnd) > Int32.Parse(this.NameStnd))
					return -1;
				else if (Int32.Parse(mn.NameStnd) < Int32.Parse(this.NameStnd))
					return 1;
				else return 0;
            }
		}
		// asa

		private ComesteroWashClient _comesteroClient;
		private IConfiguration _configuration;
		private string _snGantry;// = "0cefafcb64dc";
		private string _deviceIdGantry = "2941";
		private IServiceProvider _serviceProvider;

		private string _deviceIdWss = "5582";
		private string _snWss = "0cefafc4efd4";
		public string headerforlook; 

		public Tests()
		{
		}

		[OneTimeSetUp]
		public void Setup()
		{
			var appFactory = new WebApplicationFactory<Startup>();
			var conf = (IConfiguration)appFactory.Services.GetService(typeof(IConfiguration));
			var header = conf.GetValue<string>("Comestero:XAuth");
			_snGantry = conf.GetValue<string>("Comestero:Sn");
			_deviceIdGantry = conf.GetValue<string>("Comestero:Id");

			_comesteroClient = new ComesteroWashClient(header);
			_serviceProvider = (IServiceProvider)appFactory.Services.GetService(typeof(IServiceProvider));

			headerforlook = header; // asa

		}

		[Test]
		public async Task TestGetMachines()
		{
			var result = await _comesteroClient.GetMashines();

			Console.WriteLine("header:"+headerforlook);

			// asa 10082021 Выводим результаты теста в консоль
			JsonSerializerOptions options = new JsonSerializerOptions  // Настраиваем русскую кодировку для сериалиазции в json
			{
				Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
				WriteIndented = true
			};

			

			// Вырезаем из названия номер машины в трех вариантах написания.
			string pattern = @" \d\d |\(\d\d\)|\d\d\d|(\d)|\(\d\d-|\(\d\d -";
			var selectedMach = from m in result.Machines
							   where (Regex.IsMatch(m.Name, pattern, RegexOptions.IgnoreCase))
							   select m ;

			Console.WriteLine("ВСЕГО:" + selectedMach.Count<Machine>().ToString());

			// Копия массива Machine, но с доп.полем равным номеру машины
			MachineNameStnd[] mmm = new MachineNameStnd[selectedMach.Count<Machine>()];

			Regex reg = new Regex(pattern);

			int i = 0;
			foreach (Machine m in selectedMach)
			{
				var mm = reg.Match(m.Name);

				Console.WriteLine(mm.Groups[0].Value.Replace("(", "").Replace(")", "").Replace("-", ""));
				Console.WriteLine(JsonSerializer.Serialize<Machine>(m, options));

				mmm[i] = new MachineNameStnd(); 
				mmm[i].Id = m.Id;
				mmm[i].Serial = m.Serial;
				mmm[i].Name = m.Name;
				mmm[i].Address = m.Address;
				mmm[i].NameStnd = mm.Groups[0].Value.Replace("(", "").Replace(")", "").Replace("-","");
				i++;
			}

			Console.WriteLine("----------------------------------------------------------------");

			Array.Sort(mmm);
			string prev = "";
			int cnt1 = 0;
			int cnt2 = 0;
			
			foreach(var c in mmm)
            {
				if (c.NameStnd != prev)
				{
					cnt2++;  // Сквозной счетчик по АЗС
				}

				cnt1++; // сквозной счетчик по Machine

						Console.WriteLine("#Machine=" + cnt1.ToString() + "/" +"#АЗС=" + cnt2.ToString() + "   " + c.NameStnd + " / " + c.Name + " / " + (c.Id).ToString() + " / " + c.Address + "");

					prev = c.NameStnd;
				//}

            };
			// asa 10082021

			Assert.IsTrue(result.Machines.Length > 0);
		}

		[Test]
		public async Task TestRequestCells()
		{
			var request = new WasherCellsRequest()
			{
				Serial = _snGantry
			};

			var result = await _comesteroClient.GetCells(request);
			Assert.IsTrue(result.Cells.Length > 0);
		}

		[Test]
		public async Task TestGetStatus()
		{
			var result = await _comesteroClient.GetMachineStatus("4421");
			Assert.IsTrue(result.Code == 0);
		}

		[Test]
		public async Task TestGetShiftStatistics()
		{
			var result = await _comesteroClient.GetStatisticsAsync(_deviceIdGantry, "2021-01-01T01:00", "2021-05-05T01:00");
			Assert.IsTrue(result.Code == 0);
		}

		[Test]
		public async Task TestGetShiftStatisticsTotal()
		{
			var result = await _comesteroClient.GetStatisticsTotalAsync("3301", "2021-03-01T00:00", "2021-04-01T00:00");
			Assert.IsTrue(result.Code == 0);
		}

		[Test]
		public async Task GetEventStratus()
		{
			var result = await _comesteroClient.GetEventResponse(152873558, "40d63c34c181_1561");
			Assert.IsTrue(result.Code == 0);
		}

		[Test]
		public async Task TestSendCreditsCellId()
		{
			var result = await _comesteroClient.SendCredits(_snGantry, 300, 0, "14704", _deviceIdGantry);
			var result2 = await _comesteroClient.SendCredits(_snWss, 10, 0, null, _deviceIdWss);
			Assert.IsTrue(result2.Code == 0);
		}

		[Test]
		public async Task TestGetCellsBySn()
		{
			var result2 = await _comesteroClient.GetCells("0cefafcb64dc");
			Assert.IsTrue(result2.Code == 0);
		}

		[Test]
		public async Task TestGetEverythingFromEverywhere()
		{
			var result = await _comesteroClient.GetMashines();
			var allCells = new List<WasherCell>();

			var ms = result.Machines.Where(m => m.Name.Contains("43")).ToArray();
			var ms51 = result.Machines.Where(m => m.Name.Contains("51")).ToArray();

			foreach (var machine in result.Machines)
			{
				var cells = await _comesteroClient.GetCells(new WasherCellsRequest()
				{
					Serial = machine.Serial
				});

				if (cells.Cells.Length > 0)
				{
					allCells.AddRange(cells.Cells);
				}
			}

			Assert.IsTrue(allCells.Count > 0);
		}

		[Test]
		public async Task TestSendCreditsCwss()
		{
			var result = await _comesteroClient.SendCredits("40d63c34c181_1561", 1, 0, null, "16065");
			Assert.IsTrue(result.Code == 0);
		}

		[Test]
		public async Task TestSetStationsDeviceMatches()
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				using (var context = scope.ServiceProvider.GetService<CarWashContext>())
				{
					var stations = context.GasStations.ToArray();

					foreach (var station in stations)
					{
						var number = station.Number;
						var devices = context.Devices.ToArray().Where(d => d.StationNumber.Equals(number)).ToList();
						devices.ForEach(d =>
						{
							context.Attach(d);							
							d.GasStation = station;
							context.Entry(d).State = EntityState.Modified;
						});
					}

					var result = await context.SaveChangesAsync();
					Assert.GreaterOrEqual(result, 0);
				}
			}
		}
	}


}