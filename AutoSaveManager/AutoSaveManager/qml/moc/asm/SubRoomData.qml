import QtQuick 2.0

Item {
	property int subRoomId: 0
	property string subRoomName: ""
	property string displayString: subRoomId + " " + subRoomName
	property list<SavePoint> savePoints: [
		SavePoint{ comment: "foo" },
		SavePoint{ comment: "bar" },
		SavePoint{ }
	]
}
