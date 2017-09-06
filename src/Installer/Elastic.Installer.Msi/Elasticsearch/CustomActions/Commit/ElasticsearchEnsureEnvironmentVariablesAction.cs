using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Commit
{
	public class ElasticsearchEnsureEnvironmentVariablesAction : CommitCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchEnsureEnvironmentVariablesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.EnsureEnvironmentVariables;
		public override bool NeedsElevatedPrivileges => false;

		[CustomAction]
		public static ActionResult ElasticsearchEnsureEnvironmentVariables(Session session) =>
			session.Handle(() => new EnsureEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}