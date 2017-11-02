using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetupXPackPasswordsTask : ElasticsearchRunningInstallationTaskBase
	{
		public SetupXPackPasswordsTask(string[] args, ISession session) 
			: base(args, session) { }
		public SetupXPackPasswordsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 1200;

		protected override bool ExecuteTask()
		{
			var xPackModel = this.InstallationModel.XPackModel;
			if (!xPackModel.IsRelevant || (!xPackModel.XPackLicense.HasValue && string.IsNullOrEmpty(xPackModel.XPackLicenseFile)))
				return true;

			this.Session.SendActionStart(TotalTicks, ActionName, "Setting up X-Pack passwords", "Setting up X-Pack passwords: [1]");

			using (var client = CreateClient())
			{
				var password = this.InstallationModel.XPackModel.BootstrapPassword;
				WaitForNodeToAcceptRequests(client, password, 300);
				var elasticUserPassword = this.InstallationModel.XPackModel.ElasticUserPassword;
				SetPassword(client, password, "elastic", elasticUserPassword);

				// change the elastic user password used for subsequent users, after updating it for elastic user
				password = elasticUserPassword;
				SetPassword(client, password, "kibana", this.InstallationModel.XPackModel.KibanaUserPassword);
				SetPassword(client, password, "logstash_system", this.InstallationModel.XPackModel.LogstashSystemUserPassword);
			}
			
			return true;
		}

		private void SetPassword(HttpClient client, string elasticUserPassword, string user, string password)
		{
			this.Session.SendProgress(100, $"Changing password for '{user}'");
			using (var message = new HttpRequestMessage(HttpMethod.Put, $"_xpack/security/user/{user}/_password"))
			{
				var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{elasticUserPassword}"));

				message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
				message.Content = new StringContent($"{{\"password\":\"{password}\"}}", Encoding.UTF8, "application/json");
				var response = client.SendAsync(message).Result;
				response.EnsureSuccessStatusCode();
			}
			this.Session.SendProgress(200, $"Changed password for user '{user}'");
		}
	}
}