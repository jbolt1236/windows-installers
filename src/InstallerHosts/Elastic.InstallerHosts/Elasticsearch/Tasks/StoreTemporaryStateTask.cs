using System;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class StoreTemporaryStateTask : ElasticsearchInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public StoreTemporaryStateTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Elasticsearch");
		}

		public StoreTemporaryStateTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem,
			IServiceStateProvider serviceConfig)
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			var tempState = this.InstallationModel.TempDirectoryConfiguration.State;
			
			tempState.SeesService = this.ServiceStateProvider.SeesService;
			tempState.ServiceRunning = this.ServiceStateProvider.Running;

			var environmentState = this.InstallationModel.ElasticsearchEnvironmentConfiguration.StateProvider;
			if (!string.IsNullOrEmpty(environmentState.HomeDirectoryMachineVariable))
				tempState.HomeDirectoryMachineVariable = environmentState.HomeDirectoryMachineVariable;
			if (!string.IsNullOrEmpty(environmentState.NewConfigDirectoryMachineVariable))
				tempState.NewConfigDirectoryMachineVariable = environmentState.NewConfigDirectoryMachineVariable;
			if (!string.IsNullOrEmpty(environmentState.OldConfigDirectoryMachineVariable))
				tempState.OldConfigDirectoryMachineVariable = environmentState.OldConfigDirectoryMachineVariable;
			
			PrintInstallationState();
			
			return true;
		}

		private void PrintInstallationState()
		{
			this.Session.Log($"--- Installation Tempory State ---");
			this.Session.Log("TempDirectoryState:\r\n" + this.InstallationModel.TempDirectoryConfiguration.State);
		}
	}
}