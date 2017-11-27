using System.IO;
using System.IO.Abstractions;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Uninstall;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Rollback
{
	public class RollbackDirectoriesTask : DeleteDirectoriesTask
	{
		public RollbackDirectoriesTask(string[] args, ISession session)
			: base(args, session) { }

		public RollbackDirectoriesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			if (this.InstallationModel.NoticeModel.ExistingVersionInstalled)
			{
				RestoreConfigDirectory();
				RestorePluginsDirectory();
				return true;
			}

			var fs = this.FileSystem;

			if (this.Session.IsInstalling)
			{
				var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
				if (fs.Directory.Exists(configDirectory))
				{
					this.Session.SendActionStart(1000, ActionName, "Removing data, logs, and config directory",
						"Removing directories: [1]");

					var yamlConfiguration = ElasticsearchYamlConfiguration.FromFolder(configDirectory, fs);
					var dataDirectory = yamlConfiguration?.Settings?.DataPath ?? this.InstallationModel.LocationsModel.DataDirectory;
					var logsDirectory = yamlConfiguration?.Settings?.LogsPath ?? this.InstallationModel.LocationsModel.LogsDirectory;

					if (fs.Directory.Exists(dataDirectory))
						this.DeleteDirectory(dataDirectory);
					else this.Session.Log($"Data Directory does not exist, skipping {dataDirectory}");

					if (fs.Directory.Exists(logsDirectory))
					{
						DumpElasticsearchLogOnRollback(logsDirectory);
						this.DeleteDirectory(logsDirectory);
					}
					else this.Session.Log($"Logs Directory does not exist, skipping {logsDirectory}");

					if (fs.Directory.Exists(configDirectory))
						this.DeleteDirectory(configDirectory);
					else this.Session.Log($"Config Directory does not exist, skipping {configDirectory}");

					this.Session.SendProgress(1000, "data, logs, and config directories removed");
					this.Session.Log("data, logs, and config directories removed");
				}
			}

			return base.ExecuteTask();
		}

		private void RestoreConfigDirectory()
		{
			var tempconfigDirectory = this.FileSystem.Path.Combine(this.TempProductInstallationDirectory, "config");
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			if (!this.FileSystem.Directory.Exists(tempconfigDirectory)) return;

			this.Session.Log("Restoring config directory");
			this.FileSystem.Directory.Delete(configDirectory, true);
			this.CopyDirectory(tempconfigDirectory, configDirectory);
			this.FileSystem.Directory.Delete(tempconfigDirectory, true);
		}

		private void RestorePluginsDirectory()
		{
			var fs = this.FileSystem;
			var path = fs.Path;
			var pluginsTempDirectory = path.Combine(this.TempProductInstallationDirectory, "plugins");
			var pluginsDirectory = path.Combine(this.InstallationModel.LocationsModel.InstallDir, "plugins");

			if (!fs.Directory.Exists(pluginsTempDirectory)) return;

			this.Session.Log("Restoring plugins directory");

			// delete any plugins that might have been installed
			var installDir = fs.DirectoryInfo.FromDirectoryName(pluginsDirectory);
			foreach (var file in installDir.GetFiles())
				fs.File.Delete(file.FullName);
			foreach (var dir in installDir.GetDirectories())
				fs.Directory.Delete(dir.FullName, true);

			// restore old plugins
			var directory = fs.DirectoryInfo.FromDirectoryName(pluginsTempDirectory);
			foreach (var file in directory.GetFiles())
			{
				fs.File.Copy(file.FullName, path.Combine(directory.FullName, file.Name));
				fs.File.Delete(file.FullName);
			}
			foreach (var dir in directory.GetDirectories())
			{
				CopyDirectory(dir, fs.Directory.CreateDirectory(path.Combine(directory.FullName, dir.Name)));
				fs.Directory.Delete(dir.FullName, true);
			}

			this.FileSystem.Directory.Delete(pluginsTempDirectory, true);
		}

		private void DumpElasticsearchLogOnRollback(string logsDirectory)
		{
			if (!this.Session.IsRollback) return;
			var clusterName = this.InstallationModel.ConfigurationModel.ClusterName;
			var logFile = Path.Combine(logsDirectory, clusterName) + ".log";
			if (this.FileSystem.File.Exists(logFile))
			{
				this.Session.Log($"Elasticsearch log file found: {logFile}");
				var log = this.FileSystem.File.ReadAllText(logFile);
				this.Session.Log(log);
			}
			else
				this.Session.Log($"Elasticsearch log file not found: {logFile}");
		}

		private void DeleteDirectory(string directory)
		{
			this.Session.Log($"Attemping to delete {directory}");
			this.FileSystem.Directory.Delete(directory, true);
			this.Session.SendProgress(1000, $"{directory} removed");
		}
	}
}