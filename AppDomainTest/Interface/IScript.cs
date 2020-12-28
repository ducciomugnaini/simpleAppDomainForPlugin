using System;
using AppDomainTest.Classes;

namespace AppDomainTest.Interface
{
	public interface IScript
	{
		void Configure(Parameter p1, Parameter p2);
		Result Execute();
	}
}