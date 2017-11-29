namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public enum ElasticsearchCustomActionOrder
	{
		// Immediate actions
		LogAllTheThings = 1,
		BootstrapPasswordProperty = 2,
		ServiceParameters = 3,

		// Deferred actions
		InstallPreserveInstall = 1,
		InstallDirectories = 2,
		InstallConfiguration = 3,
		InstallJvmOptions = 4,
		InstallPlugins = 5,
		BootstrapPassword = 6,
		ServiceStartType = 7,
		SetupXPackPasswords = 8,

		// Rollback actions are played in reverse order
		RollbackDirectories = 1,
	
		// Uninstall actions
		UninstallDirectories = 1,

		// Commit actons
		CleanupInstall = 1,
	}
}
