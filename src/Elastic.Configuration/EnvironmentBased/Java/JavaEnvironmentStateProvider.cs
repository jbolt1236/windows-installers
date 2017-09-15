using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using static Microsoft.Win32.RegistryView;

namespace Elastic.Configuration.EnvironmentBased.Java
{

	public interface IJavaEnvironmentStateProvider
	{
		string JavaHomeUserVariable { get; }
		string JavaHomeMachineVariable { get; }
		string JavaHomeProcessVariable { get; }
		
		string JdkRegistry64 { get; }
		string JdkRegistry32 { get; }
		string JreRegistry64  { get; }
		string JreRegistry32 { get; }
		bool ReadJavaVersionInformation(string javaHomeCanonical, out List<string> consoleOut);
	}

	public class JavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private const string JreRootPath = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
		private const string JdkRootPath = "SOFTWARE\\JavaSoft\\Java Development Kit";
		private const string JavaHome = "JAVA_HOME";

		public string JavaHomeProcessVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Process);
		public string JavaHomeUserVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.User);
		public string JavaHomeMachineVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Machine);

		public string JdkRegistry64 => RegistrySubKey(Registry64, JdkRootPath);
		public string JdkRegistry32 => RegistrySubKey(Registry32, JdkRootPath);
		public string JreRegistry64 => RegistrySubKey(Registry64, JreRootPath); 
		public string JreRegistry32 => RegistrySubKey(Registry32, JreRootPath);
		
		private static string RegistrySubKey(RegistryView view, string subKey)
		{
			if (string.IsNullOrWhiteSpace(subKey)) return null;
			
			var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
			string version = null;
			using (var key = registry.OpenSubKey(subKey))
				version = key?.GetValue("CurrentVersion") as string;
			if (version == null) return null;

			using (var key = registry.OpenSubKey(subKey + "\\" + version))
				return key?.GetValue("JavaHome") as string;
		}

		public bool ReadJavaVersionInformation(string javaHomeCanonical, out List<string> consoleOut)
		{
			var localConsoleOut = new List<string>();
			consoleOut = localConsoleOut;
			
			var startInfo = new ProcessStartInfo
			{
				FileName = javaHomeCanonical,
				Arguments = "-version",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				ErrorDialog = false,
				CreateNoWindow = true,
				UseShellExecute = false
			};
			var process = new Process { StartInfo = startInfo };
			process.OutputDataReceived += (s, a) => localConsoleOut.Add(a.Data);;
			var errors = false;
			process.ErrorDataReceived += (s, a) =>
			{
				if (string.IsNullOrEmpty(a.Data)) return;
				errors = true;
			};
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			var exitCode = process.ExitCode;
			return exitCode <= 0 && !errors;
		}
	}
}