using System.ComponentModel.DataAnnotations;

namespace CarWash.Database
{
	public class Service
	{
		public int Id { get; set; }

		public int CellId { get; set; }

		[StringLength(50)]
		public string CellName { get; set; }

		public decimal Price { get; set; }

		public decimal Duration { get; set; }

		[StringLength(200)]
		public string Description { get; set; }
	}
}
