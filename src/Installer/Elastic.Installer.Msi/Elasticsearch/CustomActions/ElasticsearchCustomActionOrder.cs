namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public enum ElasticsearchCustomActionOrder
	{
		// Immediate actions
		LogAllTheThings = 1,

		// Deferred actions
		SetPreconditions = 2,
		InstallStoreTemporaryState = 3,
		InstallPreserveInstall = 4,
		InstallStopServiceAction = 5,
		InstallEnvironment = 6,
		InstallDirectories = 7,
		InstallConfiguration = 8,
		InstallJvmOptions = 9,
		InstallPlugins = 10,
		InstallService = 11,
		InstallStartService = 12,

		// Rollback actions are played in reverse order
		RollbackEnvironment = 1,
		RollbackDirectories = 2,
		RollbackServiceStart = 3,
		RollbackServiceInstall = 4,
	
		// Uninstall actions
		UninstallService = 1,
		UninstallPlugins = 2,
		UninstallDirectories = 3,
		UninstallEnvironment = 4,

		// Commit actons	
		EnsureEnvironmentVariables = 1,
		ElasticsearchEnsureServiceStart = 2,
		CleanupInstall = 3,
	}
}
