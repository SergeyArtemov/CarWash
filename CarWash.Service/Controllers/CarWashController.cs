using CarWash.Database;
using CarWash.Service.Attributes;
using CarWash.Service.Comestero;
using CarWash.Service.Comestero.CrestWave;
using CarWash.Service.Interfaces;
using CarWash.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nm.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CarWash.Service.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[ApiErrorsAwarable]
	public class CarWashController : ControllerBase
	{
		private const string CacheKeyMachines = "machines";
		private const string CacheKeyCells = "cells";

		private readonly ComesteroWashClient _comesteroClient;

		private readonly IMemoryCache _memoryCache;
		private readonly ICarWashLogger<CarWashController> _carWashLogger;

		public CarWashController(IConfiguration configuration, IMemoryCache memoryCache, ICarWashLogger<CarWashController> carWashLogger)
		{
			var xauth = configuration.GetValue<string>("Comestero:XAuth");
			_comesteroClient = new ComesteroWashClient(xauth);

			_memoryCache = memoryCache;
			_carWashLogger = carWashLogger;
		}

		/// <summary>
		/// Получение информации о мойке (s/n etc.)
		/// </summary>
		/// <param name="header">Токен авторизации</param>
		/// <param name="request">Запрос с ID мойки.</param>
		/// <returns></returns>
		[HttpPost]
		[Route("carwash")]		
		public async Task<IActionResult> CarWash([FromHeader(Name = "X-Authorization")] string header, IdModel request)
		{
			var machines = _memoryCache.Get(CacheKeyMachines) as Machine[];

			if (machines == null)
			{
				_comesteroClient.SetAuthorization(header);

				var machinesResponse = await _comesteroClient.GetMashines();
				machines = machinesResponse.Machines;
				_memoryCache.Set(CacheKeyMachines, machines, DateTimeOffset.UtcNow.AddDays(1));
			}

			var result = machines.FirstOrDefault(m => m.Id == request.Id);
			return new JsonResult(result);
		}

		/// <summary>
		/// Плучение списка программ для портальной мойки
		/// </summary>		
		/// <param name="request">Запрос с серийным номером портальной мойки.</param>
		/// <returns></returns>
		[HttpPost]
		[Route("getcells")]
		public async Task<IActionResult> GetCells([FromHeader(Name = "X-Authorization")] string header, SerialModel request)
		{
			_comesteroClient.SetAuthorization(header);

			/*var cells = _memoryCache.Get(CacheKeyCells) as WasherCell[];

			if (cells == null)
			{
				var cellsResponse = await _comesteroClient.GetCells(request.Serial);
				cells = cellsResponse.Cells;
				_memoryCache.Set(CacheKeyCells, cells, DateTimeOffset.UtcNow.AddDays(1));
			}*/

			var cellsResponse = await _comesteroClient.GetCells(request.Serial);
			var cells = cellsResponse.Cells;
			var result = cells.ToArray();
			return new JsonResult(result);
		}

		/// <summary>
		/// Получить полный список программ всех портальных моек.
		/// </summary>
		/// <param name="header"></param>
		/// <returns></returns>
		[HttpPost]
		[Route("getallcells")]
		public async Task<IActionResult> GetAllCells([FromHeader(Name = "X-Authorization")] string header)
		{
			_comesteroClient.SetAuthorization(header);

			var result = await _comesteroClient.GetEverything();
			return new JsonResult(result);
		}

		/// <summary>
		/// Зачисление кредитов на мойку.
		/// </summary>
		/// <param name="serial">Серийный номер устройства.</param>		
		/// <param name="amount">Количество кредитов (фактическая облата).</param>
		/// <param name="bonusAmount">Количество кредитов (бонусы).</param>
		/// <param name="cellId">Id ячейки с программой (для портальных моек обязательно).</param>
		/// <param name="postId">Id поста (необязательно).</param>
		/// <returns>Событие со статусом IN_PROGRESS. Позже статус сменится на ОК или ERROR. Проверять методом GetEventResponse(event_id, sn)</returns>
		[HttpPost]
		[Route("sendcredits")]
		public async Task<IActionResult> SendCredits([FromHeader(Name = "X-Authorization")] string header, string serial, decimal amount, decimal bonusAmount, string cellId, string postId)
		{
			var logEntry = new CarWashLogEntry()
			{
				Message = "Sending credits",
				Level = (int)LogLevel.Information,
				Method = "SendCredits",
				Request = JsonConvert.SerializeObject(new CreditsRequest()
				{
					BonusAmount = bonusAmount,
					CellId = cellId,
					CreditAmount = amount,
					PostId = postId,
					Serial = serial
				}),
				Result = "Error" // yeah that looks weird but ok
			};

			try
			{
				_comesteroClient.SetAuthorization(header);

				var result = await _comesteroClient.SendCredits(serial, amount, bonusAmount, cellId.ToString(), null);
				logEntry.Result = "ok";
				logEntry.Extras = JsonConvert.SerializeObject(result);
				await _carWashLogger.WriteAsync(logEntry);

				return new JsonResult(result);
			}
			catch (ApiCallException ex)
			{
				logEntry.Message = ex.Message;
				logEntry.ExceptionMessage = ex.Message;
				logEntry.Extras = ex.Response.ErrorDescription;
				logEntry.Level = (int)LogLevel.Error;
				await _carWashLogger.WriteAsync(logEntry);
				return new JsonResult(ex);
			}
			catch (Exception e)
			{
				logEntry.ExceptionMessage = e.Message;
				logEntry.Extras = e.ToString();
				logEntry.Level = (int)LogLevel.Error;
				await _carWashLogger.WriteAsync(logEntry);
				return new JsonResult(e);
			}
		}

		/// <summary>
		/// Проверка статуса зачисления кредитов
		/// </summary>
		/// <param name="header"></param>
		/// <param name="eventId">EventId, полученное в методе начисления кредитов.</param>
		/// <param name="sn">Серийный номер устройства (необязательно).</param>
		/// <returns></returns>
		[HttpPost]
		[Route("geteventresponse")]
		public async Task<IActionResult> GetGetEventResponse([FromHeader(Name = "X-Authorization")] string header, int eventId, string sn)
		{
			_comesteroClient.SetAuthorization(header);

			var result = await _comesteroClient.GetEventResponse(eventId, sn); ;
			return new JsonResult(result);
		}

		/// <summary>
		/// Получение списка устройств.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("getmachines")]
		public async Task<IActionResult> GetMachines([FromHeader(Name = "X-Authorization")] string header)
		{
			_comesteroClient.SetAuthorization(header);
			var result = await _comesteroClient.GetMashines();

			return new JsonResult(result);
		}


		/// <summary>
		/// Получение статуса устройства.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("getmachinestatus")]
		public async Task<IActionResult> GetMachineStatus([FromHeader(Name = "X-Authorization")] string header, string deviceId)
		{
			_comesteroClient.SetAuthorization(header);
			var result = await _comesteroClient.GetMachineStatus(deviceId);
			return new JsonResult(result);
		}

		/// <summary>
		/// Получение статистики закрытия ККТ за период.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="deviceId">ИД мойки</param>
		/// <param name="from">Начало периода</param>
		/// <param name="to">Конец периода</param>
		/// <returns></returns>
		[HttpGet]
		[Route("kktstats")]
		public async Task<IActionResult> GetMachineStats([FromHeader(Name = "X-Authorization")] string header, string deviceId, string from, string to)
		{
			_comesteroClient.SetAuthorization(header);
			var result = await _comesteroClient.GetStatisticsAsync(deviceId, from, to);
			return new JsonResult(result);
		}

		/// <summary>
		/// Получение сводной суммы по мойке за период.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="deviceId">ИД мойки</param>
		/// <param name="from">Начало периода</param>
		/// <param name="to">Конец периода</param>
		/// <returns></returns>
		[HttpGet]
		[Route("kktstatstotal")]
		public async Task<IActionResult> GetMachineStatsTotal([FromHeader(Name = "X-Authorization")] string header, string deviceId, string from, string to)
		{
			_comesteroClient.SetAuthorization(header);
			var result = await _comesteroClient.GetStatisticsTotalAsync(deviceId, from, to);
			return new JsonResult(result);
		}

		[ApiExplorerSettings(IgnoreApi = true)]
		[HttpGet]
		[Route("hey")]
		public async Task<string> Hey()
		{
			return await Task.Run(() => { return "hey hey!"; });
		}
	}
}
