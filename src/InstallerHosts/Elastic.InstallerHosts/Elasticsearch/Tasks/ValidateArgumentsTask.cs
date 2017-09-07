using System;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class ValidateArgumentsTask : ElasticsearchInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public ValidateArgumentsTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Elasticsearch");
		}

		public ValidateArgumentsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem,
			IServiceStateProvider serviceConfig)
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			PrintInstallationState();
			FailOnValidationFailures();
			
			var tempState = this.InstallationModel.TempDirectoryConfiguration.State;
			
			tempState.SeesService = this.ServiceStateProvider.SeesService;
			tempState.ServiceRunning = this.ServiceStateProvider.Running;

			var environmentState = this.InstallationModel.ElasticsearchEnvironmentConfiguration.StateProvider;
			tempState.HomeDirectoryMachineVariable = environmentState.HomeDirectoryMachineVariable;
			tempState.NewConfigDirectoryMachineVariable = environmentState.NewConfigDirectoryMachineVariable;
			tempState.OldConfigDirectoryMachineVariable = environmentState.OldConfigDirectoryMachineVariable;
			
			if (this.ServiceStateProvider.SeesService)
			{
				this.Session.Log($"Service registered");

				if (this.ServiceStateProvider.Running)
				{
					this.Session.Log($"Service running");
				}
			}

			this.Session.Log($"ES_CONFIG {Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.Machine)}");
			this.Session.Log($"ES_HOME {Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Machine)}");
			this.Session.Log($"CONF_DIR {Environment.GetEnvironmentVariable("CONF_DIR", EnvironmentVariableTarget.Machine)}");

			return true;
		}

		private void PrintInstallationState()
		{
			this.Session.Log($"--- Installation Start State ---");
			this.Session.Log($"Existing Version Installed: {this.InstallationModel.NoticeModel.ExistingVersionInstalled}");
			this.Session.Log($"Current Version: {this.InstallationModel.NoticeModel.CurrentVersion}");
			this.Session.Log($"Existing Version: {this.InstallationModel.NoticeModel.ExistingVersion}");
			this.Session.Log($"Session Installing: {this.Session.IsInstalling}");
			this.Session.Log($"Session Uninstalling: {this.Session.IsUninstalling}");
			this.Session.Log($"Session Rollback: {this.Session.IsRollback}");
			this.Session.Log($"Session Upgrading: {this.Session.IsUpgrading}");
			this.Session.Log("Passed Args:\r\n" + string.Join(", ", this.SanitizedArgs));
			this.Session.Log("TempDirectoryState:\r\n" + this.InstallationModel.TempDirectoryConfiguration.State);
			this.Session.Log("ViewModelState:\r\n" + this.InstallationModel);
		}

		private void FailOnValidationFailures()
		{
			if (this.InstallationModel.IsValid && this.InstallationModel.Steps.All(s => s.IsValid)) return;
			var errorPrefix = $"Cannot continue installation because of the following errors";
			var failures = this.InstallationModel.ValidationFailures
				.Concat(this.InstallationModel.Steps.SelectMany(s => s.ValidationFailures))
				.ToList();

			var validationFailures = ValidationFailures(failures);
			throw new Exception(errorPrefix + Environment.NewLine + validationFailures);
		}
	}
}