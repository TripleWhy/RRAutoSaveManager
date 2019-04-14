import QtQuick 2.0

Item
{
	signal subRoomAdded(int subRoomId)
	signal savePointAdded(int subRoomId, var timestamp)
	property list<SubRoomData> roomDataList: [
		SubRoomData{ subRoomId: 100; subRoomName: "Room0"; savePoints: [SavePoint{comment: "foo"}, SavePoint{comment: "bar"}] },
		SubRoomData{ subRoomId: 101; subRoomName: "Room1"; savePoints: [SavePoint{}] },
		SubRoomData{ subRoomId: 102; subRoomName: "Room2"; savePoints: [SavePoint{}, SavePoint{}, SavePoint{}] },
		SubRoomData{ subRoomId: 103; subRoomName: "Room3"; savePoints: [SavePoint{}, SavePoint{}] },
		SubRoomData{ subRoomId: 104; subRoomName: "Room4"; savePoints: [SavePoint{}, SavePoint{}] }
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
		subRoomAdded(-1);
	}


	function dispose()
	{
	}
}
