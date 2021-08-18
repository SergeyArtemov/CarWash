using System.Collections.Generic;

namespace CarWash.Reporting.Model
{
	public class PivotTable
	{
		public string Name { get; set; }

		public IEnumerable<PivotDataRecord> Records { get; set; }
	}
}
