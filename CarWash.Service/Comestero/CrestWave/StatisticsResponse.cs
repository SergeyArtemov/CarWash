using Newtonsoft.Json;
using System;

namespace CarWash.Service.Comestero.CrestWave
{
	public class StatisticsResponse : CrestWaveResponse
	{
		/*0 Запрос отправлен успешно
		1 Отсутствует токен
		2 Неверный токен, пользователь не найден
		3 Пользователь заблокирован
		4 Идентификатор устройства не задан
		5 Устройство с указанным id не найдено
		6 Неверный тип устройства
		7 Устройство не доступно пользователю
		8 Неверный формат даты
		500 Ошибка сервера*/
		[JsonProperty("kkt_stats")]
		public KktStat[] KktStats { get; set; }
	}
}
