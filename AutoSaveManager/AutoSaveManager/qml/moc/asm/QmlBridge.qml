import QtQuick 2.0

Item
{
	signal roomDataChanged()
	property list<SubRoomData> roomDataList: [
		SubRoomData{ subRoomId: 100; subRoomName: "Room0" },
		SubRoomData{ subRoomId: 101; subRoomName: "Room1" },
		SubRoomData{ subRoomId: 102; subRoomName: "Room2" },
		SubRoomData{ subRoomId: 103; subRoomName: "Room3" },
		SubRoomData{ subRoomId: 104; subRoomName: "Room4" }
	]

	function initialize()
	{
		var now = new Date();
		for (var i = 0; i < roomDataList.length; i++)
		{
			var dates = new Array(5)
			for (var j = 0; j < dates.length; j++)
			{
				var date = new Date(now);
				date.setMinutes(date.getMinutes() - (i * roomDataList.length + dates.length - j));
				dates[j] = date;
			}
			roomDataList[i].savePoints = dates;
		}
		roomDataChanged();
	}


	function dispose()
	{
	}
}
