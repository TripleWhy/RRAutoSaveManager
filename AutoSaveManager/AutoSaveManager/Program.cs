namespace AutoSaveManager
{
	using Qml.Net;

	class Program
	{
		private static int Main(string[] args)
		{
			//System.Environment.SetEnvironmentVariable("QT_QUICK_CONTROLS_CONF", System.IO.Directory.GetCurrentDirectory() + "/qml/qtquickcontrols2.conf");
			System.Console.WriteLine(System.Environment.GetEnvironmentVariable("QT_QUICK_CONTROLS_CONF"));
			using (var app = new QGuiApplication(args))
			{
				using (var engine = new QQmlApplicationEngine())
				{
					// Register our new type to be used in Qml
					QmlBridge.RegisterTypes();
					Qml.RegisterType<QmlBridge>("asm", 0, 1);
					engine.Load("qml/main.qml");
					return app.Exec();
				}
			}
		}
	}
}
