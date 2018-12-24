import QtQuick 2.4
import asm 0.1

RoomViewForm {
	property SubRoomData subRoomData

	visible: subRoomData != null
	subRoomIdLabel.text: subRoomData == null ? null : subRoomData.subRoomId
	subRoomLabelField.text: subRoomData == null ? null : subRoomData.subRoomName
}
