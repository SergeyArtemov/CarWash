using System.Collections.Generic;
using System.Linq;

namespace CarWash.Reporting.Model
{
	public class PivotDataRecord
	{
		public string StationNumber { get; set; }

		public List<PivotDataEntry> RecordData { get; set; } = new List<PivotDataEntry>();

		public override string ToString() => StationNumber;

		public bool HasData => RecordData.Any(r => r.Total > 0);
	}
}
