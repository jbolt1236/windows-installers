using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class EnsureEnvironmentVariablesTask : ElasticsearchInstallationTask
	{
		public EnsureEnvironmentVariablesTask(string[] args, ISession session) : base(args, session) { }

		public EnsureEnvironmentVariablesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			this.Session.Log($"Existing Version Installed: {this.InstallationModel.NoticeModel.ExistingVersionInstalled}");
			this.Session.Log($"Current Version: {this.InstallationModel.NoticeModel.CurrentVersion}");
			this.Session.Log($"Existing Version: {this.InstallationModel.NoticeModel.ExistingVersion}");
			this.Session.Log($"Session Installing: {this.Session.IsInstalling}");
			this.Session.Log($"Session Uninstalling: {this.Session.IsUninstalling}");
			this.Session.Log($"Session Rollback: {this.Session.IsRollback}");
			this.Session.Log($"Session Upgrading: {this.Session.IsUpgrading}");

			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;

			var esConfigEnvVar = this.InstallationModel.NoticeModel.CurrentVersion.Major == 5
				? "ES_CONFIG"
				: "CONF_DIR";

			var esConfig = Environment.GetEnvironmentVariable(esConfigEnvVar, EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(esConfig))
			{
				this.Session.Log($"{nameof(EnsureEnvironmentVariablesTask)}: Setting {esConfigEnvVar}");
				Environment.SetEnvironmentVariable(esConfigEnvVar, configDirectory, EnvironmentVariableTarget.Machine);
			}

			var esHome = Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(esHome))
			{
				this.Session.Log($"{nameof(EnsureEnvironmentVariablesTask)}: Setting ES_HOME");
				this.InstallationModel.ElasticsearchEnvironmentConfiguration.SetEsHomeEnvironmentVariable(installDirectory);
			}

			return true;
		}
	}
}