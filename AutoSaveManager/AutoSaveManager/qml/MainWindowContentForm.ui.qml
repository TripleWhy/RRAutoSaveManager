import QtQuick 2.4
import QtQuick.Controls 2.2

Item {
    id: item2
    width: 400
    height: 400
    property alias roomView: roomView
    property alias roomListView: roomListView

    ScrollView {
        id: roomScroll
        width: 200
        anchors.bottom: parent.bottom
        anchors.left: parent.left
        anchors.top: parent.top

        ListView {
            id: roomListView
            width: parent.width
            delegate: ItemDelegate {
                id: fooDelegate
                property variant myData: model
                text: modelData.subRoomId + " " + modelData.subRoomName
                width: parent.width
                onClicked: roomListView.currentIndex = index
            }
        }

        Item {
            //TODO: since the update to qml.net 0.7.0 (and Qt 5.12) roomListView is not visible somehow without an item here.
            id: rectangle
            visible: false
        }
    }

    RoomView {
        id: roomView
        anchors.leftMargin: 0
        anchors.right: parent.right
        anchors.rightMargin: 0
        anchors.left: roomScroll.right
        anchors.bottom: parent.bottom
        anchors.bottomMargin: 0
        anchors.top: parent.top
        anchors.topMargin: 0
    }
}




/*##^## Designer {
    D{i:2;anchors_width:400;invisible:true}
}
 ##^##*/
