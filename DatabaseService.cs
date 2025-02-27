using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Faye.Commands;
using Faye.Data;

namespace Faye.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Setup database connection
            string dbPath = "botdata.db";
            bool firstRun = !File.Exists(dbPath);
            _connectionString = $"Data Source={dbPath};Version=3;";

            if (firstRun)
            {
                Console.WriteLine("First run detected, creating database...");
                CreateDatabase();
            }
        }

        public Task InitializeAsync()
        {
            // Ensure all required tables exist on startup
            EnsureTablesExist();
            return Task.CompletedTask;
        }

        private void CreateDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string usersTable = @"
                CREATE TABLE Users (
                    UserId TEXT PRIMARY KEY,
                    Username TEXT,
                    Discriminator TEXT,
                    XP INTEGER DEFAULT 0,
                    Level INTEGER DEFAULT 0,
                    LastMessageTime TEXT
                );";

            // New schema includes Limits column.
            string profilesTable = @"
                CREATE TABLE Profiles (
                    UserId TEXT PRIMARY KEY,
                    Bio TEXT,
                    Interests TEXT,
                    Kinks TEXT,
                    Limits TEXT,
                    Age INTEGER,
                    Gender TEXT,
                    FOREIGN KEY(UserId) REFERENCES Users(UserId)
                );";

            string truthTable = @"
                CREATE TABLE TruthPrompts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Prompt TEXT NOT NULL,
                    AddedBy TEXT NOT NULL,
                    AddedAt TEXT NOT NULL
                );";

            string dareTable = @"
                CREATE TABLE DarePrompts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Prompt TEXT NOT NULL,
                    AddedBy TEXT NOT NULL,
                    AddedAt TEXT NOT NULL
                );";

            using (var command = new SQLiteCommand(usersTable, connection))
                command.ExecuteNonQuery();

            using (var command = new SQLiteCommand(profilesTable, connection))
                command.ExecuteNonQuery();

            using (var command = new SQLiteCommand(truthTable, connection))
                command.ExecuteNonQuery();

            using (var command = new SQLiteCommand(dareTable, connection))
                command.ExecuteNonQuery();
        }

        private void EnsureTablesExist()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var cmd = new SQLiteCommand(connection);

            string createTables = @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId TEXT PRIMARY KEY,
                    Username TEXT,
                    Discriminator TEXT,
                    XP INTEGER DEFAULT 0,
                    Level INTEGER DEFAULT 0,
                    LastMessageTime TEXT
                );
                
                CREATE TABLE IF NOT EXISTS Profiles (
                    UserId TEXT PRIMARY KEY,
                    Bio TEXT,
                    Interests TEXT,
                    Kinks TEXT,
                    Limits TEXT,
                    Age INTEGER,
                    Gender TEXT,
                    FOREIGN KEY(UserId) REFERENCES Users(UserId)
                );
                
                CREATE TABLE IF NOT EXISTS TruthPrompts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Prompt TEXT NOT NULL,
                    AddedBy TEXT NOT NULL,
                    AddedAt TEXT NOT NULL
                );
                
                CREATE TABLE IF NOT EXISTS DarePrompts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Prompt TEXT NOT NULL,
                    AddedBy TEXT NOT NULL,
                    AddedAt TEXT NOT NULL
                );";

            cmd.CommandText = createTables;
            cmd.ExecuteNonQuery();

            // Check if the Profiles table has the Limits column.
            cmd.CommandText = "PRAGMA table_info(Profiles);";
            bool limitsExists = false;
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var colName = reader["name"].ToString();
                    if (colName.Equals("Limits", StringComparison.OrdinalIgnoreCase))
                    {
                        limitsExists = true;
                        break;
                    }
                }
            }
            if (!limitsExists)
            {
                // If Limits column is missing, add it.
                cmd.CommandText = "ALTER TABLE Profiles ADD COLUMN Limits TEXT;";
                cmd.ExecuteNonQuery();
            }

            // Populate Truth and Dare prompts if tables are empty.
            cmd.CommandText = "SELECT COUNT(*) FROM TruthPrompts;";
            int truthCount = 0;
            try { truthCount = Convert.ToInt32(cmd.ExecuteScalar()); } catch { }

            cmd.CommandText = "SELECT COUNT(*) FROM DarePrompts;";
            int dareCount = 0;
            try { dareCount = Convert.ToInt32(cmd.ExecuteScalar()); } catch { }

            if (truthCount == 0) PopulateInitialTruthPrompts(connection);
            if (dareCount == 0) PopulateInitialDarePrompts(connection);
        }

        private void PopulateInitialTruthPrompts(SQLiteConnection connection)
        {
            var truthPrompts = new List<string>
            {
                "What's the most embarrassing thing you've ever done?",
                "What's a secret you've never told anyone?",
                "What's your biggest fear?",
                "What's something you would change about your appearance if you could?",
                "What's something embarrassing you've done in front of a crush?"
            };

            using var transaction = connection.BeginTransaction();
            try
            {
                const string insertQuery = "INSERT INTO TruthPrompts (Prompt, AddedBy, AddedAt) VALUES (@Prompt, 'System', @AddedAt);";
                using var cmd = new SQLiteCommand(insertQuery, connection, transaction);
                var promptParam = cmd.Parameters.Add("@Prompt", System.Data.DbType.String);
                var dateParam = cmd.Parameters.Add("@AddedAt", System.Data.DbType.String);
                string now = DateTime.UtcNow.ToString("o");
                foreach (var prompt in truthPrompts)
                {
                    promptParam.Value = prompt;
                    dateParam.Value = now;
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void PopulateInitialDarePrompts(SQLiteConnection connection)
        {
            var darePrompts = new List<string>
            {
                "Do your best impression of another player.",
                "Text a friend and tell them a joke.",
                "Dance to a song of the group's choice.",
                "Do your best robot dance.",
                "Tell a funny joke - if no one laughs, you have to tell another one."
            };

            using var transaction = connection.BeginTransaction();
            try
            {
                const string insertQuery = "INSERT INTO DarePrompts (Prompt, AddedBy, AddedAt) VALUES (@Prompt, 'System', @AddedAt);";
                using var cmd = new SQLiteCommand(insertQuery, connection, transaction);
                var promptParam = cmd.Parameters.Add("@Prompt", System.Data.DbType.String);
                var dateParam = cmd.Parameters.Add("@AddedAt", System.Data.DbType.String);
                string now = DateTime.UtcNow.ToString("o");
                foreach (var prompt in darePrompts)
                {
                    promptParam.Value = prompt;
                    dateParam.Value = now;
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ----------------------------
        //        User Data Methods
        // ----------------------------

        public async Task CreateOrUpdateUserAsync(UserData userData)
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = @"
                INSERT INTO Users (UserId, Username, Discriminator, XP, Level, LastMessageTime) 
                VALUES (@UserId, @Username, @Discriminator, @XP, @Level, @LastMessageTime)
                ON CONFLICT(UserId) DO UPDATE SET 
                    Username = @Username, 
                    Discriminator = @Discriminator, 
                    XP = @XP, 
                    Level = @Level, 
                    LastMessageTime = @LastMessageTime";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userData.UserId.ToString());
            cmd.Parameters.AddWithValue("@Username", userData.Username);
            cmd.Parameters.AddWithValue("@Discriminator", userData.Discriminator);
            cmd.Parameters.AddWithValue("@XP", userData.XP);
            cmd.Parameters.AddWithValue("@Level", userData.Level);
            cmd.Parameters.AddWithValue("@LastMessageTime", userData.LastMessageTime);
            cmd.ExecuteNonQuery();
        }

        public async Task<UserData?> GetUserAsync(ulong userId)
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM Users WHERE UserId = @UserId";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId.ToString());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserData
                {
                    UserId = ulong.Parse(reader["UserId"].ToString()!),
                    Username = reader["Username"].ToString()!,
                    Discriminator = reader["Discriminator"].ToString()!,
                    XP = Convert.ToInt32(reader["XP"]),
                    Level = Convert.ToInt32(reader["Level"]),
                    LastMessageTime = reader["LastMessageTime"].ToString()!
                };
            }
            return null;
        }

        public async Task<List<UserData>> GetTopUsersAsync(int limit = 10)
        {
            await Task.Yield();
            var users = new List<UserData>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM Users ORDER BY XP DESC LIMIT @Limit";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Limit", limit);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new UserData
                {
                    UserId = ulong.Parse(reader["UserId"].ToString()!),
                    Username = reader["Username"].ToString()!,
                    Discriminator = reader["Discriminator"].ToString()!,
                    XP = Convert.ToInt32(reader["XP"]),
                    Level = Convert.ToInt32(reader["Level"]),
                    LastMessageTime = reader["LastMessageTime"].ToString()!
                });
            }
            return users;
        }

        // ----------------------------
        //      Profile Data Methods
        // ----------------------------

        public async Task UpdateUserProfileAsync(ulong userId, ProfileData profileData)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            const string query = @"
                INSERT INTO Profiles (UserId, Bio, Interests, Kinks, Limits, Age, Gender) 
                VALUES (@UserId, @Bio, @Interests, @Kinks, @Limits, @Age, @Gender)
                ON CONFLICT(UserId) DO UPDATE SET 
                    Bio = @Bio,
                    Interests = @Interests,
                    Kinks = @Kinks,
                    Limits = @Limits,
                    Age = @Age,
                    Gender = @Gender";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId.ToString());
            cmd.Parameters.AddWithValue("@Bio", profileData.Bio);
            cmd.Parameters.AddWithValue("@Interests", profileData.Interests);
            cmd.Parameters.AddWithValue("@Kinks", profileData.Kinks);
            cmd.Parameters.AddWithValue("@Limits", profileData.Limits);
            cmd.Parameters.AddWithValue("@Age", profileData.Age);
            cmd.Parameters.AddWithValue("@Gender", profileData.Gender);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<ProfileData?> GetUserProfileAsync(ulong userId)
        {
            await Task.Yield();
            ProfileData? profileData = null;
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM Profiles WHERE UserId = @UserId";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId.ToString());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                profileData = new ProfileData
                {
                    Bio = reader["Bio"].ToString()!,
                    Interests = reader["Interests"].ToString()!,
                    Kinks = reader["Kinks"].ToString()!,
                    Limits = reader["Limits"].ToString()!,
                    Age = Convert.ToInt32(reader["Age"]),
                    Gender = reader["Gender"].ToString()!
                };
            }
            return profileData;
        }

        // ----------------------------
        //    Truth or Dare Prompts
        // ----------------------------

        public async Task<int> AddTruthPromptAsync(string prompt, string addedBy)
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = @"
                INSERT INTO TruthPrompts (Prompt, AddedBy, AddedAt) 
                VALUES (@Prompt, @AddedBy, @AddedAt);
                SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Prompt", prompt);
            cmd.Parameters.AddWithValue("@AddedBy", addedBy);
            cmd.Parameters.AddWithValue("@AddedAt", DateTime.UtcNow.ToString("o"));
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public async Task<int> AddDarePromptAsync(string prompt, string addedBy)
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = @"
                INSERT INTO DarePrompts (Prompt, AddedBy, AddedAt) 
                VALUES (@Prompt, @AddedBy, @AddedAt);
                SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Prompt", prompt);
            cmd.Parameters.AddWithValue("@AddedBy", addedBy);
            cmd.Parameters.AddWithValue("@AddedAt", DateTime.UtcNow.ToString("o"));
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public async Task<bool> RemoveTruthPromptAsync(int id)
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "DELETE FROM TruthPrompts WHERE Id = @Id";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public async Task<bool> RemoveDarePromptAsync(int id)
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "DELETE FROM DarePrompts WHERE Id = @Id";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public async Task<TruthPrompt?> GetRandomTruthPromptAsync()
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM TruthPrompts ORDER BY RANDOM() LIMIT 1";
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TruthPrompt
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Prompt = reader["Prompt"].ToString()!,
                    AddedBy = reader["AddedBy"].ToString()!,
                    AddedAt = reader["AddedAt"].ToString()!
                };
            }
            return null;
        }

        public async Task<DarePrompt?> GetRandomDarePromptAsync()
        {
            await Task.Yield();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM DarePrompts ORDER BY RANDOM() LIMIT 1";
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new DarePrompt
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Prompt = reader["Prompt"].ToString()!,
                    AddedBy = reader["AddedBy"].ToString()!,
                    AddedAt = reader["AddedAt"].ToString()!
                };
            }
            return null;
        }

        public async Task<List<TruthPrompt>> GetAllTruthPromptsAsync()
        {
            await Task.Yield();
            var prompts = new List<TruthPrompt>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM TruthPrompts";
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                prompts.Add(new TruthPrompt
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Prompt = reader["Prompt"].ToString()!,
                    AddedBy = reader["AddedBy"].ToString()!,
                    AddedAt = reader["AddedAt"].ToString()!
                });
            }
            return prompts;
        }

        public async Task<List<DarePrompt>> GetAllDarePromptsAsync()
        {
            await Task.Yield();
            var prompts = new List<DarePrompt>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT * FROM DarePrompts";
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                prompts.Add(new DarePrompt
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Prompt = reader["Prompt"].ToString()!,
                    AddedBy = reader["AddedBy"].ToString()!,
                    AddedAt = reader["AddedAt"].ToString()!
                });
            }
            return prompts;
        }
    }
}
