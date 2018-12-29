namespace AutoSaveManager
{
	using Qml.Net;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	class QmlBridge : IDisposable
	{
		public class SubRoomData
		{
			public int SubRoomId { get; set; } //TODO change to long when qml.net supports that
			public string SubRoomName { get; set; }
			public List<DateTime> savePoints = null;
		}

		private AutoSaveManager asm;

		[NotifySignal]
		public SortedDictionary<long, SubRoomData> RoomData { get; }
		//public List<SubRoomData> RoomDataList { get => new List<SubRoomData>(RoomData.Values); }
		public List<SubRoomData> RoomDataList
		{
			get
			{
				var list = new List<SubRoomData>(RoomData.Values);
				Console.WriteLine(list.Count);
				Console.WriteLine(list);
				return list;
			}
		}

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
				RoomData.Add(ram.subRoomId, new SubRoomData { SubRoomId = (int)ram.subRoomId, SubRoomName = ram.subRoomName });
			asm.Store.SnapshotStored += Store_SnapshotStored;

			RaiseSubRoomAdded(-1);
		}

		private void LoadSavePoints(long subRoomId)
		{
			Debug.Assert(RoomData.ContainsKey(subRoomId));
			RoomData[subRoomId].savePoints = new List<DateTime>(asm.Store.FetchTimestamps(subRoomId));
			RaiseSavePointAdded(subRoomId, null);
		}

		private void Store_SnapshotStored(object sender, Storage.StoreEventArgs e)
		{
			if (RoomData.TryGetValue(e.subRoomId, out SubRoomData data))
			{
				if (data.savePoints == null)
				{
					LoadSavePoints(e.subRoomId);
					Debug.Assert(data.savePoints != null);
					Debug.Assert(data.savePoints.Count > 0);
					Debug.Assert(data.savePoints[data.savePoints.Count - 1] == e.timestamp);
				}
				else
				{
					Debug.Assert(data.savePoints.Count == 0 || data.savePoints[data.savePoints.Count - 1] != e.timestamp);
					data.savePoints.Add(e.timestamp);
					RaiseSavePointAdded(e.subRoomId, e.timestamp);
				}
			}
			else
			{
				data = new SubRoomData { SubRoomId = (int)e.subRoomId, savePoints = new List<DateTime> { e.timestamp } };
				RoomData.Add(e.subRoomId, data);
				RaiseSubRoomAdded(e.subRoomId);
			}
		}

		private void RaiseSubRoomAdded(long subRoomId)
		{
			this.ActivateSignal("subRoomAdded", (int)subRoomId); //TODO make long
		}

		private void RaiseSavePointAdded(long subRoomId, DateTime? savePoint)
		{
			this.ActivateSignal("savePointAdded", (int)subRoomId, savePoint); //TODO make long
		}
	}
}
