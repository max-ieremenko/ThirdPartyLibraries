@{
	RootModule = "ThirdPartyLibraries"

	ModuleVersion = "3.2.1"
	GUID = "{B2A58F03-8986-4A22-B4F5-6E07FAB5DAEE}"

	Author = "Max Ieremenko"
	Copyright = "(C) 2019-2024 Max Ieremenko, licensed under MIT License"

	Description = "
This module helps to manage third party libraries and their licenses in .net applications.
PowerShell versions: core 7.0+.
"

	PowerShellVersion = "7.0"
	CompatiblePSEditions = @("Core")
	ProcessorArchitecture = "None"

	CmdletsToExport = (
		"Update-ThirdPartyLibrariesRepository",
		"Test-ThirdPartyLibrariesRepository",
		"Publish-ThirdPartyNotices",
		"Remove-AppFromThirdPartyLibrariesRepository",
		"Show-ThirdPartyLibrariesInfo"
	)

	AliasesToExport = @("Validate-ThirdPartyLibrariesRepository", "Generate-ThirdPartyNotices")

	PrivateData = @{
		PSData = @{
			Tags = "licenses", "third-party-notice", "third-party-libraries", "nuget-references"
			LicenseUri = "https://github.com/max-ieremenko/ThirdPartyLibraries/blob/master/LICENSE"
			ProjectUri = "https://github.com/max-ieremenko/ThirdPartyLibraries"
			ReleaseNotes = "https://github.com/max-ieremenko/ThirdPartyLibraries/releases/tag/3.2.1"
		}
	 }
}