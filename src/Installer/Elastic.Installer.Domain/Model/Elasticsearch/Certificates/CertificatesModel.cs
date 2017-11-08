using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Certificates
{
	public class CertificatesModel : StepBase<CertificatesModel, CertificatesModelValidator>
	{
		public CertificatesModel(IObservable<bool> xPackSecurityEnabled)
		{
			xPackSecurityEnabled.Subscribe(t => this.IsRelevant = t);
			this.Header = "Certificates";

			this.AddTransportCA = ReactiveCommand.CreateAsyncTask(async _ => await this.AddTransportCAUITask());
			this.WhenAnyObservable(vm => vm.AddTransportCA)
				.Subscribe(x =>
				{
					if (string.IsNullOrWhiteSpace(x)) return;
					this.TransportCAFiles.Add(x);
				});

			var canRemoveTransportCA = this.WhenAny(vm => vm.SelectedTransportCA, selected => !string.IsNullOrWhiteSpace(selected.GetValue()));
			this.RemoveTransportCA = ReactiveCommand.Create(canRemoveTransportCA);
			this.RemoveTransportCA.Subscribe(x =>
			{
				this.TransportCAFiles.Remove(this.SelectedTransportCA);
			});

			this.AddHttpCA = ReactiveCommand.CreateAsyncTask(async _ => await this.AddHttpCAUITask());
			this.WhenAnyObservable(vm => vm.AddHttpCA)
				.Subscribe(x =>
				{
					if (string.IsNullOrWhiteSpace(x)) return;
					this.HttpCAFiles.Add(x);
				});

			var canRemoveHttpCA = this.WhenAny(vm => vm.SelectedHttpCA, (selected) => !string.IsNullOrWhiteSpace(selected.GetValue()));
			this.RemoveHttpCA = ReactiveCommand.Create(canRemoveHttpCA);
			this.RemoveHttpCA.Subscribe(x =>
			{
				this.HttpCAFiles.Remove(this.SelectedHttpCA);
			});

			this.Refresh();
		}

		public Func<Task<string>> AddTransportCAUITask { get; set; }
		public Func<Task<string>> AddHttpCAUITask { get; set; }

		public ReactiveCommand<string> AddTransportCA { get; }
		public ReactiveCommand<string> AddHttpCA { get; }

		public ReactiveCommand<object> RemoveTransportCA { get; }
		public ReactiveCommand<object> RemoveHttpCA { get; }

		bool _generateTransportCert = true;
		[Argument(nameof(GenerateTransportCert))]
		public bool GenerateTransportCert
		{
			get => this._generateTransportCert;
			set => this.RaiseAndSetIfChanged(ref this._generateTransportCert, value);
		}

		private ReactiveList<string> _transportCaFiles = new ReactiveList<string>();
		[Argument(nameof(TransportCAFiles))]
		public ReactiveList<string> TransportCAFiles
		{
			get => _transportCaFiles;
			set { this.RaiseAndSetIfChanged(ref _transportCaFiles, new ReactiveList<string>(value?.Where(n => !string.IsNullOrEmpty(n)).Select(n => n.Trim()).ToList())); }
		}

		string _selectedTransportCa;
		public string SelectedTransportCA
		{
			get => this._selectedTransportCa;
			set => this.RaiseAndSetIfChanged(ref this._selectedTransportCa, value);
		}

		string _transportCertFile;
		[Argument(nameof(TransportCertFile))]
		public string TransportCertFile
		{
			get => this._transportCertFile;
			set => this.RaiseAndSetIfChanged(ref this._transportCertFile, value);
		}

		string _transportKeyFile;
		[Argument(nameof(TransportKeyFile))]
		public string TransportKeyFile
		{
			get => this._transportKeyFile;
			set => this.RaiseAndSetIfChanged(ref this._transportKeyFile, value);
		}

		bool _generateHttpCert;
		[Argument(nameof(GenerateHttpCert))]
		public bool GenerateHttpCert
		{
			get => this._generateHttpCert;
			set => this.RaiseAndSetIfChanged(ref this._generateHttpCert, value);
		}

		private ReactiveList<string> _httpCaFiles = new ReactiveList<string>();
		[Argument(nameof(HttpCAFiles))]
		public ReactiveList<string> HttpCAFiles
		{
			get => _httpCaFiles;
			set { this.RaiseAndSetIfChanged(ref _httpCaFiles, new ReactiveList<string>(value?.Where(n => !string.IsNullOrEmpty(n)).Select(n => n.Trim()).ToList())); }
		}

		string _selectedHttpCa;
		public string SelectedHttpCA
		{
			get => this._selectedHttpCa;
			set => this.RaiseAndSetIfChanged(ref this._selectedHttpCa, value);
		}

		string _httpCertFile;
		[Argument(nameof(HttpCertFile))]
		public string HttpCertFile
		{
			get => this._httpCertFile;
			set => this.RaiseAndSetIfChanged(ref this._httpCertFile, value);
		}

		string _httpKeyFile;
		[Argument(nameof(HttpKeyFile))]
		public string HttpKeyFile
		{
			get => this._httpKeyFile;
			set => this.RaiseAndSetIfChanged(ref this._httpKeyFile, value);
		}

		public override void Refresh()
		{
			this.TransportCAFiles = new ReactiveList<string>();
			this.TransportCertFile = null;
			this.TransportKeyFile = null;
			this.GenerateTransportCert = true;
			this.GenerateHttpCert = false;
			this.HttpCAFiles = new ReactiveList<string>();
			this.HttpCertFile = null;
			this.HttpKeyFile = null;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(CertificatesModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(IsRelevant)} = " + IsRelevant);
			sb.AppendLine($"- {nameof(TransportCertFile)} = " + TransportCertFile);
			sb.AppendLine($"- {nameof(TransportKeyFile)} = " + TransportKeyFile);
			sb.AppendLine($"- {nameof(TransportCAFiles)} = " + TransportCAFiles);
			sb.AppendLine($"- {nameof(HttpCertFile)} = " + TransportCertFile);
			sb.AppendLine($"- {nameof(HttpKeyFile)} = " + TransportKeyFile);
			sb.AppendLine($"- {nameof(HttpCAFiles)} = " + TransportCAFiles);
			return sb.ToString();
		}
	}
}