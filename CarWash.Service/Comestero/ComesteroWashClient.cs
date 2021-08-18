using CarWash.Service.Comestero.CrestWave;
using Nm.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarWash.Service.Comestero
{
	public class ComesteroWashClient : ApiClient
	{
		public override int DefaultTimeoutInSeconds => 300;

		private List<KeyValuePair<string, string>> _headers { get; set; } = new List<KeyValuePair<string, string>>();
		private const string CrestWaveBaseUrl = "https://vending.crest-wave.com";
		private const string AuthHeaderName = "X-Authorization";

		public ComesteroWashClient(string authHeader)
		{
			InitHeaders(authHeader);
		}

		/// <summary>
		/// Полный список доступных устройств
		/// </summary>
		/// <returns>Список доступных устройств</returns>
		public Task<MashinesResponse> GetMashines()
		{
			var  mashs = GetAsync<MashinesResponse>("/washer/v1/machines", null, _headers);
			return mashs;//GetAsync<MashinesResponse>("/washer/v1/machines", null, _headers);  // asa
		}

		public void SetAuthorization(string xauth)
		{
			if (_headers == null)
			{
				_headers = new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>(AuthHeaderName, xauth)
				};
				return;
			}

			var header = _headers.Find(m => m.Key.Equals(AuthHeaderName));

			if (string.IsNullOrEmpty(header.Value))
			{
				_headers.Add(new KeyValuePair<string, string>(AuthHeaderName, xauth));
			}
		}

		public Task<EventResponse> SendCredits(CreditsRequest request)
		{
			return SendObjectAsync<CreditsRequest, EventResponse>("/api/v1/send_credit", null, request, "POST", false, _headers);
		}

		public async Task<WasherCell[]> GetEverything()
		{
			var result = await GetMashines();
			var allCells = new List<WasherCell>();

			foreach (var machine in result?.Machines)
			{
				var cells = await GetCells(new WasherCellsRequest()
				{
					Serial = machine.Serial
				});

				if (cells.Cells.Length > 0)
				{
					allCells.AddRange(cells.Cells);
				}
			}

			return allCells.ToArray();
		}

		/// <summary>
		/// Метод зачисления кредитов на устройство.
		/// </summary>
		/// <param name="serial">Серийный номер устройства (из GetMachines)</param>		
		/// <param name="amount">Количество кредитов для зачесления (фактическая облата)</param>
		/// <param name="bonusAmount">Количество кредитов для зачесления (бонусы)</param>
		/// <param name="cellId">Id ячейки на панели (для портальных моек, получется из GetCells)</param>
		/// <param name="postId"></param>
		/// <returns></returns>
		public Task<EventResponse> SendCredits(string serial, decimal amount, decimal bonusAmount, string cellId, string postId = null)
		{
			var creditsRequest = new CreditsRequest()
			{
				CreditAmount = amount,
				Serial = serial,
				BonusAmount = bonusAmount,
				PostId = postId,
				CellId = cellId
			};

			return SendCredits(creditsRequest);
		}

		public Task<EventResponse> GetEventResponse(int? eventId, string sn)
		{
			return SendObjectAsync<EventRequest, EventResponse>($"/api/v1/get_event_status", null, new EventRequest() { Serial = sn, EventId = eventId }, "POST", false, _headers);
		}

		public async Task<StatisticsResponse> GetStatisticsAsync(string deviceId, string from, string to)
		{
			return await GetAsync<StatisticsResponse>($"/washer/v1/machines/{deviceId}/kktStats?from={from}&to={to}", null, _headers);
		}

		public async Task<StatisticsTotalResponse> GetStatisticsTotalAsync(string deviceId, string from, string to)
		{
			var records = await GetStatisticsAsync(deviceId, from, to);

			var totalResponse = new StatisticsTotalResponse()
			{
				DateFrom = from,
				DateTo = to,
				ErrorCode = records.ErrorCode,
				ErrorDescription = records.ErrorDescription,
				ErrorMessage = records.ErrorMessage,
				Code = records.Code,
				HttpStatusCode = records.HttpStatusCode,
				Message = records.Message,
				Status = records.Status,
				DeviceId = deviceId
			};

			totalResponse.CardTotal = records.KktStats.Sum(s => s.PosIncome);
			totalResponse.CashTotal = records.KktStats.Sum(s => s.CashIncome);
			totalResponse.ChangeTotal = records.KktStats.Sum(s => s.Change);
			totalResponse.SalesTotal = records.KktStats.Sum(s => s.Sales);

			return totalResponse;
		}

		public async Task<StatisticsTotalResponse> GetStatisticsTotalAsync(Machine machine, string from, string to)
		{
			var records = await GetStatisticsTotalAsync(machine.Id.ToString(), from, to);
			records.Station = machine.Name;

			return records;
		}

		public Task<WasherCellsResponse> GetCells(string serial)
		{
			var request = new WasherCellsRequest()
			{
				Serial = serial
			};
			return GetCells(request);
		}

		public Task<WasherCellsResponse> GetCells(WasherCellsRequest request)
		{
			return SendObjectAsync<WasherCellsRequest, WasherCellsResponse>("/api/v1/get_washer_cells", null, request, "POST", false, _headers);
		}

		public Task<StatusResponse> GetMachineStatus(string deviceId)
		{
			return GetAsync<StatusResponse>($"/washer/v1/machine/{deviceId}/status", null, _headers);
		}

		private void InitHeaders(string xAuthHeaderValue)
		{
			SetAuthorization(xAuthHeaderValue);
			Init(CrestWaveBaseUrl, string.Empty);
		}
	}
}
