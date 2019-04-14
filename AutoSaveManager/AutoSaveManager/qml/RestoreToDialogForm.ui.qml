import QtQuick 2.4
import QtQuick.Controls 2.5

Dialog {
    property alias targetComboBox: targetComboBox
    id: dialog
    width: 300
    height: 150
    padding: 6
    rightPadding: 6
    leftPadding: 6
    bottomPadding: 6
    topPadding: 6
    title: "Restore to"
    dim: true
    modal: true
    standardButtons: Dialog.Ok | Dialog.Cancel

    ComboBox {
        id: targetComboBox
        anchors.topMargin: 6
        anchors.right: parent.right
        anchors.left: parent.left
        anchors.top: label.bottom
        textRole: "displayString"
        model: ListModel {
        }
    }

    Label {
        id: label
        text: "Select sub room to restore this save to:"
        anchors.right: parent.right
        anchors.left: parent.left
        anchors.top: parent.top
    }
}




/*##^## Designer {
    D{i:1;anchors_x:74;anchors_y:71}D{i:3;anchors_x:41;anchors_y:23}
}
 ##^##*/
