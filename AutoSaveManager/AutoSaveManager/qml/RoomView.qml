import QtQuick 2.4
import QtQuick.Controls 2.2
import asm 0.1

RoomViewForm {
	property SubRoomData subRoomData
	property var currentSavePoint: null

	visible: subRoomData != null
	subRoomIdLabel.text: subRoomData == null ? null : subRoomData.subRoomId
	subRoomLabelField.text: subRoomData == null ? null : subRoomData.subRoomName

	savePointListView.highlightFollowsCurrentItem: true
	savePointListView.delegate: ItemDelegate {
		property variant myData: model
		text: modelData.displayString
		width: parent.width
		onClicked: savePointListView.currentIndex = index
	}

	Component.onCompleted: {
		savePointListView.currentItemChanged.connect(onSelectedSavePointChanged);
	}

	onSubRoomDataChanged: {
		console.log("RoomViewForm.onSubRoomDataChanged ", currentSubRoom)
		updateModel();
	}

	function updateModel() {
		console.log("RoomViewForm.updateModel0", Object.keys(subRoomData))
		console.log("RoomViewForm.updateModel1", subRoomData, subRoomData.savePoints)
		var m = Net.toListModel(subRoomData.savePoints);
		console.log("RoomViewForm.updateModel2", m)
		savePointListView.model = m;
	}

	function onSelectedSavePointChanged() {
		if (savePointListView === null || savePointListView.currentItem === null) {
			currentSavePoint = null
			return
		}
		currentSavePoint = savePointListView.currentItem.myData.modelData
		console.log(currentSavePoint)
	}
}
