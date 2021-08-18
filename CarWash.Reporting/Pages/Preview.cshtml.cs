using CarWash.Reporting.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CarWash.Reporting.Pages.Income
{
	public class PreviewModel : PageModel
	{
		public PivotTable Data { get; set; }
		private readonly IMemoryCache _memoryCache;

		public PreviewModel(IMemoryCache memoryCache) : base()
		{
			_memoryCache = memoryCache;
		}

		public IActionResult OnGet()
		{
			if (!_memoryCache.TryGetValue("Results", out PivotTable data))
			{
				return Redirect("/Index");
			}

			Data = data;

			return Page();
		}
	}
}
