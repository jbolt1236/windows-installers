using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Commit;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Commit
{
	public class ElasticsearchEnsureEnvVarsAction : CommitCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchEnsureEnvVarsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.EnsureEnvironmentVariables;

		[CustomAction]
		public static ActionResult ElasticsearchEnsureEnvVars(Session session) =>
			session.Handle(() => new EnsureEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}