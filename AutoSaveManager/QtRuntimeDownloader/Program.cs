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
			string thisName = Assembly.GetExecutingAssembly().GetName().Name;
			string asmName = "AutoSaveManager";

			RuntimeTarget target = RuntimeTarget.Windows64;
			MethodInfo runtimeTargetToString = typeof(RuntimeManager).GetMethod("RuntimeTargetToString", BindingFlags.Static | BindingFlags.NonPublic);
			string targetString = (string)runtimeTargetToString.Invoke(null, new object[] { target });
			string qtVersion = QmlNetConfig.QtBuildVersion;
			string qtVersionString = $"{qtVersion}-{targetString}";

			string dstPath = RuntimeManager.GetPotentialRuntimesDirectories(RuntimeManager.RuntimeSearchLocation.ExecutableDirectory)[0];
			dstPath = dstPath.Replace(thisName, asmName);
			dstPath = Path.Combine(dstPath, qtVersionString);

			Console.WriteLine($"Downloading Qt to {dstPath}...");
			Directory.CreateDirectory(dstPath);
			RuntimeManager.DownloadRuntimeToDirectory(qtVersion, target, dstPath);
			Directory.Exists(dstPath);
			return File.Exists(Path.Combine(dstPath, "version.txt")) ? 0 : 1;
		}
	}
}
