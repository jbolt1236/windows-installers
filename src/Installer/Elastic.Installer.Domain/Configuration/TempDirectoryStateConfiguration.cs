using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;

namespace Elastic.Installer.Domain.Configuration
{
	public class TempDirectoryStateConfiguration
	{
		public IFileSystem FileSystem { get; }
		private readonly string _tempDirectory;
		
		public string StateDirectory { get; }

		public TempDirectoryStateConfiguration(string tempDirectory, IFileSystem fileSystem)
		{
			FileSystem = fileSystem;
			_tempDirectory = tempDirectory;
			StateDirectory = this.FileSystem.Path.Combine(_tempDirectory, "_state");
		}
		
		public bool SeesService
		{
			get => this.Read<bool>(nameof(SeesService));
			set => this.Set(nameof(SeesService), value.ToString(CultureInfo.InvariantCulture));
		}
		public bool ServiceRunning
		{
			get => this.Read<bool>(nameof(ServiceRunning));
			set => this.Set(nameof(ServiceRunning), value.ToString(CultureInfo.InvariantCulture));
		}

		public string HomeDirectoryMachineVariable 
		{
			get => this.Read<string>(nameof(HomeDirectoryMachineVariable));
			set => this.Set(nameof(HomeDirectoryMachineVariable), value?.ToString(CultureInfo.InvariantCulture));
		}
		public string NewConfigDirectoryMachineVariable 
		{
			get => this.Read<string>(nameof(NewConfigDirectoryMachineVariable));
			set => this.Set(nameof(NewConfigDirectoryMachineVariable), value?.ToString(CultureInfo.InvariantCulture));
		}
		public string OldConfigDirectoryMachineVariable
		{
			get => this.Read<string>(nameof(OldConfigDirectoryMachineVariable));
			set => this.Set(nameof(OldConfigDirectoryMachineVariable), value?.ToString(CultureInfo.InvariantCulture));
		}

		private void Set(string key, string value)
		{
			if (!this.FileSystem.Directory.Exists(_tempDirectory))
				this.FileSystem.Directory.CreateDirectory(_tempDirectory);
			if (!this.FileSystem.Directory.Exists(StateDirectory))
				this.FileSystem.Directory.CreateDirectory(StateDirectory);

			var filePath = FilePath(key);
			this.FileSystem.File.WriteAllText(filePath, value);
		}

		public string FilePath(string key) => this.FileSystem.Path.Combine(StateDirectory, $"_{key}.state");

		public bool Exists(string key)
		{
			if (!this.FileSystem.Directory.Exists(_tempDirectory)) return false;
			if (!this.FileSystem.Directory.Exists(StateDirectory)) return false;
			
			var filePath = FilePath(key);
			return this.FileSystem.File.Exists(filePath);
		}

		private T Read<T>(string key)
		{
			if (!this.Exists(key)) return default(T);
			
			var filePath = FilePath(key);
			var value = this.FileSystem.File.ReadAllText(filePath);
			if (string.IsNullOrWhiteSpace(value)) return default(T);
			
			return (T)Convert.ChangeType(value, typeof(T));
		}
		
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(TempDirectoryStateConfiguration));
			if (this.Exists(nameof(HomeDirectoryMachineVariable)))
				sb.AppendLine($"- {nameof(HomeDirectoryMachineVariable)} = " + HomeDirectoryMachineVariable);
			if (this.Exists(nameof(OldConfigDirectoryMachineVariable)))
				sb.AppendLine($"- {nameof(OldConfigDirectoryMachineVariable)} = " + OldConfigDirectoryMachineVariable);
			if (this.Exists(nameof(NewConfigDirectoryMachineVariable)))
				sb.AppendLine($"- {nameof(NewConfigDirectoryMachineVariable)} = " + NewConfigDirectoryMachineVariable);
			if (this.Exists(nameof(SeesService)))
				sb.AppendLine($"- {nameof(SeesService)} = " + SeesService);
			if (this.Exists(nameof(ServiceRunning)))
				sb.AppendLine($"- {nameof(ServiceRunning)} = " + ServiceRunning);
			return sb.ToString();
		}
	}
}