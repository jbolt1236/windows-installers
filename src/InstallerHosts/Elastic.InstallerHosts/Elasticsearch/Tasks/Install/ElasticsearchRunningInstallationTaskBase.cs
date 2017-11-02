using System;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public abstract class ElasticsearchRunningInstallationTaskBase : ElasticsearchInstallationTaskBase
	{
		protected ElasticsearchRunningInstallationTaskBase(string[] args, ISession session) 
			: base(args, session) { }

		protected ElasticsearchRunningInstallationTaskBase(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected abstract override bool ExecuteTask();

		protected HttpClient CreateClient()
		{
			var host = !string.IsNullOrEmpty(this.InstallationModel.ConfigurationModel.NetworkHost)
				? this.InstallationModel.ConfigurationModel.NetworkHost
				: "localhost";
			var port = this.InstallationModel.ConfigurationModel.HttpPort;
			var baseAddess = $"http://{host}:{port}/";

			return new HttpClient { BaseAddress = new Uri(baseAddess) };
		}

		protected void WaitForNodeToAcceptRequests(HttpClient client, string elasticUserPassword, int totalTicks)
		{
			var statusCode = 500;
			var times = 0;
			var totalTimes = 30;
			var sleepyTime = 1000;
			var tickIncrement = totalTicks / totalTimes;

			do
			{
				if (times > 0) Thread.Sleep(sleepyTime);
				HttpResponseMessage response = null;
				try
				{
					this.Session.SendProgress(tickIncrement, "Checking Elasticsearch is up and running");

					using (var message = new HttpRequestMessage(HttpMethod.Head, string.Empty))
					{
						if (!string.IsNullOrEmpty(elasticUserPassword))
						{
							var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{elasticUserPassword}"));
							message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
						}

						response = client.SendAsync(message).Result;
						statusCode = (int)response.StatusCode;
					}
				}
				catch (AggregateException ae)
				{
					if (response != null)
						statusCode = (int)response.StatusCode;

					var httpRequestException = ae.InnerException as HttpRequestException;
					if (httpRequestException == null) throw;

					var webException = httpRequestException.InnerException as WebException;
					if (webException == null) throw;

					var socketException = webException.InnerException as SocketException;
					if (socketException == null) throw;

					if (socketException.SocketErrorCode != SocketError.ConnectionRefused) throw;
				}

				++times;
			} while (statusCode >= 500 && times < totalTimes);

			if (statusCode >= 500)
				throw new TimeoutException($"Elasticsearch not seen running after trying for {TimeSpan.FromMilliseconds(totalTimes * sleepyTime)}");

			this.Session.SendProgress(totalTicks - (tickIncrement * times), "Elasticsearch is up and running");
		}
	}
}