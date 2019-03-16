var component;

function toListModel(list)
{
	console.log(list);
	if (Array.isArray(list) || typeof(list) === "undefined" || list === null)
		return list
	if (typeof(list.length) === "number")
	{
		var model = new Array(list.length);
		for (var i = 0; i < list.length; i++)
			model[i] = list[i];
		return model;
	}
	return null;

//	if(typeof(component) == "undefined")
//		component = Qt.createComponent("SubRoomData.qml");

//	var now = new Date();
//	var model = new Array(list.length)
//	for (var i = 0; i < model.length; i++)
//	{
//		var dates = new Array(5);
//		for (var j = 0; j < dates.length; j++)
//		{
//			var date = new Date(now);
//			date.setMinutes(date.getMinutes() - (i * model.length + dates.length - j));
//			dates[j] = date;
//		}

//		var srd = component.createObject(null, {"subRoomId": 100 + i, "subRoomName": "Room" + i, "savePoints": dates});
//		console.log(i, srd.savePoints);
//		model[i] = srd;
//	}

//	model.rowCount = function()
//	{
//		return this.length;
//	}

//	return model;
}
