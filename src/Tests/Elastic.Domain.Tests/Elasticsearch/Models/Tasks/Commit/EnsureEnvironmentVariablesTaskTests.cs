using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.CompilerServices;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Commit;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Commit
{
	public class EnsureEnvironmentVariablesTaskTests : InstallationModelTestBase
	{
		[Fact] void SetsEnvironmentVariablesIfTheyAreNull() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					m.ElasticsearchEnvironmentConfiguration.SetEsConfigEnvironmentVariable(null);
					m.ElasticsearchEnvironmentConfiguration.SetOldEsConfigEnvironmentVariable(null);
					m.ElasticsearchEnvironmentConfiguration.SetEsHomeEnvironmentVariable(null);
					return new EnsureEnvironmentVariablesTask(m, s, fs);
				},
				(m, t) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.HomeDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
					env.NewConfigDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
				}
			);
		
		[Fact] void LeavesExistingHomeVariableAlone() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					m.ElasticsearchEnvironmentConfiguration.SetEsHomeEnvironmentVariable("foo");
					return new EnsureEnvironmentVariablesTask(m, s, fs);
				},
				(m, t) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.HomeDirectoryMachineVariable.Should().Be("foo");
				}
			);
		[Fact] void LeavesExistingNewConfigAlone() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					m.ElasticsearchEnvironmentConfiguration.SetEsConfigEnvironmentVariable("foo");
					return new EnsureEnvironmentVariablesTask(m, s, fs);
				},
				(m, t) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.NewConfigDirectoryMachineVariable.Should().Be("foo");
				}
			);
		[Fact] void LeavesExistingOldConfigAlone() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					m.ElasticsearchEnvironmentConfiguration.SetOldEsConfigEnvironmentVariable("foo");
					return new EnsureEnvironmentVariablesTask(m, s, fs);
				},
				(m, t) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.OldConfigDirectoryMachineVariable.Should().Be("foo");
				}
			);
	}
}

