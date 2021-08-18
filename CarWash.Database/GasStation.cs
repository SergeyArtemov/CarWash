using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CarWash.Database
{
	public class GasStation
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string CrmCode { get; set; }

		public int CrmSysCode { get; set; }

		public string CrmAddress { get; set; }

		public int EssStationId { get; set; }

		public string Description { get; set; }

		public List<Device> Devices { get; set; }

		public string Number => Regex.Match(Name, @"\d+").Value;
	}
}
