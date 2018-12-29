import QtQuick 2.4
import asm 0.1

MainWindowContentForm {
	id: mwcf
	property QmlBridge bridge
	property SubRoomData currentSubRoom

	Component.onCompleted: {
		console.log("MainWindowContentForm.onCompleted");
		roomListView.currentItemChanged.connect(onSelectedRoomChanged);
	}

	onBridgeChanged: {
		bridge.subRoomAdded.connect(updateModel);
		updateModel();
	}

	onCurrentSubRoomChanged: {
		roomView.subRoomData = currentSubRoom
	}

	function updateModel() {
		roomListView.model = Net.toListModel(bridge.roomDataList);
		console.log("view.model: ", roomListView.model.rowCount());
	}

	function onSelectedRoomChanged() {
		if (roomListView == null) {
			currentSubRoom = null
			return
		}
		currentSubRoom = roomListView.currentItem.myData.modelData
		console.log("onSelectedRoomChanged", currentSubRoom)
	}
}
