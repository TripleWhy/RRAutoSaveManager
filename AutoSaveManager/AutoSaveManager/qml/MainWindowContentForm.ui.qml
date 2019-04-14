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
        width: 150
        anchors.leftMargin: 6
        anchors.topMargin: 6
        anchors.bottomMargin: 6
        anchors.bottom: parent.bottom
        anchors.left: parent.left
        anchors.top: parent.top

        ListView {
            id: roomListView
            width: parent.width
            delegate: ItemDelegate {
                property variant myData: model
                text: modelData.displayString
                width: parent.width
                highlighted: ListView.isCurrentItem
                onClicked: roomListView.currentIndex = index
            }
        }

        Item {
            //TODO: since the update to qml.net 0.7.0 (and Qt 5.12) roomListView is not visible somehow without an item here.
            visible: false
        }
    }

    RoomView {
        id: roomView
        anchors.leftMargin: 6
        anchors.right: parent.right
        anchors.rightMargin: 6
        anchors.left: roomScroll.right
        anchors.bottom: parent.bottom
        anchors.bottomMargin: 6
        anchors.top: parent.top
        anchors.topMargin: 6
    }
}
