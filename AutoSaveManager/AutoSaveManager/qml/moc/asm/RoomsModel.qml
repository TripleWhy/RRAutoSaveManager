import QtQuick 2.0
import QtQml.Models 2.1

ObjectModel
{
	SubRoomData { subRoomId: 200; subRoomName: "bar" }
	SubRoomData { subRoomId: 201; subRoomName: "bar2" }

	function rowCount()
	{
		return count
	}
}
