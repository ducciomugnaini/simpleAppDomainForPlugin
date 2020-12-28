using System;

namespace AppDomainTest.Classes
{
	public class Parameter :MarshalByRefObject
	{
		public int Value { get; set; }
		public string Description { get; set; }
	}
}