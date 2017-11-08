using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Model.Base;
using ReactiveUI;
using Semver;

namespace Elastic.Installer.Domain.Model.Elasticsearch.XPack
{
	public class XPackModel : StepBase<XPackModel, XPackModelValidator>
	{
		public const XPackLicenseMode DefaultXPackLicenseMode = XPackLicenseMode.Basic;
		
		public XPackModel(
			VersionConfiguration versionConfig, 
			IObservable<bool> xPackEnabled, 
			IObservable<bool> installServiceAndStartAfterInstall)
		{
			xPackEnabled.Subscribe(t =>
			{
				this.IsRelevant = t;
			});

			installServiceAndStartAfterInstall.Subscribe(b=>
			{
				this.InstallServiceAndStartAfterInstall = b;
				this.SkipSettingPasswords = !b;
			});

			this.Header = "X-Pack";
			this.CurrentVersion = versionConfig.CurrentVersion;
			this.OpenLicensesAndSubscriptions = ReactiveCommand.Create();
			this.RegisterBasicLicense = ReactiveCommand.Create();
			this.OpenManualUserConfiguration = ReactiveCommand.Create();
			this.OpenManuallyApplyLicense = ReactiveCommand.Create();

			this.WhenAnyValue(vm => vm.XPackLicenseFile)
				.Subscribe(file =>
				{
					if (File.Exists(file))
					{
						var licenseContent = File.ReadAllText(file);
						var match = Regex.Match(licenseContent, "\"type\"\\s*:\\s*\"(?<licenseType>.*?)\"");
						if (match.Success)
							this.UploadedXPackLicense = match.Groups["licenseType"].Value.ToLowerInvariant();
					}
					else
						this.UploadedXPackLicense = null;
				});

			this.Refresh();
		}

		public sealed override void Refresh()
		{
			this.ElasticUserPassword = null;
			this.KibanaUserPassword = null;
			this.LogstashSystemUserPassword = null;
			this.BootstrapPassword = null;
			this.XPackSecurityEnabled = true;
			this.SelfGenerateLicense = true;
			this.XPackLicense = DefaultXPackLicenseMode;
		}

		public SemVersion CurrentVersion { get; }

		string elasticUserPassword;
		[Argument(nameof(ElasticUserPassword), IsHidden = true)]
		public string ElasticUserPassword
		{
			get => this.elasticUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.elasticUserPassword, value);
		}
		string kibanaUserPassword;
		[Argument(nameof(KibanaUserPassword), IsHidden = true)]
		public string KibanaUserPassword
		{
			get => this.kibanaUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.kibanaUserPassword, value);
		}
		string logstashSystemUserPassword;
		[Argument(nameof(LogstashSystemUserPassword), IsHidden = true)]
		public string LogstashSystemUserPassword
		{
			get => this.logstashSystemUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.logstashSystemUserPassword, value);
		}
		
		bool installServiceAndStartAfterInstall;
		public bool InstallServiceAndStartAfterInstall
		{
			get => this.installServiceAndStartAfterInstall;
			set => this.RaiseAndSetIfChanged(ref this.installServiceAndStartAfterInstall, value);
		}
		
		bool xPackSecurityEnabled;
		[Argument(nameof(XPackSecurityEnabled))]
		public bool XPackSecurityEnabled
		{
			get => this.xPackSecurityEnabled;
			set => this.RaiseAndSetIfChanged(ref this.xPackSecurityEnabled, value);
		}
		
		bool skipSettingPasswords;
		[Argument(nameof(SkipSettingPasswords))]
		public bool SkipSettingPasswords
		{
			get => this.skipSettingPasswords;
			set => this.RaiseAndSetIfChanged(ref this.skipSettingPasswords, value);
		}

		XPackLicenseMode? xPackLicense;
		[Argument(nameof(XPackLicense))]
		public XPackLicenseMode? XPackLicense
		{
			get => this.xPackLicense;
			set => this.RaiseAndSetIfChanged(ref this.xPackLicense, value);
		}

		private string bootstrapPassword;
		[Argument(nameof(BootstrapPassword), IsHidden = true)]
		public string BootstrapPassword
		{
			get => this.bootstrapPassword;
			set => this.RaiseAndSetIfChanged(ref this.bootstrapPassword, value);
		}

		private string xpackLicenseFile;
		[Argument(nameof(XPackLicenseFile))]
		public string XPackLicenseFile
		{
			get => this.xpackLicenseFile;
			set => this.RaiseAndSetIfChanged(ref this.xpackLicenseFile, value);
		}

		public bool NeedsPasswords =>
			this.IsRelevant 
			&& this.InstallServiceAndStartAfterInstall 
			&& ((this.XPackLicense == XPackLicenseMode.Trial) || 
			    (!string.IsNullOrEmpty(this.UploadedXPackLicense) && this.UploadedXPackLicense != "basic"))
			&& !this.SkipSettingPasswords
			&& this.XPackSecurityEnabled;

		public ReactiveCommand<object> OpenLicensesAndSubscriptions { get; }

		public ReactiveCommand<object> RegisterBasicLicense { get; }

		public ReactiveCommand<object> OpenManualUserConfiguration { get; }

		public ReactiveCommand<object> OpenManuallyApplyLicense { get; }

		private bool selfGenerateLicense;
		public bool SelfGenerateLicense
		{
			get => this.selfGenerateLicense;
			set => this.RaiseAndSetIfChanged(ref this.selfGenerateLicense, value);
		}

		private bool uploadLicenseFile;
		public bool UploadLicenseFile
		{
			get => this.uploadLicenseFile;
			set => this.RaiseAndSetIfChanged(ref this.uploadLicenseFile, value);
		}

		private string uploadedXPackLicense;
		public string UploadedXPackLicense
		{
			get => this.uploadedXPackLicense;
			set => this.RaiseAndSetIfChanged(ref this.uploadedXPackLicense, value);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(XPackModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(IsRelevant)} = " + IsRelevant);
			sb.AppendLine($"- {nameof(NeedsPasswords)} = " + NeedsPasswords);
			sb.AppendLine($"- {nameof(XPackLicense)} = " + (XPackLicense.HasValue ? Enum.GetName(typeof(XPackLicenseMode), XPackLicense) : string.Empty));
			sb.AppendLine($"- {nameof(XPackLicenseFile)} = " + XPackLicenseFile);
			sb.AppendLine($"- {nameof(UploadedXPackLicense)} = {UploadedXPackLicense}");
			sb.AppendLine($"- {nameof(InstallServiceAndStartAfterInstall)} = " + InstallServiceAndStartAfterInstall);
			sb.AppendLine($"- {nameof(SkipSettingPasswords)} = " + SkipSettingPasswords);
			sb.AppendLine($"- {nameof(XPackSecurityEnabled)} = " + XPackSecurityEnabled);
			sb.AppendLine($"- {nameof(BootstrapPassword)} = " + !string.IsNullOrWhiteSpace(BootstrapPassword));
			sb.AppendLine($"- {nameof(ElasticUserPassword)} = " + !string.IsNullOrWhiteSpace(ElasticUserPassword));
			sb.AppendLine($"- {nameof(KibanaUserPassword)} = " + !string.IsNullOrWhiteSpace(KibanaUserPassword));
			sb.AppendLine($"- {nameof(LogstashSystemUserPassword)} = " + !string.IsNullOrWhiteSpace(LogstashSystemUserPassword));
			return sb.ToString();
		}

	}
}