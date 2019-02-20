namespace AutoSaveManager
{
	using System;
	using System.Collections.Generic;
	using System.Data.SQLite;
	using System.IO;

	class Storage : IDisposable
	{
		private SQLiteConnection dbConnection;
		private SQLiteCommand insertCommand;
		private SQLiteCommand selectLatestCommand;
		private SQLiteCommand selectTimestamps;
		private SQLiteCommand selectSpecivicBlobCommand;
		private SQLiteCommand selectRoomsCommand;
		private SQLiteCommand selectRoomsAndNamesCommand;

		private readonly SQLiteCommand[] commands;

		public Storage(string dbFile)
		{
			dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
			dbConnection.Open();

			string createSql = 
				"CREATE TABLE IF NOT EXISTS autosaves(" +
				"subRoomId INTEGER, " +
				"timestamp INTEGER, " +
				"comment TEXT, " +
				"data BLOB, " +
				"PRIMARY KEY(subRoomId ASC, timestamp DESC)) " +
				"WITHOUT ROWID;" +
				"CREATE TABLE IF NOT EXISTS roomNames(" +
				"subRoomId INTEGER PRIMARY KEY, " +
				"name TEXT) " +
				"WITHOUT ROWID;";

			SQLiteCommand command = new SQLiteCommand(createSql, dbConnection);
			command.ExecuteNonQuery();

			insertCommand = new SQLiteCommand("INSERT OR REPLACE INTO autosaves(subRoomId, timestamp, comment, data) values (?, ?, ?, ?);", dbConnection);
			selectLatestCommand = new SQLiteCommand("SELECT timestamp, data FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC LIMIT 1;", dbConnection);
			selectTimestamps = new SQLiteCommand("SELECT timestamp, comment FROM autosaves WHERE subRoomId = ? ORDER BY timestamp ASC;", dbConnection);
			selectSpecivicBlobCommand = new SQLiteCommand("SELECT data FROM autosaves WHERE subRoomId = ? AND timestamp = ?;", dbConnection);
			selectRoomsCommand = new SQLiteCommand("SELECT DISTINCT subRoomId FROM autosaves;", dbConnection);
			selectRoomsAndNamesCommand = new SQLiteCommand("SELECT subRoomId, name FROM (SELECT DISTINCT subRoomId FROM autosaves) as sids LEFT OUTER JOIN roomNames USING(subRoomId);", dbConnection);

			commands = new SQLiteCommand[]{ insertCommand, selectLatestCommand, selectTimestamps, selectSpecivicBlobCommand, selectRoomsCommand, selectRoomsAndNamesCommand };
		}
		
		~Storage()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (dbConnection == null)
				return;
			foreach (SQLiteCommand command in commands)
				command.Dispose();
			dbConnection.Dispose();
			dbConnection = null;
		}

		public void StoreSnapshot(long subRoomId, DateTime timestamp, string comment, byte[] blob)
		{
			insertCommand.Parameters.Clear();
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp.Ticks));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, (object)comment));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary, (object)blob));
			insertCommand.ExecuteNonQuery();
			SnapshotStored(this, new StoreEventArgs { subRoomId = subRoomId, timestamp = timestamp, comment = comment });
		}

		public byte[] FetchLatestSnapshot(long subRoomId, out DateTime timestamp)
		{
			selectLatestCommand.Parameters.Clear();
			selectLatestCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			using (SQLiteDataReader reader = selectLatestCommand.ExecuteReader())
			{
				if (reader.Read())
				{
					timestamp = new DateTime(reader.GetInt64(0));
					return (byte[])reader[1];
				}
			}
			timestamp = new DateTime();
			return null;
		}

		public byte[] FetchSnapshot(long subRoomId, DateTime timestamp)
		{
			selectSpecivicBlobCommand.Parameters.Clear();
			selectSpecivicBlobCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			selectSpecivicBlobCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp.Ticks));
			using (SQLiteDataReader reader = selectSpecivicBlobCommand.ExecuteReader())
			{
				if (reader.Read())
					return (byte[])reader[0];
			}
			return null;
		}

		public IEnumerable<SavePointData> FetchTimestamps(long subRoomId)
		{
			selectTimestamps.Parameters.Clear();
			selectTimestamps.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			using (SQLiteDataReader reader = selectTimestamps.ExecuteReader())
			{
				while (reader.Read())
					yield return new SavePointData { timestamp = new DateTime(reader.GetInt64(0)), comment = reader[1] as string };
			}
		}

		public IEnumerable<long> FetchSubRoomIds()
		{
			using (SQLiteDataReader reader = selectRoomsCommand.ExecuteReader())
			{
				while (reader.Read())
					yield return reader.GetInt64(0);
			}
		}

		public class RoomAndName
		{
			public long subRoomId;
			public string subRoomName;
		}

		public IEnumerable<RoomAndName> FetchSubRoomIdsWithNames()
		{
			using (SQLiteDataReader reader = selectRoomsAndNamesCommand.ExecuteReader())
			{
				while (reader.Read())
					yield return new RoomAndName { subRoomId = reader.GetInt64(0), subRoomName = reader[1] as string };
			}
		}

		public class SavePointData
		{
			public DateTime timestamp;
			public string comment;
		}

		public class StoreEventArgs : EventArgs
		{
			public long subRoomId;
			public DateTime timestamp;
			public string comment;
		}
		public delegate void StoreEventHandler(object sender, StoreEventArgs a);
		public event EventHandler<StoreEventArgs> SnapshotStored = delegate{ };
	}
}
