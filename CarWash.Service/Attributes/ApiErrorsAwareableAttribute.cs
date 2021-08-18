using CarWash.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Nm.Dal.Models;
using Nm.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CarWash.Service.Attributes
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ApiErrorsAwarableAttribute : ExceptionFilterAttribute
	{
		public ApiErrorsAwarableAttribute()
		{
		}

		public override async Task OnExceptionAsync(ExceptionContext context)
		{
			var apiException = context.Exception as ApiCallException;
			string requestBody = string.Empty;

			var logger = context.HttpContext.RequestServices.GetService(typeof(ICarWashLogger)) as ICarWashLogger;

			LogEntry logEntry = new LogEntry()
			{
				Level = (int)LogLevel.Error,
				Method = context.HttpContext.Request.Path.ToString()
			};

			try
			{
				requestBody = await GetBodyStringContentAsync(context.HttpContext.Request.Body);
			}
			catch (Exception ex)
			{
				requestBody = $"Error occured on reading Request content. {ex}";
			}
			finally
			{
				logEntry.Request = requestBody;
			}

			if (apiException != null)
			{
				logEntry.ExceptionMessage = apiException.ToString();
				logEntry.Result = apiException.Response.Status;
				logEntry.Message = apiException.Response.ErrorMessage;
				logEntry.Extras = apiException.Response.ErrorDescription;
				context.Result = new ObjectResult(apiException.Response);
			}
			else
			{
				logEntry.ExceptionMessage = context.Exception.ToString();
				logEntry.Result = context.HttpContext.Response.StatusCode.ToString();
				logEntry.Message = GetInternalMessages(context.Exception);
				logEntry.Extras = context.HttpContext.Request.QueryString.ToString();
			}

			await logger?.WriteAsync(logEntry);
			context.ExceptionHandled = true;
		}

		private async Task<string> GetBodyStringContentAsync(Stream body)
		{
			string content;

			using (Stream receiveStream = body)
			{
				using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
				{
					content = await readStream.ReadToEndAsync();
				}
			}

			return content;
		}

		private string GetInternalMessages(Exception exception)
		{
			var stringBuilder = new StringBuilder();
			var ex = exception;

			stringBuilder.AppendLine(ex.Message);
			ex = ex.InnerException;

			while (ex != null)
			{
				stringBuilder.AppendLine(ex.Message);
				ex = ex.InnerException;
			}

			return stringBuilder.ToString();
		}
	}
}
