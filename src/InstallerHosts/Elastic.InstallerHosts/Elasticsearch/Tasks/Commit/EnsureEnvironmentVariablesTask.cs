using System.IO.Abstractions;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Commit
{
	public class EnsureEnvironmentVariablesTask : ElasticsearchInstallationTaskBase
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

			var env = this.InstallationModel.ElasticsearchEnvironmentConfiguration.StateProvider;
			EnsureConfigVariable(env);
			EnsureHomeVariable(env);
			return true;
		}

		private void EnsureHomeVariable(IElasticsearchEnvironmentStateProvider env)
		{
			var esHome = env.HomeDirectoryMachineVariable;
			if (!string.IsNullOrEmpty(esHome)) return;
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			this.Session.Log($"{nameof(EnsureEnvironmentVariablesTask)}: Setting ES_HOME");
			this.InstallationModel.ElasticsearchEnvironmentConfiguration.SetEsHomeEnvironmentVariable(installDirectory);
		}

		private void EnsureConfigVariable(IElasticsearchEnvironmentStateProvider env)
		{
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var envConfig = this.InstallationModel.ElasticsearchEnvironmentConfiguration;
			if (this.InstallationModel.NoticeModel.CurrentVersion.Major == 5)
			{
				var v = env.NewConfigDirectoryMachineVariable;
				this.Session.Log($"{nameof(EnsureEnvironmentVariablesTask)}: Setting CONF_DIR");
				if (string.IsNullOrWhiteSpace(v)) envConfig.SetEsConfigEnvironmentVariable(configDirectory);
			}
			else
			{
				var v = env.OldConfigDirectoryMachineVariable;
				this.Session.Log($"{nameof(EnsureEnvironmentVariablesTask)}: Setting ES_CONF");
				if (string.IsNullOrWhiteSpace(v)) envConfig.SetOldEsConfigEnvironmentVariable(configDirectory);
			}
		}
	}
}