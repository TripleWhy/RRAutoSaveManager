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
		DateTime lastRestoreTime;
		long lastRestoredSubRoomId = -1;
		public void SnapshotFile(string file)
		{
			string filename = Path.GetFileName(file);
			long subRoomId = long.Parse(filename);

			if (lastRestoredSubRoomId == subRoomId && (DateTime.UtcNow - lastRestoreTime).TotalSeconds < 2)
				return;

			DateTime? timestamp = null;
			byte[] data = null;
			for (int i = 0; ; i++)
			{
				try
				{
					timestamp = File.GetLastWriteTimeUtc(file);
					data = File.ReadAllBytes(file);
					break;
				}
				catch (IOException e)
				{
					if (i >= 5 || e.HResult != unchecked((int)0x80070020))
					{
						Console.WriteLine("Failed to read file " + file + ", giving up after " + (i+1) + " attempts:");
						Console.WriteLine(e);
						break;
					}
					System.Threading.Thread.Sleep(500);
				}
			}

			if (subRoomId != lastSavedSubRoomId)
				lastSavedData = Store.FetchLatestSnapshot(subRoomId, out DateTime storedTimestamp);
			lastSavedSubRoomId = subRoomId;

			if (DataEquals(data, lastSavedData))
				return;
			lastSavedData = data;
			Store.StoreSnapshot(subRoomId, timestamp.Value, null, data);
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

		public void RestoreSubRoom(long subRoomId, DateTime timestamp)
		{
			byte[] snapshot = Store.FetchSnapshot(subRoomId, timestamp);
			lastRestoreTime = DateTime.UtcNow;
			lastRestoredSubRoomId = subRoomId;
			File.WriteAllBytes(autosaveDir + "/" + subRoomId, snapshot);
		}
	}
}
