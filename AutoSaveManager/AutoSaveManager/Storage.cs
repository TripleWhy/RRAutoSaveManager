namespace AutoSaveManager
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.Data.SQLite;

	class Storage : IDisposable
	{
		private const long dbFormatVersion = 1;
		private const long autosaveFormatVersion = 2;

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
		private List<Task<int>> nonQueryExecutionTasks = new List<Task<int>>();

		public Storage(string dbFile)
		{
			dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFile));
			dbConnection.Open();

			string createSql =
				"CREATE TABLE IF NOT EXISTS settings(" +
				"name TEXT, " +
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
				"name TEXT, " +
				"FOREIGN KEY(subRoomId) REFERENCES autosaves(subRoomId)) " +
				"WITHOUT ROWID;";
			SQLiteCommand command = new SQLiteCommand(createSql, dbConnection);
			Task<int> commandTask = command.ExecuteNonQueryAsync();

			insertCommand = AddCommand("INSERT OR IGNORE INTO autosaves(subRoomId, timestamp, comment, data, autosaveFormatVersion) values (?, ?, ?, ?, ?);", dbConnection);
			selectLatestCommand = AddCommand("SELECT timestamp, data, autosaveFormatVersion FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC LIMIT 1;", dbConnection);
			selectTimestamps = AddCommand("SELECT timestamp, comment FROM autosaves WHERE subRoomId = ? ORDER BY timestamp DESC;", dbConnection);
			selectSpecivicBlobCommand = AddCommand("SELECT data, autosaveFormatVersion FROM autosaves WHERE subRoomId = ? AND timestamp = ?;", dbConnection);
			selectRoomsCommand = AddCommand("SELECT DISTINCT subRoomId FROM autosaves;", dbConnection);
			selectRoomsAndNamesCommand = AddCommand("SELECT subRoomId, name FROM (SELECT DISTINCT subRoomId FROM autosaves) as sids LEFT OUTER JOIN roomNames USING(subRoomId);", dbConnection);
			insertNameCommand = AddCommand("INSERT OR REPLACE INTO roomNames(subRoomId, name) VALUES (?, ?);", dbConnection);
			updateCommentCommand = AddCommand("UPDATE autosaves SET comment = ? WHERE subRoomId = ? AND timestamp = ?;", dbConnection);
			setSettingCommand = AddCommand("INSERT OR REPLACE INTO settings(name, intValue) VALUES (?, ?);", dbConnection);

			command = new SQLiteCommand("SELECT name, intValue FROM settings;", dbConnection);
			commandTask.Wait();
			using (SQLiteDataReader reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					string name = (string)reader[0];
					SettingKey key;
					if (!Enum.TryParse<SettingKey>(name, true, out key))
						continue;
					settings[key] = reader[1];
				}
			}
			long storedDbFormatVersion = (settings.GetValueOrDefault(SettingKey.DbFormatVersion) as long?).GetValueOrDefault(0L);
			if (storedDbFormatVersion > dbFormatVersion)
				throw new InvalidOperationException("Save file format version " + storedDbFormatVersion + " is newer than this program.");
			for (long v = storedDbFormatVersion; v < dbFormatVersion; v++)
				UpgradeDbFrom(v);
		}

		~Storage()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (dbConnection == null)
				return;
			foreach (Task<int> task in nonQueryExecutionTasks)
				task.Wait();
			nonQueryExecutionTasks.Clear();
			foreach (SQLiteCommand command in commands)
				command.Dispose();
			dbConnection.Dispose();
			dbConnection = null;
		}

		private SQLiteCommand AddCommand(string commandText, SQLiteConnection connection)
		{
			SQLiteCommand command = new SQLiteCommand(commandText, connection);
			commands.Add(command);
			return command;
		}

		private Task<int> AddExecutionTask(Task<int> task)
		{
			nonQueryExecutionTasks.Add(task);
			for (int i = nonQueryExecutionTasks.Count - 1; i >= 0; i--)
			{
				Task<int> t = nonQueryExecutionTasks[i];
				switch (t.Status)
				{
					case TaskStatus.RanToCompletion:
					case TaskStatus.Canceled:
					case TaskStatus.Faulted:
						nonQueryExecutionTasks.RemoveAt(i);
						break;
					default:
						break;
				}
			}
			return task;
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
			string sql =
				"ALTER TABLE autosaves " +
				"ADD COLUMN autosaveFormatVersion INTEGER; " +
				"UPDATE autosaves SET autosaveFormatVersion = 1;";
			SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
			command.ExecuteNonQuery();
			SetSetting(SettingKey.DbFormatVersion, 1);
		}

		private void SetSetting(SettingKey key, object value)
		{
			setSettingCommand.Parameters.Clear();
			setSettingCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, (object)key.ToString()));
			setSettingCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)value));
			AddExecutionTask(setSettingCommand.ExecuteNonQueryAsync());
			settings[key] = value;
		}

		private byte[] UpgradeSnapshot(byte[] data, long autosaveFormatVersion)
		{
			switch (autosaveFormatVersion)
			{
				case 1:
				{
					byte[] hashValue;
					using (System.Security.Cryptography.SHA256 mySHA256 = System.Security.Cryptography.SHA256.Create())
						hashValue = mySHA256.ComputeHash(data);
					byte[] result = new byte[data.Length + hashValue.Length];
					hashValue.CopyTo(result, 0);
					data.CopyTo(result, hashValue.Length);
					return result;
				}
				case Storage.autosaveFormatVersion:
					return data;
				default:
					throw new InvalidOperationException("Autosave format version " + autosaveFormatVersion + " unknown.");
			}
		}

		public void StoreSnapshot(long subRoomId, DateTime timestamp, string comment, byte[] blob, long autosaveFormatVersion)
		{
			insertCommand.Parameters.Clear();
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp.Ticks));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, (object)comment));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary, (object)blob));
			insertCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)autosaveFormatVersion));
			AddExecutionTask(insertCommand.ExecuteNonQueryAsync());
			SnapshotStored(this, new StoreEventArgs { subRoomId = subRoomId, timestamp = timestamp, comment = comment });
		}

		public class SnapshotData
		{
			public byte[] data;
			public DateTime timestamp;
		}

		public async Task<SnapshotData> FetchLatestSnapshotAsync(long subRoomId)
		{
			selectLatestCommand.Parameters.Clear();
			selectLatestCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			using (SQLiteDataReader reader = (SQLiteDataReader)await selectLatestCommand.ExecuteReaderAsync())
			{
				if (reader.Read())
				{
					return new SnapshotData { timestamp = new DateTime(reader.GetInt64(0)), data = UpgradeSnapshot((byte[])reader[1], (long)reader[2]) };
				}
			}
			return null;
		}

		public async Task<byte[]> FetchSnapshotAsync(long subRoomId, DateTime timestamp)
		{
			selectSpecivicBlobCommand.Parameters.Clear();
			selectSpecivicBlobCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			selectSpecivicBlobCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)timestamp.Ticks));
			using (SQLiteDataReader reader = (SQLiteDataReader)await selectSpecivicBlobCommand.ExecuteReaderAsync())
			{
				if (reader.Read())
					return UpgradeSnapshot((byte[])reader[0], (long)reader[1]);
			}
			return null;
		}

		public async IAsyncEnumerable<SavePointData> FetchTimestampsAsync(long subRoomId)
		{
			selectTimestamps.Parameters.Clear();
			selectTimestamps.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			using (SQLiteDataReader reader = (SQLiteDataReader)await selectTimestamps.ExecuteReaderAsync())
			{
				while (await reader.ReadAsync())
					yield return new SavePointData { timestamp = new DateTime(reader.GetInt64(0)), comment = reader[1] as string };
			}
		}

		public async IAsyncEnumerable<long> FetchSubRoomIdsAsync()
		{
			using (SQLiteDataReader reader = (SQLiteDataReader)await selectRoomsCommand.ExecuteReaderAsync())
			{
				while (await reader.ReadAsync())
					yield return reader.GetInt64(0);
			}
		}

		public class RoomAndName
		{
			public long subRoomId;
			public string subRoomName;
		}

		public async IAsyncEnumerable<RoomAndName> FetchSubRoomIdsWithNamesAsync()
		{
			using (SQLiteDataReader reader = (SQLiteDataReader)await selectRoomsAndNamesCommand.ExecuteReaderAsync())
			{
				while (await reader.ReadAsync())
					yield return new RoomAndName { subRoomId = reader.GetInt64(0), subRoomName = reader[1] as string };
			}
		}

		public void StoreSubRoomName(long subRoomId, string subRoomName)
		{
			insertNameCommand.Parameters.Clear();
			insertNameCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object)subRoomId));
			insertNameCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, (object)subRoomName));
			AddExecutionTask(insertNameCommand.ExecuteNonQueryAsync());
			//SubRoomNameChanged(this, new StoreEventArgs { subRoomId = subRoomId, subRoomName = subRoomName });
		}

		public void StoreSnapshotComment(long subRoomId, DateTime timestamp, string comment)
		{
			updateCommentCommand.Parameters.Clear();
			updateCommentCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, (object) comment));
			updateCommentCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object) subRoomId));
			updateCommentCommand.Parameters.Add(new SQLiteParameter(System.Data.DbType.Int64, (object) timestamp.Ticks));
			AddExecutionTask(updateCommentCommand.ExecuteNonQueryAsync());
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
