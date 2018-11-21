namespace AutoSaveManager
{
	using System;
	using System.Data.SQLite;
	using System.IO;

	class Storage : IDisposable
	{
		private SQLiteConnection dbConnection;
		private SQLiteCommand insertCommand;

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
				"WITHOUT ROWID";

			SQLiteCommand command = new SQLiteCommand(createSql, dbConnection);
			command.ExecuteNonQuery();

			createSql = "INSERT OR REPLACE INTO autosaves(subRoomId, timestamp, data) values (?, ?, ?)";

			insertCommand = new SQLiteCommand(createSql, dbConnection);
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
			dbConnection.Dispose();
			dbConnection = null;
		}

		public void InsertSnapshot(long subRoomId, long timestamp, byte[] blob)
		{
			if (dbConnection == null)
				return;
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary, (object)blob));
			insertCommand.ExecuteNonQuery();
			insertCommand.Parameters.Clear();
		}
	}
}
