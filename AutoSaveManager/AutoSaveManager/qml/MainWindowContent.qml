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
		bridge.roomDataChanged.connect(updateModel);
		updateModel();
	}

	onCurrentSubRoomChanged: {
		roomView.subRoomData = currentSubRoom
	}

	function updateModel() {
		var m = Net.toListModel(bridge.roomDataList);
		roomListView.model = m;
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
