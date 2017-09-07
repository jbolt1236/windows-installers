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

		[Fact] void CopiesExistingConfigDirectory() => WithValidPreflightChecks(s => s
				.FileSystem(fs =>
				{
					fs.AddDirectory(LocationsModel.DefaultConfigDirectory);
					var subFolder = Path.Combine(LocationsModel.DefaultConfigDirectory, "subFolder");
					fs.AddDirectory(subFolder);
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData("hello world"));
					fs.AddFile(Path.Combine(subFolder, "someconfig.txt"), new MockFileData("hello space"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) =>
				{
					var productTempDirectory = m.TempDirectoryConfiguration.TempProductInstallationDirectory;
					var configTempDirectory = fs.Path.Combine(productTempDirectory, "config");
					var file = fs.Path.Combine(configTempDirectory, "elasticsearch.yml");
					fs.Directory.Exists(productTempDirectory).Should().BeFalse();
					fs.Directory.Exists(configTempDirectory).Should().BeFalse();
					fs.File.Exists(file).Should().BeFalse();
					return new PreserveInstallTask(m, s, fs);
				},
				(m, t) =>
				{
					var fs = t.FileSystem;
					var productTempDirectory = m.TempDirectoryConfiguration.TempProductInstallationDirectory;
					var configTempDirectory = fs.Path.Combine(productTempDirectory, "config");
					var subFolder = fs.Path.Combine(configTempDirectory, "subFolder");
					var file = fs.Path.Combine(configTempDirectory, "elasticsearch.yml");
					var subFolderFile = fs.Path.Combine(subFolder, "someconfig.txt");
					fs.Directory.Exists(productTempDirectory).Should().BeTrue();
					fs.Directory.Exists(configTempDirectory).Should().BeTrue();
					fs.Directory.Exists(subFolder).Should().BeTrue();
					fs.File.Exists(file).Should().BeTrue();
					fs.File.ReadAllText(file).Should().Be("hello world");
					fs.File.Exists(subFolderFile).Should().BeTrue();
					fs.File.ReadAllText(subFolderFile).Should().Be("hello space");
				}
			);
		
		[Fact] void MovesPluginFolder() => WithValidPreflightChecks(s => s
				.FileSystem(fs =>
				{
					var pluginFolder = Path.Combine(LocationsModel.DefaultProductInstallationDirectory, "plugins");
					var pluginSubFolder= Path.Combine(pluginFolder, "x-pack");
					fs.AddDirectory(pluginFolder);
					fs.AddDirectory(pluginSubFolder);
					fs.AddFile(Path.Combine(pluginFolder, "elasticsearch.yml"), new MockFileData("hello world"));
					fs.AddFile(Path.Combine(pluginSubFolder, "someconfig.txt"), new MockFileData("hello space"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) =>
				{
					var tempFolder = m.TempDirectoryConfiguration.TempProductInstallationDirectory;
					var pluginsTempFolder = fs.Path.Combine(tempFolder, "plugins");
					var file = fs.Path.Combine(pluginsTempFolder, "elasticsearch.yml");
					fs.Directory.Exists(tempFolder).Should().BeFalse();
					fs.Directory.Exists(pluginsTempFolder).Should().BeFalse();
					fs.File.Exists(file).Should().BeFalse();
					return new PreserveInstallTask(m, s, fs);
				},
				(m, t) =>
				{
					var fs = t.FileSystem;
					var tempFolder = m.TempDirectoryConfiguration.TempProductInstallationDirectory;
					var pluginsTempFolder = fs.Path.Combine(tempFolder, "plugins");
					var subFolder = fs.Path.Combine(pluginsTempFolder, "x-pack");
					var file = fs.Path.Combine(pluginsTempFolder, "elasticsearch.yml");
					var subFolderFile = fs.Path.Combine(subFolder, "someconfig.txt");
					fs.Directory.Exists(tempFolder).Should().BeTrue();
					fs.Directory.Exists(pluginsTempFolder).Should().BeTrue();
					fs.Directory.Exists(subFolder).Should().BeTrue();
					fs.File.Exists(file).Should().BeTrue();
					fs.File.ReadAllText(file).Should().Be("hello world");
					fs.File.Exists(subFolderFile).Should().BeTrue();
					fs.File.ReadAllText(subFolderFile).Should().Be("hello space");
					
					var pluginsFolder = Path.Combine(LocationsModel.DefaultProductInstallationDirectory, "plugins");
					var xPackFolder = Path.Combine(pluginsFolder, "x-pack");
					fs.Directory.Exists(pluginsFolder).Should().BeTrue();
					//moved out during installation in order to accomodate a fast restore
					fs.Directory.Exists(xPackFolder).Should().BeFalse();
				}
			);
	}
}

