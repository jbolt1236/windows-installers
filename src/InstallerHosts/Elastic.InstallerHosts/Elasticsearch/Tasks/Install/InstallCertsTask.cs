using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class InstallCertsTask : ElasticsearchInstallationTaskBase
	{
		public InstallCertsTask(string[] args, ISession session) 
			: base(args, session) {}

		public InstallCertsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		private const int TotalTicks = 1000;

		protected override bool ExecuteTask()
		{
			var certsModel = this.InstallationModel.CertificatesModel;
			if (!certsModel.IsRelevant)
				return true;

			this.Session.SendActionStart(TotalTicks, ActionName, "Setting up X-Pack TLS certificates", "Setting up X-Pack TLS certificates: [1]");

			// TODO: Implement

			return true;
		}
	}
}