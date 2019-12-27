namespace AutoSaveManager
{
	using System;
	using System.Collections;
	using System.IO;

	class AutoSaveManager : IDisposable
	{
		private class WatchData
		{
			public string path;
			public long autosaveFormatVersion;
		}

		private readonly string dataDir;
		private readonly string dbFile;
		private readonly WatchData[] watchDatas;
		private readonly string latestAutosaveDir;

		public Storage Store { get; }
		private FileSystemWatcher watcher;

		public AutoSaveManager()
		{
			string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appDataLocalLow = Path.GetFullPath(Path.Combine(appDataLocal, "../LocalLow/"));

			dataDir = Path.GetFullPath(Path.Combine(appDataLocal, "RRAutoSaveManager"));
			Directory.CreateDirectory(dataDir);
			dbFile = Path.Combine(dataDir, "db.dat");
			Store = new Storage(dbFile);

			watchDatas = new WatchData[]
			{
				new WatchData{ path = Path.GetFullPath(Path.Combine(appDataLocalLow, "Against Gravity/Rec Room/Autosaves")), autosaveFormatVersion = 1 },
				new WatchData{ path = Path.GetFullPath(Path.Combine(appDataLocalLow, "Against Gravity/Rec Room/AutosavesV2")), autosaveFormatVersion = 2 },
			};
			latestAutosaveDir = watchDatas[watchDatas.Length - 1].path;
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
			foreach (WatchData watchData in watchDatas)
			{
				SnapshotAllFiles(watchData);
				WatchFiles(watchData);
			}
		}

		private void SnapshotAllFiles(WatchData watchData)
		{
			Directory.CreateDirectory(watchData.path);
			foreach (string file in Directory.EnumerateFileSystemEntries(watchData.path))
				SnapshotFile(file, watchData.autosaveFormatVersion);
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
		public async void SnapshotFile(string file, long autosaveFormatVersion)
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
				lastSavedData = (await Store.FetchLatestSnapshotAsync(subRoomId)).data;
			lastSavedSubRoomId = subRoomId;

			if (DataEquals(data, lastSavedData))
				return;
			lastSavedData = data;
			Store.StoreSnapshot(subRoomId, timestamp.Value, null, data, autosaveFormatVersion);
		}

		//[PermissionSet(SecurityAction.Demand, Name="FullTrust")]
		private void WatchFiles(WatchData watchData)
		{
			watcher = new FileSystemWatcher
			{
				Path = watchData.path,
				Filter = "",
				NotifyFilter = NotifyFilters.LastWrite,
			};

			FileSystemEventHandler fseh = (object source, FileSystemEventArgs e) => OnChanged(source, e, watchData);
			watcher.Changed += new FileSystemEventHandler(fseh);
			watcher.Created += new FileSystemEventHandler(fseh);
			
			// Begin watching.
			watcher.EnableRaisingEvents = true;
		}

		private void OnChanged(object source, FileSystemEventArgs e, WatchData watchData)
		{
			Console.WriteLine("File: " +  e.FullPath + " " + e.ChangeType);
			try
			{
				SnapshotFile(e.FullPath, watchData.autosaveFormatVersion);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("File: " + e.FullPath + " update failed: " + ex);
			}
		}

		public void RestoreSubRoom(long subRoomId, DateTime timestamp)
		{
			RestoreSubRoom(subRoomId, timestamp, subRoomId);
		}

		public async void RestoreSubRoom(long srcSubRoomId, DateTime timestamp, long dstSubRoomId)
		{
			byte[] snapshot = await Store.FetchSnapshotAsync(srcSubRoomId, timestamp);
			lastRestoreTime = DateTime.UtcNow;
			lastRestoredSubRoomId = dstSubRoomId;
			File.WriteAllBytes(Path.Combine(latestAutosaveDir, dstSubRoomId.ToString()), snapshot);
		}
	}
}
