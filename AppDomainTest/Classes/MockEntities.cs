using System;

namespace AppDomainTest.Classes
{
	public class MockEntities
	{
		public string MyString { get; set; }

		public MockEntities()
		{
			MyString = "Mock";
		}

		public void Print()
		{
			Console.WriteLine("Hello from " + MyString);
		}
	}
}