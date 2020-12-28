using System;

namespace AppDomainTest.Classes
{
	public class Result : MarshalByRefObject
	{
		public int Code { get; set; }
		public string Description { get; set; }
	}
}