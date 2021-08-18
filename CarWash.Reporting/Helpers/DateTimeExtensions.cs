using System;

namespace CarWash.Reporting.Helpers
{
	public static class DateTimeExtensions
	{
		public static int MonthDifference(this DateTime lValue, DateTime rValue)
		{
			return Math.Abs(lValue.Month - rValue.Month + 12 * (lValue.Year - rValue.Year));
		}
	}
}
