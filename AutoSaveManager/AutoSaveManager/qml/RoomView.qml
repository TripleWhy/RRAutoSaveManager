import QtQuick 2.4
import asm 0.1

RoomViewForm {
	property SubRoomData subRoomData

	subRoomIdLabel.text: subRoomData.subRoomId
	subRoomLabelField.text: subRoomData.subRoomName
}
