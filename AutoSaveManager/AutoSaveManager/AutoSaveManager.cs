namespace AutoSaveManager
{
	using Google.Protobuf;
	using System;
	using System.Collections;
	using System.IO;
	using System.Reflection;

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
		private static readonly MessageParser<Autosave> autosaveParser = Autosave.Parser.WithDiscardUnknownFields(false);
		private const int hashSize = 32;

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
			latestAutosaveDir = watchDatas[^1].path;
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
				SnapshotAllFiles(watchData);
			WatchFiles(watchDatas[^1]);
		}

		private void SnapshotAllFiles(WatchData watchData)
		{
			Directory.CreateDirectory(watchData.path);
			foreach (string file in Directory.EnumerateFileSystemEntries(watchData.path))
				SnapshotFile(file, watchData.autosaveFormatVersion);
		}

		long lastSavedSubRoomId = -1;
		ArraySegment<byte> lastSavedData;
		long lastSavedFormatVersion = -1;
		DateTime lastRestoreTime;
		long lastRestoredSubRoomId = -1;
		public void SnapshotFile(string file, long autosaveFormatVersion)
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
				lastSavedData = Store.FetchLatestSnapshotContentBytes(subRoomId, out _, out lastSavedFormatVersion);
			lastSavedSubRoomId = subRoomId;
			if (lastSavedFormatVersion > autosaveFormatVersion)
				return;

			ArraySegment<byte> content = SnapshotContentBtytes(data, autosaveFormatVersion);
			if (SnapshotsEqual(content, lastSavedData))
				return;
			lastSavedData = content;
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

			FileSystemEventHandler fseh = (object source, FileSystemEventArgs e) => OnChanged(e, watchData);
			watcher.Changed += new FileSystemEventHandler(fseh);
			watcher.Created += new FileSystemEventHandler(fseh);
			
			// Begin watching.
			watcher.EnableRaisingEvents = true;
		}

		private void OnChanged(FileSystemEventArgs e, WatchData watchData)
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

		public void RestoreSubRoom(long srcSubRoomId, DateTime timestamp, long dstSubRoomId)
		{
			byte[] snapshot = Store.FetchSnapshot(srcSubRoomId, timestamp, out long autosaveFormatVersion);
			lastRestoreTime = DateTime.UtcNow;
			lastRestoredSubRoomId = dstSubRoomId;

			string filePath = Path.Combine(latestAutosaveDir, dstSubRoomId.ToString());
			if (autosaveFormatVersion == Storage.autosaveFormatVersion && dstSubRoomId == srcSubRoomId)
			{
				File.WriteAllBytes(filePath, snapshot);
			}
			else
			{
				ArraySegment<byte> fileData = SnapshotContentBtytes(snapshot, autosaveFormatVersion);
				UpdateSubroomId(ref fileData, dstSubRoomId);
				AutosaveFromContent(ref fileData);
				File.WriteAllBytes(filePath, fileData.ToArray());
			}
		}

		static public ArraySegment<byte> SnapshotContentBtytes(byte[] data, long autosaveFormatVersion)
		{
			switch (autosaveFormatVersion)
			{
				case 1:
					return data;
				case Storage.autosaveFormatVersion:
					return new ArraySegment<byte>(data, hashSize, data.Length - hashSize);
				default:
					throw new InvalidOperationException("Autosave format version " + autosaveFormatVersion + " unknown.");
			};
		}

		static private void AutosaveFromContent(ref ArraySegment<byte> data)
		{
			byte[] hashValue;
			using (System.Security.Cryptography.SHA256 hasher = System.Security.Cryptography.SHA256.Create())
				hashValue = hasher.ComputeHash(data.Array, data.Offset, data.Count);
			if (data.Offset >= hashValue.Length)
			{
				data = new ArraySegment<byte>(data.Array, data.Offset - hashValue.Length, data.Count + hashValue.Length);
				hashValue.CopyTo(data.Array, data.Offset);
			}
			else
			{
				byte[] result = new byte[data.Count + hashValue.Length];
				hashValue.CopyTo(result, 0);
				data.CopyTo(result, hashValue.Length);
				data = result;
			}
		}

		static private void UpdateSubroomId(ref ArraySegment<byte> data, long dstSubRoomId)
		{
			Autosave autosave = autosaveParser.ParseFrom(data.Array, data.Offset, data.Count);
			autosave.SubroomId = dstSubRoomId;

			int messageSize = autosave.CalculateSize();
			int autsaveSize = messageSize + hashSize;
			if (data.Array.Length >= autsaveSize)
				data = new ArraySegment<byte>(data.Array, hashSize, messageSize);
			else
				data = new ArraySegment<byte>(new byte[autsaveSize], hashSize, messageSize);
			ConstructorInfo offsetConstructor = typeof(CodedOutputStream).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(byte[]), typeof(int), typeof(int) }, null); //constructr is private for some reason
			CodedOutputStream output = (CodedOutputStream)offsetConstructor.Invoke(new object[] { data.Array, data.Offset, data.Count });
			autosave.WriteTo(output);
			output.CheckNoSpaceLeft();
		}

		static public bool SnapshotsEqual(byte[] a, long autosaveFormatVersionA, byte[] b, long autosaveFormatVersionB)
		{
			return SnapshotsEqual(SnapshotContentBtytes(a, autosaveFormatVersionA), SnapshotContentBtytes(b, autosaveFormatVersionB));
		}

		static public bool SnapshotsEqual(ArraySegment<byte> contentBytesA, ArraySegment<byte> contentBytesB)
		{
			return ((contentBytesA.Count == 0) == (contentBytesB.Count == 0))
				&& (contentBytesA.Equals(contentBytesB)
					|| contentBytesA.AsSpan().SequenceEqual(contentBytesB.AsSpan())
					|| ProtosEqual(contentBytesA, contentBytesB));
		}

		static public bool BytesEqual(byte[] a, byte[] b)
		{
			return ((a is null) == (b is null))
				&& (object.ReferenceEquals(a, b)
					|| ((IStructuralEquatable)a).Equals(b, StructuralComparisons.StructuralEqualityComparer));
		}

		static public bool ProtosEqual(ArraySegment<byte> a, ArraySegment<byte> b)
		{
			Autosave autosaveA = autosaveParser.ParseFrom(a.Array, a.Offset, a.Count);
			Autosave autosaveB = autosaveParser.ParseFrom(b.Array, b.Offset, b.Count);
			autosaveA.SubroomId = autosaveB.SubroomId = 0;
			autosaveA.Timestamp = autosaveB.Timestamp = null;
			return autosaveA.Equals(autosaveB);
		}
	}
}
