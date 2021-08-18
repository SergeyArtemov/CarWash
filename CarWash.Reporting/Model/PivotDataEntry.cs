using System;

namespace CarWash.Reporting.Model
{
	public class PivotDataEntry
	{
		public string Interval { get; set; }

		public int MonthNumber { get; set; }

		public decimal Total => BonusPoints + Cash + BankMobile + BankTerminal - Change;

		public decimal BonusPoints { get; set; }

		public decimal BonusPointsPercents => Total > 0 ? Math.Round(BonusPoints / Total * 100, 2) : 0;

		public decimal Cash { get; set; }

		public decimal CashPercents => Total > 0 ? Math.Round(Cash / Total * 100, 2) : 0;

		public decimal Change { get; set; }

		public decimal BankTerminal { get; set; }

		public decimal BankTerminalPercents => Total > 0 ? Math.Round(BankTerminal / Total * 100, 2) : 0;

		public decimal BankMobile { get; set; }

		public decimal BankMobilePercents => Total > 0 ? Math.Round(BankMobile / Total * 100, 2) : 0;

		public override string ToString() => $"{MonthNumber} {Total}";
	}
}
