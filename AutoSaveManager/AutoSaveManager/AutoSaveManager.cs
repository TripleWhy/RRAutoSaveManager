namespace AutoSaveManager
{
	using System;
	using System.Collections;
	using System.IO;

	class AutoSaveManager : IDisposable
	{
		private readonly string dataDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RRAutoSaveManager"));
		private readonly string dbFile;
		private readonly string autosaveDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "../LocalLow/Against Gravity/Rec Room/Autosaves"));

		public Storage Store { get; }
		private FileSystemWatcher watcher;

		public AutoSaveManager()
		{
			Directory.CreateDirectory(dataDir);
			dbFile = Path.Combine(dataDir, "db.dat");
			Store = new Storage(dbFile);
		}

		public void Dispose()
		{
			watcher?.Dispose();
			Store.Dispose();
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
			Directory.CreateDirectory(path);
			foreach (string file in Directory.EnumerateFileSystemEntries(path))
				SnapshotFile(file);
		}

		static public bool DataEquals(byte[] a1, byte[] b1)
		{
			return ((a1 == null) == (b1 == null))
				&& (object.ReferenceEquals(a1, b1)
					|| ((IStructuralEquatable)a1).Equals(b1, StructuralComparisons.StructuralEqualityComparer));
		}

		long lastSavedSubRoomId = -1;
		byte[] lastSavedData;
		public void SnapshotFile(string file)
		{
			string filename = Path.GetFileName(file);
			long subRoomId = long.Parse(filename);
			DateTime timestamp = File.GetLastWriteTimeUtc(file);
			byte[] data = File.ReadAllBytes(file);

			if (subRoomId != lastSavedSubRoomId)
				lastSavedData = Store.FetchLatestSnapshot(subRoomId, out DateTime storedTimestamp);
			lastSavedSubRoomId = subRoomId;

			if (DataEquals(data, lastSavedData))
				return;
			lastSavedData = data;
			Store.StoreSnapshot(subRoomId, timestamp, data);
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
