using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetCertsPropertyTask : ElasticsearchInstallationTaskBase
	{
		public SetCertsPropertyTask(string[] args, ISession session) 
			: base(args, session) {}

		public SetCertsPropertyTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			// TODO: Implement

			return true;
		}
	}
}