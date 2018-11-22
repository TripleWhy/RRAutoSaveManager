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
		//private SQLiteCommand selectLatestCommand;
		private SQLiteCommand selectTimestamps;
		private SQLiteCommand selectSpecivicBlobCommand;
		private SQLiteCommand selectRoomsCommand;

		public Storage(string dbFile)
		{
			dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
			dbConnection.Open();

			string createSql = 
				"CREATE TABLE IF NOT EXISTS autosaves(" +
				"subRoomId INTEGER, " +
				"timestamp INTEGER, " +
				"data BLOB, " +
				"PRIMARY KEY(subRoomId ASC, timestamp DESC)) " +
				"WITHOUT ROWID;";

			SQLiteCommand command = new SQLiteCommand(createSql, dbConnection);
			command.ExecuteNonQuery();

			insertCommand = new SQLiteCommand("INSERT OR REPLACE INTO autosaves(subRoomId, timestamp, data) values (?, ?, ?);", dbConnection);
			//selectLatestCommand = new SQLiteCommand("SELECT timestamp, data FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC LIMIT 1;", dbConnection);
			selectTimestamps = new SQLiteCommand("SELECT timestamp FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC;", dbConnection);
			selectSpecivicBlobCommand = new SQLiteCommand("SELECT data FROM autosaves WHERE subRoomId = ? AND timestamp = ?;", dbConnection);
			selectRoomsCommand = new SQLiteCommand("SELECT DISTINCT subRoomId FROM autosaves;", dbConnection);
		}
		
		~Storage()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (dbConnection == null)
				return;
			insertCommand.Dispose();
			insertCommand = null;
			//selectLatestCommand.Dispose();
			//selectLatestCommand = null;
			selectTimestamps.Dispose();
			selectTimestamps = null;
			selectSpecivicBlobCommand.Dispose();
			selectSpecivicBlobCommand = null;
			selectRoomsCommand.Dispose();
			selectRoomsCommand = null;
			dbConnection.Dispose();
			dbConnection = null;
		}

		public void StoreSnapshot(long subRoomId, long timestamp, byte[] blob)
		{
			insertCommand.Parameters.Clear();
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary, (object)blob));
			insertCommand.ExecuteNonQuery();
		}

		public byte[] FetchSnapshot(long subRoomId, long timestamp)
		{
			selectSpecivicBlobCommand.Parameters.Clear();
			selectSpecivicBlobCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			selectSpecivicBlobCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp));
			byte[] data = null;
			using (SQLiteDataReader reader = selectSpecivicBlobCommand.ExecuteReader())
			{
				if (reader.Read())
					data = (byte[])reader[0];
			}
			return data;
		}

		public IEnumerable<long> FetchTimestamps(long subRoomId)
		{
			selectTimestamps.Parameters.Clear();
			selectTimestamps.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			using (SQLiteDataReader reader = selectTimestamps.ExecuteReader())
			{
				while (reader.Read())
					yield return reader.GetInt64(0);
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
	}
}
