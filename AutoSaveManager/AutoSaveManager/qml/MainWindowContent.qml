import QtQuick 2.4
import asm 0.1

MainWindowContentForm {
	id: mwcf
	property QmlBridge bridge
	property SubRoomData currentSubRoom

	Component.onCompleted: {
		roomListView.currentItemChanged.connect(onSelectedRoomChanged);
		bridge.subRoomNameChanged.connect(onSubRoomNameChanged); //workaround, this should not be necessary
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
	}

	function onSubRoomNameChanged(subRoomId, newName) {
		if (roomListView.currentItem.myData.modelData.subRoomId === subRoomId) {
			roomListView.currentItem.text = roomListView.currentItem.myData.modelData.subRoomId + " " + roomListView.currentItem.myData.modelData.subRoomName
		}
		else {
			for(var child in roomListView.contentItem.children) {
				var delegate = roomListView.contentItem.children[child]
				var myData = delegate.myData
				if (myData) {
					var srd = myData.modelData
					if (srd.subRoomId === subRoomId) {
						delegate.text = srd.subRoomId + " " + srd.subRoomName
						break;
					}
				}
			}
		}
	}
}
