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
			var certificatesModel = InstallationModel.CertificatesModel;

			if (!certificatesModel.IsRelevant)
				return true;

			// only set generated properties if explicit values have not been provided
			if (certificatesModel.GenerateTransportCert && 
				string.IsNullOrEmpty(certificatesModel.TransportCertFile))
			{
				SetProperty(nameof(certificatesModel.TransportCertFile), "transport.crt");
				SetProperty(nameof(certificatesModel.TransportKeyFile), "transport.key");
				SetProperty(nameof(certificatesModel.TransportCAFiles), "transport-ca.crt");
			}

			if (certificatesModel.GenerateHttpCert &&
			    string.IsNullOrEmpty(certificatesModel.HttpCertFile))
			{
				SetProperty(nameof(certificatesModel.HttpCertFile), "http.crt");
				SetProperty(nameof(certificatesModel.HttpKeyFile), "http.key");
				SetProperty(nameof(certificatesModel.HttpCAFiles), "http-ca.crt");
			}

			return true;
		}

		private void SetProperty(string name, string value)
		{
			var path = this.FileSystem.Path.Combine(InstallationModel.LocationsModel.ConfigDirectory, "x-pack", "certs", value);
			this.Session.Set(name.ToUpperInvariant(), path);
		}
	}
}