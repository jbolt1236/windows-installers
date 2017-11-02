using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using static System.Windows.Visibility;
using EventsMixin = System.Windows.Controls.Primitives.EventsMixin;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class XPackView : StepControl<XPackModel, XPackView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(XPackModel), typeof(XPackView), new PropertyMetadata(null, ViewModelPassed));

		public override XPackModel ViewModel
		{
			get => (XPackModel) GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		private readonly Brush _defaultBrush;

		public XPackView()
		{
			InitializeComponent();
			this._defaultBrush = this.ElasticPasswordBox.BorderBrush;
		}

		protected override void InitializeBindings()
		{
			// cannot bind to the Password property directly as it does not expose a DP
			this.ElasticPasswordBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.ElasticUserPassword = this.ElasticPasswordBox.Password);
			this.KibanaUserPasswordBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.KibanaUserPassword = this.KibanaUserPasswordBox.Password);
			this.LogstashSystemPasswordBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.LogstashSystemUserPassword = this.LogstashSystemPasswordBox.Password);

			this.Bind(ViewModel, vm => vm.SkipSettingPasswords, v => v.SkipPasswordGenerationCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.XPackSecurityEnabled, v => v.EnableXPackSecurityCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.XPackLicenseFile, v => v.LicenseFileTextBox.Text);
			this.Bind(ViewModel, vm => vm.SelfGenerateLicense, v => v.CreateLicenseRadioButton.IsChecked);
			this.Bind(ViewModel, vm => vm.UploadLicenseFile, v => v.UploadLicenseFileRadioButton.IsChecked);
			this.BindCommand(ViewModel, vm => vm.OpenLicensesAndSubscriptions, v => v.OpenSubscriptionsLink, nameof(OpenSubscriptionsLink.Click));
			this.BindCommand(ViewModel, vm => vm.RegisterBasicLicense, v => v.RegisterBasicLicenseLink, nameof(RegisterBasicLicenseLink.Click));
			this.BindCommand(ViewModel, vm => vm.OpenManualUserConfiguration, v => v.OpenManualUserConfigurationLink, nameof(OpenManualUserConfigurationLink.Click));
			this.BindCommand(ViewModel, vm => vm.OpenManuallyApplyLicense, v => v.OpenManuallyApplyLicenseLink, nameof(OpenManuallyApplyLicenseLink.Click));

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.ViewModel.OpenLicensesAndSubscriptions.Subscribe(x => Process.Start(ViewResources.XPackView_OpenLicensesAndSubscriptions));
			this.ViewModel.RegisterBasicLicense.Subscribe(x => Process.Start(ViewResources.XPackView_RegisterBasicLicense));
			this.ViewModel.OpenManuallyApplyLicense.Subscribe(x => Process.Start(string.Format(ViewResources.XPackView_UpdatingYourLicense, majorMinor)));
			this.ViewModel.OpenManualUserConfiguration.Subscribe(x => Process.Start(string.Format(ViewResources.XPackView_ManualUserConfiguration, majorMinor)));

			foreach (var name in Enum.GetNames(typeof(XPackLicenseMode)))
				this.LicenseDropDown.Items.Add(new ComboBoxItem {Content = name});

			this.Bind(ViewModel, vm => vm.XPackLicense, x => x.LicenseDropDown.SelectedIndex
				, null, vmToViewConverterOverride: new LicenseToSelectedIndexConverter()
				, viewToVMConverterOverride: new SelectedIndexToLicenseConverter());

			this.LicenseFileBrowseButton.Events().Click
				.Subscribe(file => this.BrowseForFile(ViewModel.XPackLicenseFile, result => ViewModel.XPackLicenseFile = result));

			this.ViewModel.WhenAnyValue(vm => vm.SelfGenerateLicense)
				.Subscribe(generate => ViewModel.XPackLicense = generate 
					? (XPackLicenseMode)Enum.Parse(typeof(XPackLicenseMode), ((ComboBoxItem)this.LicenseDropDown.SelectedItem).Content.ToString()) 
					: (XPackLicenseMode?)null);

			this.ViewModel.WhenAnyValue(
					vm => vm.XPackLicense,
					vm => vm.SkipSettingPasswords,
					vm => vm.XPackSecurityEnabled,
					vm => vm.InstallServiceAndStartAfterInstall,
					vm => vm.SelfGenerateLicense,
					vm => vm.UploadLicenseFile,
					vm => vm.UploadedXPackLicense
				)
				.Subscribe(t =>
				{
					var isNonBasicLicense = (t.Item1 == XPackLicenseMode.Trial && t.Item5) || 
											(t.Item6 && !string.IsNullOrEmpty(t.Item7) && t.Item7 != "basic");
					var securityEnabled = t.Item3;
					var installServiceAndStartAfterInstall = t.Item4;

					this.SecurityGrid.Visibility = isNonBasicLicense ? Visible : Hidden;
					
					if (!isNonBasicLicense || !securityEnabled)
						this.UserGrid.Visibility = Collapsed;
					else if (!this.ViewModel.NeedsPasswords)
						this.UserGrid.Visibility = Collapsed;
					else this.UserGrid.Visibility = Visible;
					
					this.ManualSetupGrid.Visibility =
						!isNonBasicLicense || !securityEnabled
							? Collapsed : (!this.ViewModel.NeedsPasswords ? Visible : Collapsed);

					this.UserLabel.Visibility = isNonBasicLicense && securityEnabled ? Visible : Collapsed;
					this.SkipPasswordGenerationCheckBox.Visibility = isNonBasicLicense && securityEnabled ? Visible : Collapsed;
					this.SkipPasswordGenerationCheckBox.IsEnabled = securityEnabled && installServiceAndStartAfterInstall;
				});

			this.ViewModel.WhenAnyValue(
					vm => vm.UploadLicenseFile,
					vm => vm.InstallServiceAndStartAfterInstall
				)
				.Subscribe(t =>
				{
					this.ManualLicenseGrid.Visibility = t.Item1 && !t.Item2 ? Visible : Collapsed;
					this.UploadLicenseGrid.Visibility = t.Item1 && t.Item2 ? Visible : Collapsed;
				});

			this.ViewModel.WhenAnyValue(vm => vm.SelfGenerateLicense)
				.Subscribe(u => this.CreateLicenseGrid.Visibility = u ? Visible : Collapsed);
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233, 73, 152));
			this.ElasticPasswordBox.BorderBrush = _defaultBrush;
			this.KibanaUserPasswordBox.BorderBrush = _defaultBrush;
			this.LogstashSystemPasswordBox.BorderBrush = _defaultBrush;
			this.LicenseFileTextBox.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case nameof(ViewModel.ElasticUserPassword):
						this.ElasticPasswordBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.KibanaUserPassword):
						this.KibanaUserPasswordBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.LogstashSystemUserPassword):
						this.LogstashSystemPasswordBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.XPackLicenseFile):
						this.LicenseFileTextBox.BorderBrush = b;
						continue;
				}
			}
		}

		protected class LicenseToSelectedIndexConverter : IBindingTypeConverter
		{
			private static readonly List<XPackLicenseMode> XPackLicenseModes =
				Enum.GetValues(typeof(XPackLicenseMode)).Cast<XPackLicenseMode>().ToList();

			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				result = -1;
				if (!(@from is XPackLicenseMode)) return true;
				var e = (XPackLicenseMode) @from;
				var i = XPackLicenseModes.TakeWhile(v => v != e).Count();
				result = i;
				return true;
			}
		}

		protected class SelectedIndexToLicenseConverter : IBindingTypeConverter
		{
			private static readonly List<XPackLicenseMode> XPackLicenseModes =
				Enum.GetValues(typeof(XPackLicenseMode)).Cast<XPackLicenseMode>().ToList();

			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				result = null;
				if (!(@from is int)) return true;
				var i = (int) @from;
				if (i >= 0 && i < XPackLicenseModes.Count)
					result = XPackLicenseModes[i];
				;
				return true;
			}
		}

		protected void BrowseForFile(string defaultLocation, Action<string> setter)
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
				Title = ViewResources.XPackView_LicenseFileDialogTitle,
				Filters = { new CommonFileDialogFilter(ViewResources.XPackView_LicenseFileDialogFilter, "*.json") }
			};

			var result = dlg.ShowDialog();
			if (result == CommonFileDialogResult.Ok) setter(dlg.FileName);
		}
	}
}