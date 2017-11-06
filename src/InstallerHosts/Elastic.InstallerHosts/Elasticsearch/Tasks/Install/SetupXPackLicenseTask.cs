using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetupXPackLicenseTask : ElasticsearchRunningInstallationTaskBase
	{
		public SetupXPackLicenseTask(string[] args, ISession session) 
			: base(args, session) { }

		public SetupXPackLicenseTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 600;

		protected override bool ExecuteTask()
		{
			var xPackModel = this.InstallationModel.XPackModel;
			if (!xPackModel.IsRelevant || string.IsNullOrEmpty(xPackModel.XPackLicenseFile)) return true;

			this.Session.SendActionStart(TotalTicks, ActionName, "Setting up X-Pack license", "Setting up X-Pack license: [1]");

			using (var client = CreateClient())
			{
				var password = this.InstallationModel.XPackModel.BootstrapPassword;
				WaitForNodeToAcceptRequests(client, password, 300);

				this.Session.SendProgress(100, "Updating license");
				using (var message = new HttpRequestMessage(HttpMethod.Put, "_xpack/license?acknowledge=true"))
				{
					if (!string.IsNullOrEmpty(password))
					{
						this.Session.Log("Using Bootstrap password to apply license");
						var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{password}"));
						message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
					}

					var license = this.FileSystem.File.ReadAllText(this.InstallationModel.XPackModel.XPackLicenseFile);			
					message.Content = new StringContent(license, Encoding.UTF8, "application/json");
					var response = client.SendAsync(message).Result;
					if (!response.IsSuccessStatusCode)
					{
						if (response.Content != null)
						{
							var responseContent = response.Content.ReadAsStringAsync().Result;
							response.Content.Dispose();
							this.Session.Log(responseContent);
						}

						throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
					}
				}
				this.Session.SendProgress(200, "Updated license");
			}

			return true;
		}
	}
}