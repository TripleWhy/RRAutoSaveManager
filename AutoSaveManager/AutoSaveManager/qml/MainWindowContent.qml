import QtQuick 2.4
import asm 0.1

MainWindowContentForm {
	id: mwcf
	property QmlBridge bridge
	property SubRoomData currentSubRoom

	Component.onCompleted: {
		roomListView.currentItemChanged.connect(onSelectedRoomChanged);
	}

	onBridgeChanged: {
		bridge.subRoomAdded.connect(updateModel);
		bridge.savePointAdded.connect(onSavePointAdded);
		updateModel();
	}

	onCurrentSubRoomChanged: {
		roomView.subRoomData = currentSubRoom
	}

	roomView.onRoomRenamed: {
		roomListView.currentItem.text = currentSubRoom.displayString
	}

	function updateModel(subRoomId) {
		var m = Net.toListModel(bridge.roomDataList)
		roomListView.model = m
		if (subRoomId >= 0)
			selectSubRoom(subRoomId)

		//roomView.restoreToDialogComboBox.model = m //TODO should work but does not render any text
		roomView.restoreToDialogComboBox.model.clear()
		for (var i = 0; i < roomListView.count; ++i) {
			roomView.restoreToDialogComboBox.model.append({"displayString": m.at(i).displayString, "subRoomId": m.at(i).subRoomId})
		}
	}

	function onSelectedRoomChanged() {
		if (roomListView == null) {
			currentSubRoom = null
			return
		}
		currentSubRoom = roomListView.currentItem.myData.modelData
	}

	function selectSubRoom(subRoomId)
	{
		for (var i = 0; i < roomListView.count; ++i) {
			if (roomListView.model.at(i).subRoomId === subRoomId) {
				roomListView.currentIndex = i
				break
			}
		}
	}

	function onSavePointAdded(subRoomId, timestamp) {
		if (currentSubRoom.subRoomId !== subRoomId)
			selectSubRoom(subRoomId)
		else
			roomView.updateModel()
		roomView.selectSavePoint(timestamp)
	}
}
