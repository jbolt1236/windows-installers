using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Commit
{
	public class ElasticsearchEnsureServiceStartAction : CommitCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchEnsureServiceStartAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.ElasticsearchEnsureServiceStart;
		public override Condition Condition => new Condition("INSTALLASSERVICE=\"true\" AND STARTAFTERINSTALL=\"true\"");
		public override bool NeedsElevatedPrivileges => false;

		[CustomAction]
		public static ActionResult ElasticsearchEnsureServiceStart(Session session) =>
			session.Handle(() => new EnsureServiceStartTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}