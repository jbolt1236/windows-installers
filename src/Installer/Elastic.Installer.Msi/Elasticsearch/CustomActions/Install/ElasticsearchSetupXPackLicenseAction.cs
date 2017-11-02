using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class ElasticsearchSetupXPackLicenseAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchSetupXPackLicenseAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.SetupXPackLicense;
		public override Condition Condition => new Condition("(NOT Installed) AND (NOT XPACKLICENSEFILE=\"\")");
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(ElasticsearchServiceStartAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchSetupXPackLicense(Session session) =>
			session.Handle(() => new SetupXPackLicenseTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}