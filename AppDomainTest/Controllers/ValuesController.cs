using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Http;
using AppDomainTest.Classes;
using AppDomainTest.Interface;

namespace AppDomainTest.Controllers
{
	public class ValuesController : ApiController
	{

		[HttpGet]
		[Route("api/AssemblyTest")]
		public IHttpActionResult AssemblyTest()
		{
			try
			{
				Assembly runtimeAssembly;

				//using (var codeProvider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } }))
				using (var codeProvider = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider())
				{
					var parameters = new CompilerParameters
					{
						GenerateExecutable = false, // Create a dll
						GenerateInMemory = true, // Create it in memory
						WarningLevel = 3, // Default warning level
						CompilerOptions = "/optimize", // Optimize code
						TreatWarningsAsErrors = false, // Better be false to avoid break in warnings
						IncludeDebugInformation = false
					};

					// secondo me nn gliene frega niente del path
					//var binariesPath = AssemblyUtilities.AssemblyDirectory + Path.DirectorySeparatorChar;
					var referencedAssemblies = new HashSet<string>
					{
						"mscorlib.dll",
						"System.dll",
						"System.Core.dll",
						"System.Xml.dll",
						"System.ComponentModel.DataAnnotations.dll",
						"System.Linq.dll",
						"System.Linq.Expressions.dll",
						"System.Data.dll",
						"Microsoft.CSharp.dll",

						"Newtonsoft.Json.dll",
						"C:\\Develop\\NET\\Lab\\AppDomainTest\\AppDomainTest\\AppDomainTest\\bin\\AppDomainTest.dll", // <- Pcube alias
						"C:\\external_dll\\TestLib.dll"

						/*$"{binariesPath}EntityFramework.dll",
						$"{binariesPath}EntityFramework.IndexingExtensions.dll",
						$"{binariesPath}Pcube.Data.dll",
						$"{binariesPath}AdaptiveLINQ.dll"*/
					};

					parameters.ReferencedAssemblies.AddRange(referencedAssemblies.ToArray());
					
					parameters.OutputAssembly = System.IO.Path.GetTempPath()+ Guid.NewGuid().ToString() + ".p3s"; // "C:\\dynamic_assembly_temp\\DynamicAssembly_" + Guid.NewGuid().ToString().Substring(0, 5) + ".dll";
					parameters.GenerateInMemory = false;
#if DEBUG
					/*parameters.GenerateInMemory = false;
					parameters.TempFiles = new TempFileCollection(Environment.GetEnvironmentVariable("TEMP"), true);
					parameters.IncludeDebugInformation = true;*/
#endif

					var results = codeProvider.CompileAssemblyFromSource(parameters, GetTemplate());
					if (results.Errors.HasErrors)
					{
						var enumerator = results.Errors.GetEnumerator();
						var compileError = new StringBuilder();
						while (enumerator.MoveNext())
						{
							var item = enumerator.Current as CompilerError;
							if (item != null) compileError.AppendLine(item.ErrorText);
						}

						throw new PlatformNotSupportedException("Error on generating runtime assembly dll",
							new Exception(compileError.ToString()));
					}

					runtimeAssembly = results.CompiledAssembly;
					
					// -- creare un nuovo appDomain

					// Construct and initialize settings for a second AppDomain.
					AppDomainSetup domainSetup = new AppDomainSetup()
					{
						ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\bin",
						ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
						ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
						LoaderOptimization = LoaderOptimization.MultiDomainHost,
						
						// PrivateBinPath = "C:\\external_dll; C:\\dynamic_assembly_temp" // <- inutile =( => impostare handler nel AppDomain secondario
					};

					// Create proxy to add assemblies to a specific appDomain
					// https://stackoverflow.com/questions/17225276/create-custom-appdomain-and-add-assemblies-to-it

					AppDomain myAppDomain = AppDomain.CreateDomain("myDomain", null, domainSetup);
					//myAppDomain.Load(runtimeAssembly.GetName());
					
					//Create the loader (a proxy).
					var assemblyProxy = (SimpleAssemblyProxy) myAppDomain.CreateInstanceAndUnwrap(
						typeof(SimpleAssemblyProxy).Assembly.FullName, typeof(SimpleAssemblyProxy).FullName);

					//assemblyLoader.SetSystemAssemblyList(SYSTEM_ASSEMBLIES);
					assemblyProxy.AttachAssemblyResolveHandler();
					
					assemblyProxy.PrintAllAssemblyLoaded();
					PrintCurrentAppDomainLoadedAssemblies("AppDomain.Current");
					
					var pathList = new List<string>{"C:\\external_dll"};
					
					var result = assemblyProxy.ExecuteScript(runtimeAssembly.Location, pathList);

					var code = result.Code;
					var description = result.Description;
					
					Console.WriteLine("Code -> 			"+code);
					Console.WriteLine("Description -> 	"+description);
					
					//Do whatever you want to do.

					//Finally unload the AppDomain.
					AppDomain.Unload(myAppDomain);
					
					//List the assemblies in the current application domain.
					PrintCurrentAppDomainLoadedAssemblies("AppDomain.Current");
				}

				return Ok();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		private void PrintCurrentAppDomainLoadedAssemblies(string title = null)
		{
			//List the assemblies in the current application domain.
			Console.WriteLine("-------------------------------------------------------------------------------------- "+title);
			Console.WriteLine("List of assemblies loaded in current appdomain:");
			foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
			Console.WriteLine(assem.ToString());
			Console.WriteLine("--------------------------------------------------------------------------------------");
		}

		public class SimpleAssemblyProxy : MarshalByRefObject
		{
			public List<Assembly> ScriptAssemblies = new List<Assembly>();

			public Result ExecuteByNameOfRuntimeAssembly(string typeName)
			{
				IScript script = (IScript) AppDomain.CurrentDomain.CreateInstanceAndUnwrap(
					typeName, typeName);
				return script.Execute();
			}
			
			/*public Result ExecuteScript(Assembly runtimeAssembly)
			{
				var myAssembly = AppDomain.CurrentDomain.Load(runtimeAssembly.GetName());
				IScript script = (IScript) myAssembly.CreateInstance("AppDomainTest.Classes.DynamicClass");
				return script.Execute();
			}*/
			
			// tutto quelle che restituisce il proxy deve essere MBRO
			public Result ExecuteScript(string assemblyLocation, IEnumerable<String> pathArray)
			{
				string path = pathArray.First();
				foreach (string dll in Directory.GetFiles(path, "*.dll"))
					ScriptAssemblies.Add(Assembly.LoadFile(dll));

				var assembly = LoadFile(assemblyLocation);
				IScript script = (IScript) assembly.CreateInstance("AppDomainTest.Classes.DynamicClass");

				return script.Execute();
			}
			
			// pubblico perchè non restituisce niente e comunica soltanto attraverso il proxy
			public void LoadFileFromPath(string assemblyPath)
			{
				// gli indiani sanno tutto
				// https://www.c-sharpcorner.com/blogs/asp-net-assemblyloadfrom-vs-assemblyloadfile1
				ValidatePath(assemblyPath);
				Assembly.LoadFrom(assemblyPath);
			}
			
			// privato e non richiamabile dal controller perchè Assembly non è MBRO 
			// ma richiamabile internamente perchè interni al AppDomain corrente
			private Assembly LoadFile(string assemblyPath)
			{
				ValidatePath(assemblyPath);
				return Assembly.LoadFile(assemblyPath);
			}
			
			public void LoadAssemblyName(string assemplyFullName)
			{
				Assembly.Load(assemplyFullName);
			}
			
			public void Load(string path)
			{
				ValidatePath(path);

				Assembly.Load(path);
			}

			public void LoadFrom(string path)
			{
				ValidatePath(path);

				Assembly.LoadFrom(path);
			}

			public void ValidatePath(string path)
			{
				if (path == null) throw new ArgumentNullException("path");
				if (!System.IO.File.Exists(path))
					throw new ArgumentException(String.Format("path \"{0}\" does not exist", path));
			}
			
			public void PrintAllAssemblyLoaded()
			{
				//List the assemblies in the current application domain.
				Console.WriteLine("-------------------------------------------------------------------------------------- SECONDARY AppDomain");
				Console.WriteLine("List of assemblies loaded in current appdomain:");
				foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
					Console.WriteLine(assem.ToString());
				Console.WriteLine("--------------------------------------------------------------------------------------");
			}

			public void AttachAssemblyResolveHandler()
			{
				AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
				{
					try
					{
						var systemAssembly =  ScriptAssemblies.FirstOrDefault(a => a.FullName.Equals(args.Name));
						
						// todo cercare la dll giusta in base al nome in arg in
						// - bin pcube
						// - cartelle custom passate in configurazione => aggiunte dentr
						
						/*if(systemAssembly == null)
							return Assembly.LoadFrom("C:\\external_dll\\TestLib.dll");*/

						return systemAssembly;
					}
					catch (Exception ex)
					{
						return null;
					}
				};
			}

			public void LoadAssemblyByName(AssemblyName getName)
			{
				Assembly.Load(getName);
			}
		}


		#region utils

		private void PrintAllAssemblyLoaded(AppDomain appDomain)
		{
			//List the assemblies in the current application domain.
			Console.WriteLine("--------------------------------------------------------------------------------------");
			Console.WriteLine("List of assemblies loaded in current appdomain:");
			foreach (Assembly assem in appDomain.GetAssemblies())
				Console.WriteLine(assem.ToString());
			Console.WriteLine("--------------------------------------------------------------------------------------");
		}

		private static string GetTemplate()
		{
			return @"using System;
using System.Collections.Generic;
using AppDomainTest.Interface;
using Newtonsoft.Json;
using TestLib;

namespace AppDomainTest.Classes
{
	internal class Account
	{
		public string Email { get; set; }
		public bool Active { get; set; }
		public DateTime CreatedDate { get; set; }
		public IList<string> Roles { get; set; }
	}

	public class DynamicClass :  IScript
	{
		public Parameter par_1 { get; set; }
		public Parameter par_2 { get; set; }

		~DynamicClass(){
			Console.WriteLine(Char.ToString('D'));
		}

		public void Configure(Parameter p1, Parameter p2)
		{
			par_1 = p1;
			par_2 = p2;
		}

		public Result Execute()
		{
			Account account = new Account
			{
				Email = Char.ToString('A'),
            Active = true,
            CreatedDate = new DateTime(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Roles = new List<string>
            {
                Char.ToString('B'),
                Char.ToString('C')
            }
        };
        string json = JsonConvert.SerializeObject(account, Formatting.Indented);

		var mock = new MockEntities();
		mock.Print();

        Console.WriteLine(json);
		var res = Calculator.Sum(1, 2);
			
        return new Result
        {
            Code = res,
            Description = Char.ToString('S')
        };
    }
}
}";
		}


		/*// GET api/values
		public IEnumerable<string> Get()
		{
		    return new string[] { "value1", "value2" };
		}

		// GET api/values/5
		public string Get(int id)
		{
		    return "value";
		}

		// POST api/values
		public void Post([FromBody] string value)
		{
		}

		// PUT api/values/5
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/values/5
		public void Delete(int id)
		{
		}*/
	}

	#endregion
}