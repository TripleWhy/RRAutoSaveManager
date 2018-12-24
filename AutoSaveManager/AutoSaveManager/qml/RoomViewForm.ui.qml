import QtQuick 2.4
import QtQuick.Controls 2.2
import QtQuick.Layouts 1.3

Item {
    id: roomView
    property alias subRoomIdLabel: subRoomIdLabel
    property alias subRoomLabelField: subRoomLabelField

    RowLayout {
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
}




/*##^## Designer {
    D{i:0;autoSize:true;height:480;width:640}
}
 ##^##*/
