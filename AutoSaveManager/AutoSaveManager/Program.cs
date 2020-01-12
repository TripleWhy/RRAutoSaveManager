namespace AutoSaveManager
{
	using Qml.Net;
	using Qml.Net.Runtimes;

	class Program
	{
		private static int Main(string[] args)
		{
			RuntimeManager.DiscoverOrDownloadSuitableQtRuntime();
			Qt.PutEnv("QT_QUICK_CONTROLS_CONF", System.IO.Directory.GetCurrentDirectory() + "/qml/qtquickcontrols2.conf");
			QmlNetConfig.ShouldEnsureUIThread = false; // remove this line when fixed in qml.net: temporary workaround for https://github.com/qmlnet/qmlnet/issues/112
			using QGuiApplication app = new QGuiApplication(args);
			using QQmlApplicationEngine engine = new QQmlApplicationEngine();
			// Register our new type to be used in Qml
			QmlBridge.RegisterTypes();
			engine.Load("qml/main.qml");
			return app.Exec();
		}
	}
}
