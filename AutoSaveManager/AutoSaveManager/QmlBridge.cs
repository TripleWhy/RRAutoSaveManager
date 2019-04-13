namespace AutoSaveManager
{
	using Qml.Net;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Timers;

	[Signal("subRoomAdded", NetVariantType.Int)]
	[Signal("savePointAdded", NetVariantType.Int, NetVariantType.DateTime)]
	[Signal("subRoomNameChanged", NetVariantType.Int, NetVariantType.Object)]
	class QmlBridge : IDisposable
	{
		public class SubRoomData
		{
			public class SavePoint
			{
				public delegate void StoreCommentCallback(string comment);
				public StoreCommentCallback StoreComment;

				public DateTime Timestamp { get; set; }
				public string DisplayString { get; set; }
				private string comment;
				private Timer commentStoreTimer;

				public SavePoint(DateTime timestamp, String comment, StoreCommentCallback storeCommentCallback)
				{
					Timestamp = timestamp;
					DisplayString = SavePointDisplayString(timestamp);
					this.comment = comment;
					StoreComment = storeCommentCallback;
				}

				[NotifySignal]
				public string Comment
				{
					get => comment;
					set
					{
						if (value == comment)
							return;
						comment = value;
						StartCommentStoreTimer();
						this.ActivateSignal("commentChanged", comment);
					}
				}

				private void StartCommentStoreTimer()
				{
					if (commentStoreTimer == null)
					{
						commentStoreTimer = new Timer(1000);
						commentStoreTimer.Elapsed += CommentStoreTimer_Elapsed;
						commentStoreTimer.AutoReset = false;
					}
					commentStoreTimer.Stop();
					commentStoreTimer.Start();
				}

				private void CommentStoreTimer_Elapsed(object sender, ElapsedEventArgs e)
				{
					Timer t = commentStoreTimer;
					commentStoreTimer = null;
					t.Dispose();

					StoreComment(Comment);
				}
			}

			public SubRoomData(long subRoomId, string subRoomName, List<SavePoint> savePoints, StoreNameCallback storeNameCallback)
			{
				this.SubRoomId = subRoomId;
				this.subRoomName = subRoomName;
				this.savePoints = savePoints;
				this.StoreName = storeNameCallback;
			}
			public SubRoomData(long subRoomId, string subRoomName, LoadSavePointsCallback loadSavePointsCallback, StoreNameCallback storeNameCallback)
			{
				this.SubRoomId = subRoomId;
				this.subRoomName = subRoomName;
				this.LoadSavePoints = loadSavePointsCallback;
				this.StoreName = storeNameCallback;
			}

			public long SubRoomId { get; set; }
			private string subRoomName;
			private Timer nameStoreTimer;

			public delegate void StoreNameCallback(string comment);
			public StoreNameCallback StoreName;

			[NotifySignal]
			public string SubRoomName
			{
				get => subRoomName;
				set
				{
					string val = string.IsNullOrEmpty(value) ? null : value;
					if (val == subRoomName)
						return;
					subRoomName = val;
					StartNameStoreTimer();
					RaiseSubRoomNameChanged();
				}
			}

			private void StartNameStoreTimer()
			{
				if (nameStoreTimer == null)
				{
					nameStoreTimer = new Timer(1000);
					nameStoreTimer.Elapsed += NameStoreTimer_Elapsed;
					nameStoreTimer.AutoReset = false;
				}
				nameStoreTimer.Stop();
				nameStoreTimer.Start();
			}

			private void NameStoreTimer_Elapsed(object sender, ElapsedEventArgs e)
			{
				Timer t = nameStoreTimer;
				nameStoreTimer = null;
				t.Dispose();

				StoreName(subRoomName);
			}

			public delegate void LoadSavePointsCallback(long subRoomId);
			public LoadSavePointsCallback LoadSavePoints;

			private List<SavePoint> savePoints;
			public bool IsSavePointsInitialized { get => savePoints != null; }
			public List<SavePoint> SavePoints
			{
				get
				{
					if (savePoints == null && LoadSavePoints != null)
					{
						LoadSavePoints(SubRoomId);
						Debug.Assert(savePoints != null);
					}
					return savePoints;
				}
				set => savePoints = value;
			}

			private void RaiseSubRoomNameChanged()
			{
				this.ActivateSignal("subRoomNameChanged", subRoomName ?? "");
			}

			public void TestCallback()
			{
				Console.WriteLine(this + " " + GetHashCode() + " TestCallback");
			}
		}

		private AutoSaveManager asm;

		[NotifySignal]
		public SortedDictionary<long, SubRoomData> RoomData { get; }
		public List<SubRoomData> RoomDataList { get => new List<SubRoomData>(RoomData.Values); }

		public QmlBridge()
		{
			RoomData = new SortedDictionary<long, SubRoomData>();
		}

		~QmlBridge()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (asm == null)
				return;
			asm.Dispose();
			asm = null;
		}

		public static void RegisterTypes()
		{
			Qml.RegisterType<QmlBridge>("asm", 0, 1);
			Qml.RegisterType<SubRoomData>("asm", 0, 1);
		}

		private SubRoomData CreateSubRoomData(long subRoomId, string subRoomName, List<SubRoomData.SavePoint> savePoints)
		{
			if (savePoints != null)
				return new SubRoomData(subRoomId, subRoomName, savePoints, (string name) => StoreSubRoomName(subRoomId, name));
			else
				return new SubRoomData(subRoomId, subRoomName, LoadSavePoints, (string name) => StoreSubRoomName(subRoomId, name));
		}

		public void Initialize()
		{
			if (asm != null)
				return;
			asm = new AutoSaveManager();
			asm.StartWatching();

			RoomData.Clear();
			foreach (Storage.RoomAndName ram in asm.Store.FetchSubRoomIdsWithNames())
				RoomData.Add(ram.subRoomId, CreateSubRoomData(ram.subRoomId, ram.subRoomName, null));
			asm.Store.SnapshotStored += Store_SnapshotStored;

			RaiseSubRoomAdded(-1);
		}

		private SubRoomData.SavePoint CreateSavePoint(long subRoomId, DateTime timestamp, string comment)
		{
			return new SubRoomData.SavePoint(timestamp, comment, (string newComment) => StoreSavePointComment(subRoomId, timestamp, newComment));
		}

		private void LoadSavePoints(long subRoomId)
		{
			Debug.Assert(RoomData.ContainsKey(subRoomId));
			List<SubRoomData.SavePoint> savePoints = new List<SubRoomData.SavePoint>();
			foreach (Storage.SavePointData spd in asm.Store.FetchTimestamps(subRoomId))
				savePoints.Add(CreateSavePoint(subRoomId, spd.timestamp, spd.comment));
			RoomData[subRoomId].SavePoints = savePoints;
			RaiseSavePointAdded(subRoomId, null);
		}

		private void StoreSubRoomName(long subRoomId, string subRoomName)
		{
			Debug.Assert(RoomData.ContainsKey(subRoomId));
			asm.Store.StoreSubRoomName(subRoomId, subRoomName);
			this.ActivateSignal("subRoomNameChanged", subRoomId, subRoomName); //workaround, this should not be necessary
		}

		private void StoreSavePointComment(long subRoomId, DateTime timestamp, string comment)
		{
			Debug.Assert(RoomData.ContainsKey(subRoomId));
			asm.Store.StoreSnapshotComment(subRoomId, timestamp, comment);
		}

		public static string SavePointDisplayString(DateTime dt)
		{
			return dt.ToString();
		}

		private void Store_SnapshotStored(object sender, Storage.StoreEventArgs e)
		{
			if (RoomData.TryGetValue(e.subRoomId, out SubRoomData data))
			{
				if (!data.IsSavePointsInitialized)
				{
					Debug.Assert(data.SavePoints != null);
					Debug.Assert(data.SavePoints.Count > 0);
					Debug.Assert(data.SavePoints[0].Timestamp == e.timestamp);
				}
				else
				{
					Debug.Assert(data.SavePoints.Count == 0 || data.SavePoints[0].Timestamp != e.timestamp);
					data.SavePoints.Insert(0, CreateSavePoint(e.subRoomId, e.timestamp, e.comment));
					RaiseSavePointAdded(e.subRoomId, e.timestamp);
				}
			}
			else
			{
				data = CreateSubRoomData(e.subRoomId, null, new List<SubRoomData.SavePoint> { CreateSavePoint(e.subRoomId, e.timestamp, e.comment) });
				RoomData.Add(e.subRoomId, data);
				RaiseSubRoomAdded(e.subRoomId);
			}
		}

		public void RestoreSubRoom(long subRoomId, DateTime timestamp)
		{
			asm.RestoreSubRoom(subRoomId, timestamp);
		}

		private void RaiseSubRoomAdded(long subRoomId)
		{
			this.ActivateSignal("subRoomAdded", subRoomId);
			this.ActivateSignal("roomDataChanged");
		}

		private void RaiseSavePointAdded(long subRoomId, DateTime? savePoint)
		{
			this.ActivateSignal("savePointAdded", subRoomId, savePoint);
		}
	}
}
