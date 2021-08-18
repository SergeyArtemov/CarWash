using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace CarWash.Database
{
	public class Device
	{
		public int Id { get; set; }

		public int DeviceId { get; set; }

		public string Name { get; set; }

		public string SerialNumber { get; set; }

		[Column("Address")]
		public string AddressComestero { get; set; }

		public virtual GasStation GasStation { get; set; }

		public string StationNumber => Regex.Match(Name, @"\d+").Value;
	}
}
