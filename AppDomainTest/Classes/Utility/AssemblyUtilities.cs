using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AppDomainTest.Classes.Utility
{
	public class AssemblyUtilities
	{
		public static IEnumerable<Assembly> GetSystemAssemblies(
			string[] assemblyPrefixes = null)
		{
			return ((IEnumerable<Assembly>) AppDomain.CurrentDomain.GetAssemblies());
		}

		public static IEnumerable<Assembly> GetSystemAssemblies(string assemblyPrefix)
		{
			string[] assemblyPrefixes;
			if (!string.IsNullOrEmpty(assemblyPrefix))
				assemblyPrefixes = new string[1]{ assemblyPrefix };
			else
				assemblyPrefixes = (string[]) null;
			return GetSystemAssemblies(assemblyPrefixes);
		}
	}
}