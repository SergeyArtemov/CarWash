﻿using CarWash.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nm.Dal.Interfaces;
using Nm.Dal.Repositories;
using Nm.Logging;


namespace CarWash.WinService
{
    class Startup
    {
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

			var carWashConnectingString = ConfigurationManager.AppSetting["carWashConnectingString"]; // "Server=cows020\\MSSQLSERVER01;Database=CarWash;Integrated Security=true;"

			services.AddMemoryCache();

			//services.AddDbContext<CarWashContext>(o => o.UseSqlServer(Configuration.GetConnectionString("CarWash")), ServiceLifetime.Transient); asa -
			//services.AddDbContext<CarWashContext>(o => o.UseSqlServer("Server=cows020\\MSSQLSERVER01;Database=CarWash;Integrated Security=true;"), ServiceLifetime.Transient); //asa +
			services.AddDbContext<CarWashContext>(o => o.UseSqlServer(carWashConnectingString), ServiceLifetime.Transient);
			services.AddTransient<ILogRepository, LogRepository<CarWashContext>>();
			services.AddTransient<INmLogger, NmLogger>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		/*
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				//app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapRazorPages();
			});
		}*/
	}
}



