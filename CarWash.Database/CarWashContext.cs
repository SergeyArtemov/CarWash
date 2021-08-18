using Microsoft.EntityFrameworkCore;

namespace CarWash.Database
{
	public interface ICarWashContext
	{
	}

	public class CarWashContext : DbContext, ICarWashContext
	{
		public DbSet<CarWashLogEntry> Log { get; set; }

		public DbSet<Service> Services { get; set; }

		public DbSet<GasStation> GasStations { get; set; }

		public DbSet<DeviceStatistics> DeviceStatistics { get; set; }

		public DbSet<Device> Devices { get; set; }

		public DbSet<CrmDataRecord> CrmData { get; set; }

		public CarWashContext(DbContextOptions<CarWashContext> dbContextOptions)
			: base(dbContextOptions)
		{
			Database.EnsureCreated();
		}
	}
}
