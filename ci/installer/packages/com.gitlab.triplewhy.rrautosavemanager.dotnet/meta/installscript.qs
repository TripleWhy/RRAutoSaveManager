function Component()
{
	component.valueChanged.connect(this, Component.prototype.onComponentValueChanged);
}

Component.prototype.onComponentValueChanged = function(key, value)
{
	if (key !== "UncompressedSizeSum")
		return;

	var components = installer.components();
	for (var i = 0 ; i < components.length ;i++)
	{
		var c = components[i];
		if (c.name === "com.gitlab.triplewhy.rrautosavemanager.config.fde")
		{
			if (value === "0")
			{
				c.setValue("AutoDependOn", "com.gitlab.triplewhy.rrautosavemanager.core");
			}
			else
			{
				c.setValue("AutoDependOn", "");
			}
		}
	}
}
