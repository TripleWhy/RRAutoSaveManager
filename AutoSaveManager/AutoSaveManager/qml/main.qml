import QtQuick 2.9
import QtQuick.Controls 2.2
import asm 0.1

ApplicationWindow {
	visible: true
	width: 640
	height: 480
	title: qsTr("Rec Room Auto Save Manager")

	QmlBridge {
		id: bridge
		Component.onCompleted: {
			initialize();
		}
		Component.onDestruction: {
			dispose();
		}
	}

	MainWindowContent {
		anchors.fill: parent
		bridge: bridge
	}
}
