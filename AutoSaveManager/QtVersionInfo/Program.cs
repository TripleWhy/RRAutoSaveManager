namespace QtRuntimeDownloader
{
	using Qml.Net;
	using Qml.Net.Runtimes;
	using System;
	using System.IO;
	using System.Reflection;

	class Program
	{
		static int Main(string[] args)
		{
			File.WriteAllText("dotnetversion.txt", System.Environment.Version.ToString());

			RuntimeTarget target = RuntimeTarget.Windows64;
			MethodInfo runtimeTargetToString = typeof(RuntimeManager).GetMethod("RuntimeTargetToString", BindingFlags.Static | BindingFlags.NonPublic);
			string targetString = (string)runtimeTargetToString.Invoke(null, new object[] { target });
			string qtVersion = QmlNetConfig.QtBuildVersion;
			string qtVersionString = $"{qtVersion}-{targetString}";
			File.WriteAllText("qtversion.txt", qtVersionString);

			return 0;
		}
	}
}
