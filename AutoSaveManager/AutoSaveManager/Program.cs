namespace AutoSaveManager
{
	using System;
	using System.IO;

	class Program : IDisposable
	{
		private string dataDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RRAutoSaveManager"));
		private string dbFile;
		private string autosaveDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "../LocalLow/Against Gravity/Rec Room/Autosaves"));

		private Storage store;
		FileSystemWatcher watcher;

		public Program()
		{
			dbFile = Path.Combine(dataDir, "db.dat");
			Console.WriteLine(autosaveDir);

			Directory.CreateDirectory(dataDir);
			store = new Storage(dbFile);

			SnapshotAllFiles(autosaveDir);
			WatchFiles(autosaveDir);
		}

		public void Dispose()
		{
			watcher.Dispose();
			store.Dispose();
		}

		void SnapshotAllFiles(string path)
		{
			foreach (string file in Directory.EnumerateFileSystemEntries(path))
				SnapshotFile(file);
		}

		long lastSavedSubRoomId = -1;
		byte[] lastSavedData;
		public void SnapshotFile(string file)
		{
			string filename = Path.GetFileName(file);
			long subRoomId = long.Parse(filename);
			long timestamp = File.GetLastWriteTimeUtc(file).Ticks;
			byte[] data = File.ReadAllBytes(file);
			if (subRoomId == lastSavedSubRoomId && (object.ReferenceEquals(data, lastSavedData) || data == lastSavedData))
				return;
			lastSavedSubRoomId = subRoomId;
			lastSavedData = data;
			store.StoreSnapshot(subRoomId, timestamp, data);
		}

		//[PermissionSet(SecurityAction.Demand, Name="FullTrust")]
		void WatchFiles(string path)
		{
			// Create a new FileSystemWatcher and set its properties.
			watcher = new FileSystemWatcher
			{
				Path = path,
				Filter = "",
				NotifyFilter = NotifyFilters.LastWrite,
			};

			// Add event handlers.
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.Created += new FileSystemEventHandler(OnChanged);
			
			// Begin watching.
			watcher.EnableRaisingEvents = true;
		}

		// Define the event handlers.
		void OnChanged(object source, FileSystemEventArgs e)
		{
			Console.WriteLine("File: " +  e.FullPath + " " + e.ChangeType);
			try
			{
				SnapshotFile(e.FullPath);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("File: " + e.FullPath + " update failed: " + ex);
			}
		}

		private static void Main(string[] args)
		{
			using (Program p = new Program())
			{
				Console.WriteLine("Press \'q\' to quit.");
				while(Console.Read()!='q');
			}
		}
	}
}
