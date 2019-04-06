namespace AutoSaveManager
{
	using Qml.Net;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	[Signal("subRoomAdded", NetVariantType.Int)]
	[Signal("savePointAdded", NetVariantType.Int, NetVariantType.DateTime)]
	class QmlBridge : IDisposable
	{
		public class SubRoomData
		{
			public class SavePoint
			{
				public DateTime Timestamp { get; set; }
				public string DisplayString { get; set; }
				public string Comment { get; set; }

				public SavePoint(DateTime timestamp, String comment)
				{
					Timestamp = timestamp;
					DisplayString = SavePointDisplayString(timestamp);
					Comment = comment;
				}
			}

			public long SubRoomId { get; set; }
			public string SubRoomName { get; set; }
			public DateTime TestDt { get; set; }
			
			public delegate void LoadSavePointsCallback(long subRoomId);
			public LoadSavePointsCallback LoadSavePoints;

			private List<SavePoint> _savePoints;
			public bool IsSavePointsInitialized { get => _savePoints != null; }
			public List<SavePoint> SavePoints
			{
				get
				{
					if (_savePoints == null && LoadSavePoints != null)
					{
						LoadSavePoints(SubRoomId);
						Debug.Assert(_savePoints != null);
					}
					return _savePoints;
				}
				set => _savePoints = value;
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

		public void Initialize()
		{
			if (asm != null)
				return;
			asm = new AutoSaveManager();
			asm.StartWatching();

			RoomData.Clear();
			foreach (Storage.RoomAndName ram in asm.Store.FetchSubRoomIdsWithNames())
				RoomData.Add(ram.subRoomId, new SubRoomData { SubRoomId = ram.subRoomId, SubRoomName = ram.subRoomName, LoadSavePoints = LoadSavePoints });
			asm.Store.SnapshotStored += Store_SnapshotStored;

			RaiseSubRoomAdded(-1);
		}

		private void LoadSavePoints(long subRoomId)
		{
			Debug.Assert(RoomData.ContainsKey(subRoomId));
			List<SubRoomData.SavePoint> savePoints = new List<SubRoomData.SavePoint>();
			foreach (Storage.SavePointData spd in asm.Store.FetchTimestamps(subRoomId))
				savePoints.Add(new SubRoomData.SavePoint(spd.timestamp, spd.comment));
			RoomData[subRoomId].SavePoints = savePoints;
			RaiseSavePointAdded(subRoomId, null);
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
					data.SavePoints.Insert(0, new SubRoomData.SavePoint(e.timestamp, e.comment));
					RaiseSavePointAdded(e.subRoomId, e.timestamp);
				}
			}
			else
			{
				data = new SubRoomData { SubRoomId = e.subRoomId, SavePoints = new List<SubRoomData.SavePoint> { new SubRoomData.SavePoint(e.timestamp, e.comment) } };
				RoomData.Add(e.subRoomId, data);
				RaiseSubRoomAdded(e.subRoomId);
			}
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
