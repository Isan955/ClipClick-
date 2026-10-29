using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using ClipClick.Models;

namespace ClipClick.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        public int MaxEntries { get; set; }

        public DatabaseService() : this(500)
        {
        }

        public DatabaseService(int maxEntries)
        {
            MaxEntries = maxEntries > 0 ? maxEntries : 500;
            string folder = GetStorageDirectory();
            string dbPath = Path.Combine(folder, "clipclick.db");
            _connectionString = string.Format("Data Source={0};Version=3;", dbPath);

            Initialize();
        }

        public static string GetStorageDirectory()
        {
            try
            {
                string asmDir = Path.GetDirectoryName(typeof(DatabaseService).Assembly.Location);
                if (!string.IsNullOrEmpty(asmDir))
                {
                    string dataFolder = Path.Combine(asmDir, "Data");
                    if (Directory.Exists(dataFolder))
                    {
                        return dataFolder;
                    }
                }
            }
            catch
            {
            }

            string localFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (Directory.Exists(localFolder))
            {
                return localFolder;
            }

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(appData))
                {
                    string folder = Path.Combine(appData, "ClipClick");
                    Directory.CreateDirectory(folder);
                    return folder;
                }
            }
            catch
            {
            }

            try
            {
                Directory.CreateDirectory(localFolder);
            }
            catch
            {
            }
            return localFolder;
        }

        private void Initialize()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS ClipboardEntries (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Content TEXT NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        IsPinned INTEGER NOT NULL DEFAULT 0,
                        CharCount INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS idx_clipboard_created ON ClipboardEntries(CreatedAt DESC);
                    CREATE INDEX IF NOT EXISTS idx_clipboard_pinned ON ClipboardEntries(IsPinned DESC, CreatedAt DESC);
                ";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public ClipboardItem AddClipboardEntry(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // Check most recent entry to prevent duplicate spam
                string checkSql = "SELECT Id, Content FROM ClipboardEntries ORDER BY CreatedAt DESC LIMIT 1;";
                using (var checkCmd = new SQLiteCommand(checkSql, conn))
                {
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int lastId = Convert.ToInt32(reader["Id"]);
                            string lastContent = reader["Content"].ToString();
                            if (string.Equals(lastContent, content, StringComparison.Ordinal))
                            {
                                // Just touch the timestamp to bump it to top
                                string updateSql = "UPDATE ClipboardEntries SET CreatedAt = @now WHERE Id = @id;";
                                using (var updateCmd = new SQLiteCommand(updateSql, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@now", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@id", lastId);
                                    updateCmd.ExecuteNonQuery();
                                }
                                return null; // Already existed and bumped
                            }
                        }
                    }
                }

                // Insert new entry
                DateTime now = DateTime.Now;
                int charCount = content.Length;
                int newId = 0;

                string insertSql = @"
                    INSERT INTO ClipboardEntries (Content, CreatedAt, IsPinned, CharCount)
                    VALUES (@content, @createdAt, 0, @charCount);
                    SELECT last_insert_rowid();
                ";

                using (var insertCmd = new SQLiteCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@content", content);
                    insertCmd.Parameters.AddWithValue("@createdAt", now);
                    insertCmd.Parameters.AddWithValue("@charCount", charCount);
                    newId = Convert.ToInt32(insertCmd.ExecuteScalar());
                }

                // Enforce max 500 entries limit (keep pinned entries safe)
                string pruneSql = @"
                    DELETE FROM ClipboardEntries 
                    WHERE IsPinned = 0 
                      AND Id NOT IN (
                          SELECT Id FROM ClipboardEntries 
                          ORDER BY IsPinned DESC, CreatedAt DESC 
                          LIMIT @limit
                      );
                ";
                using (var pruneCmd = new SQLiteCommand(pruneSql, conn))
                {
                    pruneCmd.Parameters.AddWithValue("@limit", MaxEntries);
                    pruneCmd.ExecuteNonQuery();
                }

                return new ClipboardItem
                {
                    Id = newId,
                    Content = content,
                    CreatedAt = now,
                    IsPinned = false,
                    CharCount = charCount
                };
            }
        }

        public List<ClipboardItem> GetClipboardEntries(string filter = null)
        {
            var list = new List<ClipboardItem>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Content, CreatedAt, IsPinned, CharCount FROM ClipboardEntries";
                bool hasFilter = !string.IsNullOrWhiteSpace(filter);

                if (hasFilter)
                {
                    sql += " WHERE Content LIKE @query";
                }
                sql += " ORDER BY IsPinned DESC, CreatedAt DESC LIMIT @limit;";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (hasFilter)
                    {
                        cmd.Parameters.AddWithValue("@query", "%" + filter + "%");
                    }
                    cmd.Parameters.AddWithValue("@limit", MaxEntries);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ClipboardItem
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Content = reader["Content"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                IsPinned = Convert.ToInt32(reader["IsPinned"]) != 0,
                                CharCount = Convert.ToInt32(reader["CharCount"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public void TogglePin(int id, bool isPinned)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "UPDATE ClipboardEntries SET IsPinned = @pinned WHERE Id = @id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pinned", isPinned ? 1 : 0);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteEntry(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM ClipboardEntries WHERE Id = @id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ClearAllUnpinned()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM ClipboardEntries WHERE IsPinned = 0;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ClearAllHistory(bool includePinned = false)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = includePinned
                    ? "DELETE FROM ClipboardEntries;"
                    : "DELETE FROM ClipboardEntries WHERE IsPinned = 0;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void PruneToLimit()
        {
            PruneToLimit(MaxEntries);
        }

        public void PruneToLimit(int limit)
        {
            if (limit <= 0) return;
            MaxEntries = limit;
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string pruneSql = @"
                    DELETE FROM ClipboardEntries 
                    WHERE IsPinned = 0 
                      AND Id NOT IN (
                          SELECT Id FROM ClipboardEntries 
                          ORDER BY IsPinned DESC, CreatedAt DESC 
                          LIMIT @limit
                      );
                ";
                using (var pruneCmd = new SQLiteCommand(pruneSql, conn))
                {
                    pruneCmd.Parameters.AddWithValue("@limit", limit);
                    pruneCmd.ExecuteNonQuery();
                }
            }
        }

        public void GetStats(out int totalCount, out int pinnedCount)
        {
            totalCount = 0;
            pinnedCount = 0;
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) AS Total, SUM(CASE WHEN IsPinned = 1 THEN 1 ELSE 0 END) AS Pinned FROM ClipboardEntries;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader["Total"]);
                            object pinnedVal = reader["Pinned"];
                            pinnedCount = (pinnedVal != DBNull.Value) ? Convert.ToInt32(pinnedVal) : 0;
                        }
                    }
                }
            }
        }
    }
}
