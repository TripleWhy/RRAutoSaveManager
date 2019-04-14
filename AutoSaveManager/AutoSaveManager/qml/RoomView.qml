import QtQuick 2.4
import QtQuick.Controls 2.5
import asm 0.1

RoomViewForm {
	signal roomRenamed(string newName)
	property SubRoomData subRoomData
	property var currentSavePoint: null
	property alias restoreToDialogComboBox: restoreToDialog.targetComboBox

	visible: subRoomData != null
	subRoomIdLabel.text: subRoomData == null ? null : subRoomData.subRoomId
	subRoomLabelField.text: subRoomData == null ? null : subRoomData.subRoomName

	savePointListView.highlightFollowsCurrentItem: true
	savePointListView.delegate: ItemDelegate {
		property variant myData: model
		text: modelData.displayString
		font.bold: modelData.comment
		width: parent.width
		highlighted: ListView.isCurrentItem
		onClicked: savePointListView.currentIndex = index
	}

	RestoreToDialog {
		id: restoreToDialog
		onAccepted: {
			if (restoreToDialogComboBox.currentIndex < 0)
				return
			var targetSubRoomId = restoreToDialogComboBox.model.get(restoreToDialogComboBox.currentIndex).subRoomId
			console.log(subRoomData.subRoomId, targetSubRoomId)
			bridge.restoreSubRoom(subRoomData.subRoomId, currentSavePoint.timestamp, targetSubRoomId)
		}
	}

	Component.onCompleted: {
		savePointListView.currentItemChanged.connect(onSelectedSavePointChanged)
		subRoomLabelField.textChanged.connect(onSubRoomLabelFieldTextChanged)
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

	restoreButton.onClicked: {
		bridge.restoreSubRoom(subRoomData.subRoomId, currentSavePoint.timestamp)
	}

	restoreToButton.onClicked: {
		restoreToDialog.open()
	}

	function updateModel() {
		var m = Net.toListModel(subRoomData.savePoints)
		savePointListView.model = m
	}

	function selectSavePoint(timestamp)
	{
		for (var i = 0; i < savePointListView.count; ++i) {
			if (savePointListView.model.at(i).timestamp === timestamp) {
				savePointListView.currentIndex = i
				break
			}
		}
	}

	function onSelectedSavePointChanged() {
		if (savePointListView === null || savePointListView.currentItem === null) {
			currentSavePoint = null
			return
		}
		currentSavePoint = savePointListView.currentItem.myData.modelData
	}

	function onSubRoomLabelFieldTextChanged() {
		if (currentSubRoom != null) {
			currentSubRoom.subRoomName = subRoomLabelField.text
			roomRenamed(currentSubRoom.subRoomName)
		}
	}

	function onNoteTextAreaTextChanged() {
		if (currentSavePoint != null) {
			currentSavePoint.comment = noteTextArea.text
			//savePointListView.currentItem.font.bold = currentSavePoint.comment //TODO works, but elides the text for some reason
		}
	}
}
