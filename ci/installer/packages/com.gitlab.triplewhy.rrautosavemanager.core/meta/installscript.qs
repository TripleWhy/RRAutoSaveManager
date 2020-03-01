function Component()
{
}

Component.prototype.createOperations = function()
{
	component.createOperations();

	if (systemInfo.productType === "windows")
	{
		component.addOperation(
			"CreateShortcut",
			"@TargetDir@/RRAutosaveManager.exe",
			"@StartMenuDir@/RRAutosaveManager.lnk",
			"workingDirectory=@TargetDir@",
			"iconPath=@TargetDir@/RRAutosaveManager.exe",
			"iconId=0",
			"description=Extended Rec Room autosave management");
	}
}
