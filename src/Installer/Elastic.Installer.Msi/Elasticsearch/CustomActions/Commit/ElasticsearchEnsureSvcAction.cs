using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Commit;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Commit
{
	public class ElasticsearchEnsureSvcAction : CommitCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchEnsureSvcAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.ElasticsearchEnsureServiceStart;
		public override Condition Condition => new Condition("INSTALLASSERVICE=\"true\" AND STARTAFTERINSTALL=\"true\"");

		[CustomAction]
		public static ActionResult ElasticsearchEnsureSvc(Session session) =>
			session.Handle(() => new EnsureServiceStartTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}