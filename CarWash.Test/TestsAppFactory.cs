using CarWash.Service;
using CarWash.Service.Comestero.CrestWave;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CarWash.Test
{
	public class TestsAppFactory
	{
		private HttpClient _httpClient;

		[SetUp]
		public void Setup()
		{
			var factory = new WebApplicationFactory<Startup>();
			factory.ClientOptions.BaseAddress = new Uri("https://localhost:4435/");

			var conf = (IConfiguration)factory.Services.GetService(typeof(IConfiguration));
			var header = conf.GetValue<string>("Comestero:XAuth");

			_httpClient = factory.CreateClient();
			_httpClient.DefaultRequestHeaders.Add("XAuth", header);
		}

		[Test]
		public async Task TestGetMachines()
		{
			var result = await _httpClient.GetAsync("api/CarWash/getmachines");
			var machinesResponse = await result.Content.ReadAsAsync<MashinesResponse>();

			Assert.NotNull(machinesResponse);
		}
	}
}
