using CarWash.Database;
using CarWash.Service.Interfaces;
using CarWash.Service.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Nm.Dal.Interfaces;
using Nm.Dal.Repositories;
using Nm.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CarWash.Service
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();
			services.AddMemoryCache();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "CarWash Service API", Version = "v1" });
				c.ResolveConflictingActions(a => a.First());
				c.CustomSchemaIds(s => s.FullName);				
				var path = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
				c.IncludeXmlComments(path);
			});

			var connectionString = Configuration.GetConnectionString("CarWash");

			services.AddDbContext<CarWashContext>(o => { o.UseSqlServer(connectionString); }, ServiceLifetime.Transient);
			services.AddTransient(typeof(INmLogger), typeof(NmLogger));

			services.AddTransient<ILogRepository, LogRepository<CarWashContext>>();

			services.AddTransient<ICarWashLogger, CarWashLogger>();
			services.AddTransient(typeof(ICarWashLogger<>), typeof(CarWashLogger<>));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
			}
			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.UseSwagger();

			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "CarWash Service API V1");
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
