using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Elasticsearch.Certificates;
using Elastic.Installer.UI.Controls;
using FluentValidation.Results;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using static System.Windows.Visibility;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class CertificatesView : StepControl<CertificatesModel, CertificatesView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(CertificatesModel), typeof(CertificatesView), new PropertyMetadata(null, ViewModelPassed));

		public override CertificatesModel ViewModel
		{
			get => (CertificatesModel) GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		private readonly Brush _defaultBrush;

		public CertificatesView()
		{
			InitializeComponent();
			this._defaultBrush = this.TransportCertTextBox.BorderBrush;
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.GenerateTransportCert, v => v.GenerateTransportCertificateCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.TransportCertFile, v => v.TransportCertTextBox.Text);
			this.Bind(ViewModel, vm => vm.TransportKeyFile, v => v.TransportKeyTextBox.Text);
			this.OneWayBind(ViewModel, vm => vm.TransportCAFiles, v => v.TransportCertAuthoritiesListBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedTransportCA, v => v.TransportCertAuthoritiesListBox.SelectedItem);

			this.ViewModel.AddTransportCAUITask = () =>
				Task.FromResult(BrowseForFile(
					ViewModel.TransportCAFiles.LastOrDefault(),
					"Add a Certificate Authority for Transport layer",
					"Certificate Authority (PEM encoded)",
					"*.crt"));

			this.TransportCertBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFile(
					ViewModel.TransportCertFile,
					"Select a Certificate file for Transport layer",
					"Certificate (PEM encoded)",
					"*.crt",
					result => ViewModel.TransportCertFile = result));

			this.TransportKeyBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFile(
					ViewModel.TransportKeyFile,
					"Select a Key file for Transport layer",
					"Key (PEM encoded)",
					"*.key",
					result => ViewModel.TransportKeyFile = result));

			this.AddTransportCertAuthorityButton.Command = this.ViewModel.AddTransportCA;
			this.RemoveTransportCertAuthorityButton.Command = this.ViewModel.RemoveTransportCA;

			this.Bind(ViewModel, vm => vm.GenerateHttpCert, v => v.GenerateHttpCertificateCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.HttpCertFile, v => v.HttpCertTextBox.Text);
			this.Bind(ViewModel, vm => vm.HttpKeyFile, v => v.HttpKeyTextBox.Text);
			this.OneWayBind(ViewModel, vm => vm.HttpCAFiles, v => v.HttpCertAuthoritiesListBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedHttpCA, v => v.HttpCertAuthoritiesListBox.SelectedItem);

			this.ViewModel.AddHttpCAUITask = () =>
				Task.FromResult(BrowseForFile(
					ViewModel.HttpCAFiles.LastOrDefault(),
					"Add a Certificate Authority for HTTP layer",
					"Certificate Authority (PEM encoded)",
					"*.crt"));

			this.AddHttpCertAuthorityButton.Command = this.ViewModel.AddHttpCA;
			this.RemoveHttpCertAuthorityButton.Command = this.ViewModel.RemoveHttpCA;

			this.HttpCertBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFile(
					ViewModel.HttpCertFile, 
					"Select a Certificate file for HTTP layer",
					"Certificate (PEM encoded)", 
					"*.crt",
					result => ViewModel.HttpCertFile = result));

			this.HttpKeyBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFile(
					ViewModel.HttpKeyFile,
					"Select a Key file for HTTP layer",
					"Key (PEM encoded)",
					"*.key",
					result => ViewModel.HttpKeyFile = result));

			this.WhenAnyValue(m => m.ViewModel.GenerateTransportCert)
				.Subscribe(g => this.TransportCertGrid.Visibility = g ? Collapsed : Visible);

			this.WhenAnyValue(m => m.ViewModel.GenerateHttpCert)
				.Subscribe(g => this.HttpCertGrid.Visibility = g ? Collapsed : Visible);
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233, 73, 152));
			this.TransportCertTextBox.BorderBrush = _defaultBrush;
			this.TransportKeyTextBox.BorderBrush = _defaultBrush;
			this.TransportCertAuthoritiesListBox.BorderBrush = _defaultBrush;
			this.HttpCertTextBox.BorderBrush = _defaultBrush;
			this.HttpKeyTextBox.BorderBrush = _defaultBrush;
			this.HttpCertAuthoritiesListBox.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case nameof(ViewModel.TransportCertFile):
						this.TransportCertTextBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.TransportKeyFile):
						this.TransportKeyTextBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.TransportCAFiles):
						this.TransportCertAuthoritiesListBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.HttpCertFile):
						this.HttpCertTextBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.HttpKeyFile):
						this.HttpKeyTextBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.HttpCAFiles):
						this.HttpCertAuthoritiesListBox.BorderBrush = b;
						continue;
				}
			}
		}

		protected string BrowseForFile(string defaultLocation, string title, string filterName, string filter, Action<string> setter = null)
		{
			var dlg = new CommonOpenFileDialog
			{
				InitialDirectory = defaultLocation,
				AddToMostRecentlyUsedList = false,
				AllowNonFileSystemItems = false,
				DefaultDirectory = defaultLocation,
				EnsureFileExists = true,
				EnsurePathExists = true,
				EnsureReadOnly = false,
				EnsureValidNames = true,
				Multiselect = false,
				ShowPlacesList = true,
				Title = title,
				Filters = { new CommonFileDialogFilter(filterName, filter) }
			};

			var result = dlg.ShowDialog();
			if (result == CommonFileDialogResult.Ok)
			{
				setter?.Invoke(dlg.FileName);
				return dlg.FileName;
			}

			return null;
		}
	}
}