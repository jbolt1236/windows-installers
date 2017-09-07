using System.Security.AccessControl;
using Elastic.Installer.Domain.Configuration;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks
{
	public class StoreTemporaryStateTaskTests : InstallationModelTestBase
	{
		private IServiceStateProvider NoService { get; } = new NoopServiceStateProvider();
		private IServiceStateProvider InstalledAndRunning { get; } = new NoopServiceStateProvider {SeesService = true, Running = true};
		private IServiceStateProvider InstalledNotRunning { get; } = new NoopServiceStateProvider { SeesService = true };

		[Fact] void ServiceNotInstalled() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, NoService), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					AssertCreationOfDirectoryAndFiles(t, m, state);
					state.Exists(nameof(state.SeesService)).Should().BeTrue();
					state.SeesService.Should().BeFalse();
					state.Exists(nameof(state.ServiceRunning)).Should().BeTrue();
					state.ServiceRunning.Should().BeFalse();

				}
			);
		
		[Fact] void ServiceInstalledAndRunning() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledAndRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					AssertCreationOfDirectoryAndFiles(t, m, state);
					state.Exists(nameof(state.SeesService)).Should().BeTrue();
					state.SeesService.Should().BeTrue();
					state.Exists(nameof(state.ServiceRunning)).Should().BeTrue();
					state.ServiceRunning.Should().BeTrue();

				}
			);
		
		[Fact] void ServiceInstalledNotRunning() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					AssertCreationOfDirectoryAndFiles(t, m, state);
					state.Exists(nameof(state.SeesService)).Should().BeTrue();
					state.SeesService.Should().BeTrue();
					state.Exists(nameof(state.ServiceRunning)).Should().BeTrue();
					state.ServiceRunning.Should().BeFalse();
				}
			);
		
		[Fact] void DoesNotStoreOldConfigVariableIfNotSet() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					state.Exists(nameof(state.OldConfigDirectoryMachineVariable)).Should().BeFalse();
				}
			);

		private readonly string _oldConfigLocation = @"C:\OldConfigLocation";
		[Fact] void StoresOldConfigVariableWhenSet() => WithValidPreflightChecks(s => s
				.Elasticsearch(e=>e.EsConfigMachineVariableOld(_oldConfigLocation))
			)
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					state.Exists(nameof(state.OldConfigDirectoryMachineVariable)).Should().BeTrue();
					state.OldConfigDirectoryMachineVariable.Should().Be(_oldConfigLocation);
				}
			);
		
		[Fact] void DoesNotStoreNewConfigVariableIfNotSet() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					state.Exists(nameof(state.NewConfigDirectoryMachineVariable)).Should().BeFalse();
				}
			);

		private readonly string _newConfigLocation = @"C:\NewConfigLocation";
		[Fact] void StoresNewConfigVariableWhenSet() => WithValidPreflightChecks(s => s
				.Elasticsearch(e=>e.EsConfigMachineVariable(_newConfigLocation))
			)
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					state.Exists(nameof(state.NewConfigDirectoryMachineVariable)).Should().BeTrue();
					state.NewConfigDirectoryMachineVariable.Should().Be(_newConfigLocation);
				}
			);
		
		[Fact] void DoesNotStoreHomeVariableIfNotSet() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					state.Exists(nameof(state.HomeDirectoryMachineVariable)).Should().BeFalse();
				}
			);

		private readonly string _homeLocation = @"C:\Elasticsearch";
		[Fact] void StoresHomeVariableWhenSet() => WithValidPreflightChecks(s => s
				.Elasticsearch(e=>e.EsHomeMachineVariable(_homeLocation))
			)
			.AssertTask(
				(m, s, fs) => new StoreTemporaryStateTask(m, s, fs, InstalledNotRunning), 
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					state.Exists(nameof(state.HomeDirectoryMachineVariable)).Should().BeTrue();
					state.HomeDirectoryMachineVariable.Should().Be(_homeLocation);
				}
			);

		private static void AssertCreationOfDirectoryAndFiles(InstallationModelTester t, ElasticsearchInstallationModel m,
			TempDirectoryStateConfiguration state)
		{
			t.FileSystem.Directory.Exists(m.TempDirectoryConfiguration.TempProductInstallationDirectory)
				.Should().BeTrue();
			t.FileSystem.Directory.Exists(state.StateDirectory).Should().BeTrue();
			t.FileSystem.DirectoryInfo.FromDirectoryName(state.StateDirectory).EnumerateFiles().Should().NotBeEmpty();
		}
	}
}
