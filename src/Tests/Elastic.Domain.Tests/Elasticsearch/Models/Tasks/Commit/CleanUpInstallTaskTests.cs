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
	public class CleanUpInstallTaskTests : InstallationModelTestBase
	{
		[Fact] void CleansProductInstallTempDirectory() => WithValidPreflightChecks()
			//call StoreTemporaryStateTask before PreserveInstallTask like the installer does
			.ExecuteTask((m, s, fs) => new StoreTemporaryStateTask(m, s, fs, new NoopServiceStateProvider()))
			.AssertTask(
				(m, s, fs) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					var dir = state.StateDirectory;
					var file = state.FilePath(nameof(state.SeesService));
					fs.Directory.Exists(dir).Should().BeTrue();
					fs.File.Exists(file).Should().BeTrue();
					return new CleanupInstallTask(m, s, fs);
				},
				(m, t) =>
				{
					var state = m.TempDirectoryConfiguration.State;
					var dir = state.StateDirectory;
					var file = state.FilePath(nameof(state.SeesService));
					var fs = t.FileSystem;
					fs.Directory.Exists(dir).Should().BeFalse();
					fs.File.Exists(file).Should().BeFalse();
				}
			);
	}
}

