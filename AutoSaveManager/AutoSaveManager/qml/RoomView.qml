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
		highlighted: ListView.isCurrentItem
		onClicked: savePointListView.currentIndex = index
	}

	Component.onCompleted: {
		savePointListView.currentItemChanged.connect(onSelectedSavePointChanged)
		noteTextArea.textChanged.connect(onNoteTextAreaTextChanged)
	}

	onSubRoomDataChanged: {
		updateModel()
	}

	onCurrentSavePointChanged: {
		if (currentSavePoint == null) {
			selectedLabel.text = ""
			noteTextArea.text = ""
		}
		else {
			selectedLabel.text = currentSavePoint.displayString
			noteTextArea.text = currentSavePoint.comment
		}
	}

	function updateModel() {
		var m = Net.toListModel(subRoomData.savePoints)
		savePointListView.model = m
	}

	function onSelectedSavePointChanged() {
		if (savePointListView === null || savePointListView.currentItem === null) {
			currentSavePoint = null
			return
		}
		currentSavePoint = savePointListView.currentItem.myData.modelData
		console.log(currentSavePoint)
	}

	function onNoteTextAreaTextChanged() {
		if (currentSavePoint != null)
			currentSavePoint.comment = noteTextArea.text
	}
}
