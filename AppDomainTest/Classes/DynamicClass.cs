using System;
using System.Collections.Generic;
using AppDomainTest.Interface;
using Newtonsoft.Json;

namespace AppDomainTest.Classes
{
	internal class Account
	{
		public string Email { get; set; }
		public bool Active { get; set; }
		public DateTime CreatedDate { get; set; }
		public IList<string> Roles { get; set; }
	}

	public class DynamicClass : IScript
	{
		public Parameter par_1 { get; set; }
		public Parameter par_2 { get; set; }

		public void Configure(Parameter p1, Parameter p2)
		{
			par_1 = p1;
			par_2 = p2;
		}

		public Result Execute()
		{
			Account account = new Account
			{
				Email = "james@example.com",
				Active = true,
				CreatedDate = new DateTime(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
				Roles = new List<string>
				{
					"User",
					"Admin"
				}
			};
			string json = JsonConvert.SerializeObject(account, Formatting.Indented);

			Console.WriteLine(json);
			
			return new Result
			{
				Code = 200,
				Description = "Success"
			};
		}
	}
}