import QtQuick 2.4
import QtQuick.Controls 2.2
import QtQuick.Layouts 1.3

Item {
    id: roomView
    property alias subRoomIdLabel: subRoomIdLabel
    property alias subRoomLabelField: subRoomLabelField
    property alias savePointListView: savePointListView
	property alias selectedLabel: selectedLabel

    RowLayout {
        id: topRow
        Label {
            id: label
            text: qsTr("Sub Room Id:")
        }

        Label {
            id: subRoomIdLabel
            text: "0"
        }

        Label {
            id: label2
            text: qsTr("Label:")
        }

        TextField {
            id: subRoomLabelField
            text: qsTr("0")
        }
    }

    ScrollView {
        id: scrollView
        width: 300
        anchors.topMargin: 6
        anchors.top: topRow.bottom
        anchors.bottom: parent.bottom
        anchors.left: parent.left

        ListView {
            id: savePointListView
        }

        Item {
            //TODO: since the update to qml.net 0.7.0 (and Qt 5.12) roomListView is not visible somehow without an item here.
            visible: false
        }
    }

    Label {
        id: selectedLabel
        anchors.rightMargin: 6
        anchors.leftMargin: 6
        anchors.top: topRow.bottom
        anchors.right: parent.right
        anchors.left: scrollView.right
        anchors.topMargin: 6
    }

    Button {
        id: restoreButton
        text: qsTr("Restore")
        anchors.rightMargin: 6
        anchors.leftMargin: 6
        anchors.top: selectedLabel.bottom
        anchors.right: parent.right
        anchors.left: scrollView.right
        anchors.topMargin: 6
    }

    Label {
        id: noteLabel
        text: qsTr("Notes:")
        visible: true
        anchors.rightMargin: 6
        anchors.leftMargin: 6
        anchors.top: restoreButton.bottom
        anchors.right: parent.right
        anchors.left: scrollView.right
        anchors.topMargin: 6
    }

    TextArea {
        id: noteTextArea
        placeholderText: "Notes"
        anchors.bottomMargin: 6
        anchors.rightMargin: 6
        anchors.leftMargin: 6
        anchors.top: noteLabel.bottom
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.left: scrollView.right
        anchors.topMargin: 6
    }
}




/*##^## Designer {
    D{i:0;autoSize:true;height:480;width:640}D{i:9;anchors_x:402}D{i:11;anchors_x:505;anchors_y:79}
}
 ##^##*/
