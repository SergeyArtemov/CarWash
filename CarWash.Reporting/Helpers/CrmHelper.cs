using CarWash.Database;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarWash.Reporting.Helpers
{
	public static class CrmHelper
	{
		public static async Task<List<CrmDataRecord>> GetCrmDataAsync(DateTime dateStart, DateTime dateEnd, string connectionString)
		{
			var data = new List<CrmDataRecord>();

			using (var connection = new SqlConnection(connectionString))
			{
				await connection.OpenAsync();

				using (var command = new SqlCommand(GetCarWashIncomeRequest(dateStart, dateEnd), connection))
				{
					var dataReader = command.ExecuteReader();

					while (dataReader.Read())
					{
						var record = new CrmDataRecord()
						{
							CrmCode = dataReader["CrmCode"].ToString(),
							PayTypeName = dataReader["PayTypeName"].ToString(),
							SumAmount = Convert.ToDecimal(dataReader["SumAmount"]),
							SumTotal = Convert.ToDecimal(dataReader["SumTotal"]),
							EssStationId = dataReader["EssStationId"].ToString(),
							DateStart = dateStart,
							DateEnd = dateEnd
						};

						data.Add(record);
					}
				}
			}

			return data;
		}

		private static string GetCarWashIncomeRequest(DateTime dateStart, DateTime dateEnd) => @$"
DECLARE @StartDate DATETIME = '{dateStart.ToString("yyyyMMdd")}'
DECLARE @EndDate DATETIME = '{dateEnd.ToString("yyyyMMdd")}'


SELECT
REPLACE(P.Code, 'О', '0') CrmCode, pt.[Name] PayTypeName, SUM(O.Amount) SumAmount, SUM([Total]) SumTotal, P.EssStationId	  
  FROM (SELECT *, 
  (CASE WHEN Pos = 11 THEN 92 ELSE Pos END) Pos2  FROM 
  [ARC-Nmlos].[dbo].[Order]
  ) O
  INNER JOIN [ARC-Nmlos].[dbo].Pos P ON O.Pos2 = P.Oid
  INNER JOIN [ARC-Nmlos].[dbo].[Payment] pay on pay.[Order] = o.oid  
  INNER JOIN [ARC-Nmlos].[dbo].PaymentType pt on pt.Oid = pay.PaymentType
  WHERE O.[Time] >= @StartDate AND O.[Time] < @EndDate 
  AND O.Origin = 7  
  GROUP BY 
  P.EssStationId,
  p.Code,
  pay.PaymentType,
  pt.Name
  order by p.Code";
	}
}
