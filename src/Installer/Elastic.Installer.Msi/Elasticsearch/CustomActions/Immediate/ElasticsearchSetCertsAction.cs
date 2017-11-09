using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Immediate
{
	public class ElasticsearchSetCertsAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchSetCertsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.CertsProperties;
		public override Condition Condition => new Condition("(NOT Installed) AND XPACKSECURITYENABLED~=\"true\" AND (XPACKLICENSE~=\"Trial\" OR (NOT XPACKLICENSEFILE=\"\"))");
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => new Step(nameof(ElasticsearchSetBootstrapPasswordAction));
		public override When When => When.After;
		public override Execute Execute => Execute.immediate;

		[CustomAction]
		public static ActionResult ElasticsearchSetCerts(Session session) =>
			session.Handle(() => new SetCertsPropertyTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}