function Component()
{
}

Component.prototype.createOperations = function()
{
	component.createOperations();
	if (installer.value("os") === "win")
	{
		var redistPath = "@TargetDir@/vc_redist.x64.exe"
		component.addElevatedOperation("Execute", redistPath, "/quiet", "/norestart")
	}
	component.addOperation("Delete", redistPath)
}
