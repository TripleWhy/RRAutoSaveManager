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
    }

    RoomView {
        id: roomView
    }
}
