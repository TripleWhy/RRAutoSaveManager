namespace AutoSaveManager
{
	using System;
	using System.Collections.Generic;
	using System.Data.SQLite;
	using System.IO;

	class Storage : IDisposable
	{
		private const long dbFormatVersion = 1;
		public const long autosaveFormatVersion = 2;

		private enum SettingKey
		{
			DbFormatVersion,
		}
		private readonly Dictionary<SettingKey, object> settings = new Dictionary<SettingKey, object>();

		private SQLiteConnection dbConnection;
		private readonly SQLiteCommand insertCommand;
		private readonly SQLiteCommand selectLatestCommand;
		private readonly SQLiteCommand selectTimestamps;
		private readonly SQLiteCommand selectSpecivicBlobCommand;
		private readonly SQLiteCommand selectRoomsCommand;
		private readonly SQLiteCommand selectRoomsAndNamesCommand;
		private readonly SQLiteCommand insertNameCommand;
		private readonly SQLiteCommand updateCommentCommand;
		private readonly SQLiteCommand setSettingCommand;
		private readonly List<SQLiteCommand> commands = new List<SQLiteCommand>();
		private readonly string createTablesSql;

		public Storage(string dbFile)
		{
			bool dbIsNew = !File.Exists(dbFile);
			dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
			dbConnection.Open();

			createTablesSql =
				"CREATE TABLE IF NOT EXISTS settings(" +
				"name TEXT PRIMARY KEY, " +
				"intValue INTEGER); " +
				"" +
				"CREATE TABLE IF NOT EXISTS autosaves(" +
				"subRoomId INTEGER, " +
				"timestamp INTEGER, " +
				"comment TEXT, " +
				"data BLOB, " +
				"autosaveFormatVersion INTEGER DEFAULT 1, " +
				"PRIMARY KEY(subRoomId ASC, timestamp DESC)) " +
				"WITHOUT ROWID; " +
				"" +
				"CREATE TABLE IF NOT EXISTS roomNames(" +
				"subRoomId INTEGER PRIMARY KEY, " +
				"name TEXT) " +
				"WITHOUT ROWID;";
			using (SQLiteCommand command = new SQLiteCommand(createTablesSql, dbConnection))
				command.ExecuteNonQuery();

			insertCommand = AddCommand(dbConnection, "INSERT OR IGNORE INTO autosaves(subRoomId, timestamp, comment, data, autosaveFormatVersion) values (?, ?, ?, ?, ?);",
				System.Data.DbType.Int64, System.Data.DbType.Int64, System.Data.DbType.String, System.Data.DbType.Binary, System.Data.DbType.Int64);
			selectLatestCommand = AddCommand(dbConnection, "SELECT timestamp, data, autosaveFormatVersion FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC LIMIT 1;",
				System.Data.DbType.Int64);
			selectTimestamps = AddCommand(dbConnection, "SELECT timestamp, comment FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC;",
				System.Data.DbType.Int64);
			selectSpecivicBlobCommand = AddCommand(dbConnection, "SELECT data, autosaveFormatVersion FROM autosaves WHERE subRoomId = ? AND timestamp = ?;",
				System.Data.DbType.Int64, System.Data.DbType.Int64);
			selectRoomsCommand = AddCommand(dbConnection, "SELECT DISTINCT subRoomId FROM autosaves;");
			selectRoomsAndNamesCommand = AddCommand(dbConnection, "SELECT subRoomId, name FROM (SELECT DISTINCT subRoomId FROM autosaves) as sids LEFT OUTER JOIN roomNames USING(subRoomId);");
			insertNameCommand = AddCommand(dbConnection, "INSERT OR REPLACE INTO roomNames(subRoomId, name) VALUES (?, ?);",
				System.Data.DbType.Int64, System.Data.DbType.String);
			updateCommentCommand = AddCommand(dbConnection, "UPDATE autosaves SET comment = ? WHERE subRoomId = ? AND timestamp = ?;",
				System.Data.DbType.String, System.Data.DbType.Int64, System.Data.DbType.Int64);
			setSettingCommand = AddCommand(dbConnection, "INSERT OR REPLACE INTO settings(name, intValue) VALUES (?, ?);",
				System.Data.DbType.String, System.Data.DbType.Int64);

			using (SQLiteCommand command = new SQLiteCommand("SELECT name, intValue FROM settings;", dbConnection))
			using (SQLiteDataReader reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					string name = (string)reader[0];
					if (!Enum.TryParse<SettingKey>(name, true, out SettingKey key))
						continue;
					settings[key] = reader[1];
				}
			}
			if (dbIsNew)
				SetSetting(SettingKey.DbFormatVersion, dbFormatVersion);
			else
			{
				long storedDbFormatVersion = (settings.GetValueOrDefault(SettingKey.DbFormatVersion) as long?).GetValueOrDefault(0L);
				if (storedDbFormatVersion > dbFormatVersion)
					throw new InvalidOperationException("Save file format version " + storedDbFormatVersion + " is newer than this program.");
				for (long v = storedDbFormatVersion; v < dbFormatVersion; v++)
					UpgradeDbFrom(v);
			}
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
			using (SQLiteCommand command = new SQLiteCommand("VACUUM;", dbConnection))
				command.ExecuteNonQuery();
			dbConnection.Dispose();
			dbConnection = null;
		}

		private SQLiteCommand AddCommand(SQLiteConnection connection, string commandText, params System.Data.DbType[] parameterTypes)
		{
			SQLiteCommand command = new SQLiteCommand(commandText, connection);
			command.Parameters.Clear();
			foreach (System.Data.DbType parameterType in parameterTypes)
				command.Parameters.Add(new SQLiteParameter(parameterType));
			commands.Add(command);
			return command;
		}

		private void UpgradeDbFrom(long version)
		{
			switch (version)
			{
				case 0:
					UpgradeDbFrom0To1();
					break;
				default:
					throw new InvalidOperationException("Internal version upgrade error (" + version + ".");
			}
		}

		private void UpgradeDbFrom0To1()
		{
			using SQLiteTransaction transaction = dbConnection.BeginTransaction();
			string sql =
				"ALTER TABLE autosaves " +
				"ADD COLUMN autosaveFormatVersion INTEGER; " +
				"UPDATE autosaves SET autosaveFormatVersion = 1; " +
				"ALTER TABLE roomNames RENAME TO roomNamesOld; ";
			using (SQLiteCommand command = new SQLiteCommand(sql, dbConnection))
				command.ExecuteNonQuery();
			using (SQLiteCommand command = new SQLiteCommand(createTablesSql, dbConnection))
				command.ExecuteNonQuery();
			sql = "INSERT INTO roomNames (subRoomId, name) " +
				"SELECT subRoomId, name FROM roomNamesOld; " +
				"DROP TABLE roomNamesOld;";
			using (SQLiteCommand command = new SQLiteCommand(sql, dbConnection))
				command.ExecuteNonQuery();
			SetSetting(SettingKey.DbFormatVersion, 1);
			transaction.Commit();
		}

		private void SetSetting(SettingKey key, object value)
		{
			setSettingCommand.Parameters[0].Value = key.ToString();
			setSettingCommand.Parameters[1].Value = value;
			setSettingCommand.ExecuteNonQuery();
			settings[key] = value;
		}

		public void StoreSnapshot(long subRoomId, DateTime timestamp, string comment, byte[] blob, long autosaveFormatVersion)
		{
			insertCommand.Parameters[0].Value = subRoomId;
			insertCommand.Parameters[1].Value = timestamp.Ticks;
			insertCommand.Parameters[2].Value = comment;
			insertCommand.Parameters[3].Value = blob;
			insertCommand.Parameters[4].Value = autosaveFormatVersion;
			insertCommand.ExecuteNonQuery();
			SnapshotStored(this, new StoreEventArgs { subRoomId = subRoomId, timestamp = timestamp, comment = comment });
		}

		public ArraySegment<byte> FetchLatestSnapshotContentBytes(long subRoomId, out DateTime timestamp, out long autosaveFormatVersion)
		{
			selectLatestCommand.Parameters[0].Value = subRoomId;
			using (SQLiteDataReader reader = selectLatestCommand.ExecuteReader())
			{
				if (reader.Read())
				{
					timestamp = new DateTime(reader.GetInt64(0));
					autosaveFormatVersion = (long)reader[2];
					return AutoSaveManager.SnapshotContentBtytes((byte[])reader[1], autosaveFormatVersion);
				}
			}
			timestamp = new DateTime();
			autosaveFormatVersion = 0;
			return null;
		}

		public byte[] FetchSnapshot(long subRoomId, DateTime timestamp, out long autosaveFormatVersion)
		{
			selectSpecivicBlobCommand.Parameters[0].Value = subRoomId;
			selectSpecivicBlobCommand.Parameters[1].Value = timestamp.Ticks;
			using (SQLiteDataReader reader = selectSpecivicBlobCommand.ExecuteReader())
			{
				if (reader.Read())
				{
					autosaveFormatVersion = (long)reader[1];
					return (byte[])reader[0];
				}
			}
			autosaveFormatVersion = 0;
			return null;
		}

		public IEnumerable<SavePointData> FetchTimestamps(long subRoomId)
		{
			selectTimestamps.Parameters[0].Value = subRoomId;
			using SQLiteDataReader reader = selectTimestamps.ExecuteReader();
			while (reader.Read())
				yield return new SavePointData { timestamp = new DateTime(reader.GetInt64(0)), comment = reader[1] as string };
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
			using SQLiteDataReader reader = selectRoomsAndNamesCommand.ExecuteReader();
			while (reader.Read())
				yield return new RoomAndName { subRoomId = reader.GetInt64(0), subRoomName = reader[1] as string };
		}

		public void StoreSubRoomName(long subRoomId, string subRoomName)
		{
			insertNameCommand.Parameters[0].Value = subRoomId;
			insertNameCommand.Parameters[1].Value = subRoomName;
			insertNameCommand.ExecuteNonQuery();
			//SubRoomNameChanged(this, new StoreEventArgs { subRoomId = subRoomId, subRoomName = subRoomName });
		}

		public void StoreSnapshotComment(long subRoomId, DateTime timestamp, string comment)
		{
			updateCommentCommand.Parameters[0].Value = comment;
			updateCommentCommand.Parameters[1].Value = subRoomId;
			updateCommentCommand.Parameters[2].Value = timestamp.Ticks;
			updateCommentCommand.ExecuteNonQuery();
			//SnapshotCommentChanged(this, new StoreEventArgs { subRoomId = subRoomId, timestamp = timestamp, comment = comment });
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
