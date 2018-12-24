namespace AutoSaveManager
{
	using Qml.Net;

	class Program
	{
		private static int Main(string[] args)
		{
			Qt.PutEnv("QT_QUICK_CONTROLS_CONF", System.IO.Directory.GetCurrentDirectory() + "/qml/qtquickcontrols2.conf");
			using (var app = new QGuiApplication(args))
			{
				using (var engine = new QQmlApplicationEngine())
				{
					// Register our new type to be used in Qml
					QmlBridge.RegisterTypes();
					engine.Load("qml/main.qml");
					return app.Exec();
				}
			}
		}
	}
}
