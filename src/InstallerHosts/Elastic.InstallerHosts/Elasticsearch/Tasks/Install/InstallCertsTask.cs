using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Compression;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class InstallCertsTask : ElasticsearchInstallationTaskBase
	{
		public InstallCertsTask(string[] args, ISession session) 
			: base(args, session) {}

		public InstallCertsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		private const int TotalTicks = 1000;

		protected override bool ExecuteTask()
		{
			var certsModel = this.InstallationModel.CertificatesModel;
			if (!certsModel.IsRelevant)
				return true;

			this.Session.SendActionStart(TotalTicks, ActionName, "Setting up X-Pack TLS certificates", "Setting up X-Pack TLS certificates: [1]");

			if (certsModel.GenerateTransportCert)
			{
				var layer = "transport";
				GenerateCertificate(layer);
				UnzipAndMoveCertificates(layer);
			}
			else if (!string.IsNullOrEmpty(certsModel.TransportCertFile))
			{
				// move files to config
			}

			if (certsModel.GenerateHttpCert)
			{
				var layer = "http";
				GenerateCertificate(layer);
				UnzipAndMoveCertificates(layer);
			}
			else if (!string.IsNullOrEmpty(certsModel.HttpCertFile))
			{
				// move files to config
			}

			return true;
		}

		private void UnzipAndMoveCertificates(string layer)
		{
			var installationDir = this.InstallationModel.LocationsModel.InstallDir;
			var @out = this.FileSystem.Path.Combine(installationDir, "bin", "x-pack", $"{layer}.zip");
			var certsDirectory = this.FileSystem.Path.Combine(InstallationModel.LocationsModel.ConfigDirectory, "x-pack", "certs");

			if (!FileSystem.Directory.Exists(certsDirectory))
				FileSystem.Directory.CreateDirectory(certsDirectory);

			var unzippedDirectory = this.FileSystem.Path.GetDirectoryName(@out);

			ZipFile.ExtractToDirectory(@out, unzippedDirectory);

			// TODO: Now copy the files over

		}

		private void GenerateCertificate(string layer)
		{
			var installationDir = this.InstallationModel.LocationsModel.InstallDir;
			var binary = this.FileSystem.Path.Combine(installationDir, "bin", "x-pack", "certgen.bat");
			var @in = this.FileSystem.Path.Combine(installationDir, "bin", "x-pack", $"{layer}-certgen.yml");
			var @out = this.FileSystem.Path.Combine(installationDir, "bin", "x-pack", $"{layer}.zip");

			if (FileSystem.File.Exists(@in))
				FileSystem.File.Delete(@in);

			this.FileSystem.File.WriteAllLines(@in, new[]
			{
				"instances:",
				$"    - name : \"{this.InstallationModel.ConfigurationModel.NodeName}\"",
				$"      filename : \"{layer}\"",
			});

			if (FileSystem.File.Exists(@out))
				FileSystem.File.Delete(@out);

			var p = new Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = binary,
					Arguments = $"--in \"{@in}\" --out \"{@out}\" --silent",
					ErrorDialog = false,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true
				}
			};

			p.StartInfo.EnvironmentVariables[ElasticsearchEnvironmentStateProvider.ConfDir] =
				this.InstallationModel.LocationsModel.ConfigDirectory;

			void OnDataReceived(object sender, DataReceivedEventArgs a)
			{
				var message = a.Data;
				if (message != null) this.Session.Log(message);
			}

			// TODO: Check to see if written errors results in non-zero exit code
			var errors = false;

			void OnErrorsReceived(object sender, DataReceivedEventArgs a)
			{
				var message = a.Data;
				if (message != null)
				{
					errors = true;
					this.Session.Log(message);
				}
			}

			p.ErrorDataReceived += OnErrorsReceived;
			p.OutputDataReceived += OnDataReceived;
			p.Start();
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			p.WaitForExit();

			var exitCode = p.ExitCode;

			p.ErrorDataReceived -= OnErrorsReceived;
			p.OutputDataReceived -= OnDataReceived;
			p.Close();

			if (exitCode != 0)
			{
				this.Session.Log($"certgen process returned non-zero exit code: {exitCode}");
				throw new Exception();
			}
		}
	}
}