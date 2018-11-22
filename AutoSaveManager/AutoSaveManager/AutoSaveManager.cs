namespace AutoSaveManager
{
	using System;
	using System.IO;

	class AutoSaveManager : IDisposable
	{
		private string dataDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RRAutoSaveManager"));
		private string dbFile;
		private string autosaveDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "../LocalLow/Against Gravity/Rec Room/Autosaves"));

		private Storage store;
		private FileSystemWatcher watcher;

		public AutoSaveManager()
		{
			Directory.CreateDirectory(dataDir);
			dbFile = Path.Combine(dataDir, "db.dat");
			store = new Storage(dbFile);
		}

		public void Dispose()
		{
			watcher?.Dispose();
			store.Dispose();
		}

		public void StartWatching()
		{
			if (watcher != null)
				return;
			SnapshotAllFiles(autosaveDir);
			WatchFiles(autosaveDir);
		}

		private void SnapshotAllFiles(string path)
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
		private void WatchFiles(string path)
		{
			watcher = new FileSystemWatcher
			{
				Path = path,
				Filter = "",
				NotifyFilter = NotifyFilters.LastWrite,
			};

			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.Created += new FileSystemEventHandler(OnChanged);
			
			// Begin watching.
			watcher.EnableRaisingEvents = true;
		}

		private void OnChanged(object source, FileSystemEventArgs e)
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
	}
}
