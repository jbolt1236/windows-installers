using System;
using System.Data;
using System.IO.Abstractions;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using ReactiveUI;

namespace Elastic.Installer.Domain.Configuration
{
	public class TempDirectoryConfiguration
	{
		private const string DirectorySuffix = "_Installation";
		private ISession Session { get; }
		private IFileSystem FileSystem { get; }
		private string ProductName { get; }
		public string TempProductInstallationDirectory { get; }
		public TempDirectoryStateConfiguration State { get; }
		public TempDirectoryConfiguration(ISession session, IElasticsearchEnvironmentStateProvider esState, IFileSystem fileSystem)
		{
			Session = session;
			FileSystem = fileSystem ?? new FileSystem();;
			ProductName = this.Session.ProductName;
			TempProductInstallationDirectory = this.FileSystem.Path.Combine(esState.TempDirectoryVariable, ProductName + DirectorySuffix);
			State = new TempDirectoryStateConfiguration(TempProductInstallationDirectory, FileSystem);
		}

		public void CleanUp()
		{
			if (!this.FileSystem.Directory.Exists(this.TempProductInstallationDirectory)) return;
			try
			{
				this.FileSystem.Directory.Delete(this.TempProductInstallationDirectory, true);
			}
			catch (Exception e)
			{
				// log, but continue.
				this.Session.Log($"Exception deleting {this.TempProductInstallationDirectory}: {e}");
			}
		}

	}
}