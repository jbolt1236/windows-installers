using System;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration
{
	public class JavaConfigurationTests
	{
		private readonly string _machineVariable = @"C:\Java\Machine";
		private readonly string _userVariable = @"C:\Java\User";
		private readonly string _processVariable = @"C:\Java\Process";
		private readonly string _registryJdk64 = @"C:\Java\RegistryJdk64";
		private readonly string _registryJre64 = @"C:\Java\RegistryJre64";

		private void AssertJavaHome(string expext, Func<MockJavaEnvironmentStateProvider, IJavaEnvironmentStateProvider> setup)
		{
			var javaConfiguration = new JavaConfiguration(setup(new MockJavaEnvironmentStateProvider()));
			javaConfiguration.JavaHomeCanonical.Should().Be(expext);
		}

		[Fact] void MachineHomeIsSeen() => AssertJavaHome(_machineVariable, m=> m.JavaHomeMachineVariable(_machineVariable));

		[Fact] void UserHomeIsSeen()=> AssertJavaHome(_userVariable, m=> m.JavaHomeUserVariable(_userVariable));

		[Fact] void ProcessHomeIsSeen()=> AssertJavaHome(_processVariable, m=> m.JavaHomeProcessVariable(_processVariable));
		
		[Fact] void RegistryJdk64HomeIsSeen() => AssertJavaHome(_registryJdk64, m=> m.JdkRegistry64(_registryJdk64));
		
		[Fact] void RegistryJre64HomeIsSeen() => AssertJavaHome(_registryJre64, m=> m.JreRegistry64(_registryJre64));
		
		[Fact] void ProcessBeatsUser() => AssertJavaHome(_processVariable, m => m
			.JavaHomeProcessVariable(_processVariable)
			.JavaHomeUserVariable(_userVariable)
		);

		[Fact] void UserBeatsMachine() => AssertJavaHome(_userVariable, m => m
			.JavaHomeUserVariable(_userVariable)
			.JavaHomeMachineVariable(_machineVariable)
		);
		
		[Fact] void MachineBeatsRegistry() => AssertJavaHome(_machineVariable, m => m
			.JavaHomeMachineVariable(_machineVariable)
			.JdkRegistry64(_registryJdk64)
		);
		
		[Fact] void Jdk64BeatsJre64() => AssertJavaHome(_registryJdk64, m => m
			.JdkRegistry64(_registryJdk64)
			.JreRegistry64(_registryJre64)
		);
	
	}
}
