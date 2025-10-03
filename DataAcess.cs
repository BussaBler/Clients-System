using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Microsoft.Data.Sqlite;
using Windows.Networking;
using Windows.Storage;

namespace Client_System_C_
{
    internal static class DataAcess
    {
        private static SqliteConnection? db;
        static readonly Dictionary<string, (string createSql, string[] cols)> Expected = new()
        {
            ["Users"] = (
                @"CREATE TABLE IF NOT EXISTS {0} (
                    internalId INTEGER PRIMARY KEY AUTOINCREMENT,
                    phone TEXT,
                    cpf TEXT,
                    idName TEXT,
                    contactName TEXT,
                    email TEXT,
                    cep TEXT,
                    address TEXT
                );",
                new[] { "internalId", "phone", "cpf", "idName", "contactName", "email", "cep", "address" }
            ),
            ["Purchases"] = (
                @"CREATE TABLE IF NOT EXISTS {0} (
                    purchaseId INTEGER PRIMARY KEY AUTOINCREMENT,
                    userId INTEGER,
                    date TEXT,
                    total_price REAL DEFAULT 0,
                    FOREIGN KEY (userId) REFERENCES Users(internalId) ON DELETE CASCADE
                );",
                new[] { "purchaseId", "userId", "date", "total_price" }
            ),
            ["PurchasesItems"] = (
                @"CREATE TABLE IF NOT EXISTS {0} (
                    itemId INTEGER PRIMARY KEY AUTOINCREMENT,
                    purchaseId INTEGER,
                    itemName TEXT,
                    quantity INTEGER,
                    price REAL,
                    discount TEXT,
                    FOREIGN KEY (purchaseId) REFERENCES Purchases(purchaseId) ON DELETE CASCADE
                );",
                new[] { "itemId", "purchaseId", "itemName", "quantity", "price", "discount" }
            ),
            ["Machines"] = (
                @"CREATE TABLE IF NOT EXISTS {0} (
                    internalId INTEGER PRIMARY KEY AUTOINCREMENT,
                    machineId TEXT,
                    machineModel TEXT,
                    ownerName TEXT,
                    ownerPhone TEXT,
                    ownerPhone2 TEXT
                );",
                new[] { "internalId", "machineId", "machineModel", "ownerName", "ownerPhone", "ownerPhone2" }
            ),
            ["Repairs"] = (
                @"CREATE TABLE IF NOT EXISTS {0} (
                    repairId INTEGER PRIMARY KEY AUTOINCREMENT,
                    description TEXT,
                    machineId INTEGER,
                    price REAL,
                    date TEXT,
                    serviceOrder TEXT,
                    done BOOLEAN,
                    FOREIGN KEY (machineId) REFERENCES Machines(internalId) ON DELETE CASCADE
                );",
                new[] { "repairId", "description", "machineId", "price", "date", "serviceOrder", "done" }
            ),
        };

        public async static void InitDataBase()
        {
            // Ensure database file exists
            await ApplicationData.Current.LocalFolder.CreateFileAsync("clientsDatabase.db", CreationCollisionOption.OpenIfExists);

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "clientsDatabase.db");

            // Create the connection once, store in static field
            db = new SqliteConnection($"Filename={dbPath}");
            db.Open();
            EnsureSchema(db);
        }

        public static void EnsureSchema(SqliteConnection db)
        {
            using var tx = db.BeginTransaction();
            // Temporarily disable FK checks while reshaping tables
            new SqliteCommand("PRAGMA foreign_keys=OFF;", db, tx).ExecuteNonQuery();

            foreach (var (tableName, schema) in Expected)
                EnsureTableMatches(db, tx, tableName, schema.createSql, schema.cols);

            // Validate FKs before committing
            new SqliteCommand("PRAGMA foreign_keys=ON;", db, tx).ExecuteNonQuery();
            var fkCheck = new SqliteCommand("PRAGMA foreign_key_check;", db, tx).ExecuteReader();
            if (fkCheck.Read())
                throw new InvalidOperationException($"Foreign key violation after migration: {fkCheck.GetString(0)}");

            tx.Commit();
        }

        static void EnsureTableMatches(SqliteConnection db, SqliteTransaction tx, string tableName, string createSqlFmt, string[] expectedCols)
        {
            bool exists = TableExists(db, tx, tableName);
            if (!exists)
            {
                Exec(db, tx, string.Format(createSqlFmt, QuoteIdent(tableName)));
                return;
            }

            var actualCols = GetColumns(db, tx, tableName); // name -> type/flags (not used here)
            bool same =
                expectedCols.SequenceEqual(actualCols.Select(c => c.name)) // exact order & names
                                                                           // If you don't care about order, use: SetEquals instead of SequenceEqual.
                ;

            if (same) return;

            // Rebuild: create _new table with desired schema, copy shared columns, swap.
            var tmp = $"_{tableName}_new_{Guid.NewGuid():N}".Substring(0, 28);
            Exec(db, tx, string.Format(createSqlFmt, QuoteIdent(tmp)));

            // columns present in both old and new (by name)
            var shared = expectedCols.Intersect(actualCols.Select(c => c.name))
                                     .Where(c => !IsRowidPrimaryKey(tableName, c))
                                     .ToArray();

            if (shared.Length > 0)
            {
                var colList = string.Join(",", shared.Select(QuoteIdent));
                Exec(db, tx, $"INSERT INTO {QuoteIdent(tmp)} ({colList}) SELECT {colList} FROM {QuoteIdent(tableName)};");
            }

            // Drop old and rename new
            Exec(db, tx, $"DROP TABLE {QuoteIdent(tableName)};");
            Exec(db, tx, $"ALTER TABLE {QuoteIdent(tmp)} RENAME TO {QuoteIdent(tableName)};");
        }

        static bool TableExists(SqliteConnection db, SqliteTransaction tx, string tableName)
        {
            using var cmd = new SqliteCommand(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n LIMIT 1;", db, tx);
            cmd.Parameters.AddWithValue("@n", tableName);
            return cmd.ExecuteScalar() != null;
        }

        static (string name, string type, bool notnull, bool pk, string dflt)[] GetColumns(SqliteConnection db, SqliteTransaction tx, string table)
        {
            using var cmd = new SqliteCommand($"PRAGMA table_info({QuoteIdent(table)});", db, tx);
            using var r = cmd.ExecuteReader();
            var list = new List<(string, string, bool, bool, string)>();
            while (r.Read())
            {
                var name = r.GetString(1);
                var type = r.IsDBNull(2) ? "" : r.GetString(2);
                var notnull = r.GetInt32(3) == 1;
                var dflt = r.IsDBNull(4) ? null : r.GetString(4);
                var pk = r.GetInt32(5) == 1;
                list.Add((name, type, notnull, pk, dflt));
            }
            return list.ToArray();
        }

        static void Exec(SqliteConnection db, SqliteTransaction tx, string sql)
        {
            using var cmd = new SqliteCommand(sql, db, tx);
            cmd.ExecuteNonQuery();
        }

        static string QuoteIdent(string ident) => $"\"{ident.Replace("\"", "\"\"")}\"";
        static bool IsRowidPrimaryKey(string table, string col) => false;

        public static void InsertUser(string cpf, string idName, string contactName, string email, string phone, string cep, string address)
        {
            string sql = @"
                INSERT INTO Users (cpf, idName, contactName, email, phone, cep, address) 
                VALUES (@cpf, @idName, @contactName, @email, @phone, @cep, @address);
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@cpf", cpf);
            cmd.Parameters.AddWithValue("@idName", idName);
            cmd.Parameters.AddWithValue("@contactName", contactName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@cep", cep);
            cmd.Parameters.AddWithValue("@address", address);

            cmd.ExecuteNonQuery();
        }

        public static int InsertPurchase(int userInternalId, string date)
        {
            string sql = @"
                INSERT INTO Purchases (userId, date) VALUES (@userInternalId, @date);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@userInternalId", userInternalId);
            cmd.Parameters.AddWithValue("@date", date);

            object? result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public static void InsertPurchaseItem(int purchaseId, string itemName, int quantity, double price, string discount)
        {
            string sql = @"
                INSERT INTO PurchasesItems (purchaseId, itemName, quantity, price, discount) 
                VALUES (@purchaseId, @itemName, @quantity, @price, @discount);
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
            cmd.Parameters.AddWithValue("@itemName", itemName);
            cmd.Parameters.AddWithValue("@quantity", quantity);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@discount", discount);

            cmd.ExecuteNonQuery();
        }

        public static void UpdateTotalPrice(int purchaseId)
        {
            string sql = @"
                UPDATE Purchases 
                SET total_price = (
                    SELECT SUM(quantity * price) 
                    FROM PurchasesItems 
                    WHERE purchaseId = @purchaseId
                ) 
                WHERE purchaseId = @purchaseId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);

            cmd.ExecuteNonQuery();
        }

        public class PurchaseHistory
        {
            public int PurchaseId { get; set; }
            public DateOnly Date { get; set; }
            public double TotalPrice { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public double Price { get; set; }
            public string Discount { get; set; } = string.Empty;
        }

        public static List<PurchaseHistory> GetPurchaseHistory(int userId)
        {
            string sql = @"
                SELECT P.purchaseId, 
                       P.date, 
                       P.total_price, 
                       I.itemName, 
                       I.quantity, 
                       I.price,
                       I.discount
                FROM Purchases P
                JOIN PurchasesItems I ON P.purchaseId = I.purchaseId
                WHERE P.userId = @userId
                ORDER BY P.date DESC;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@userId", userId);

            var historyList = new List<PurchaseHistory>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var record = new PurchaseHistory
                {
                    PurchaseId = reader.GetInt32(0),
                    Date = DateOnly.Parse(reader.GetString(1)),
                    TotalPrice = reader.GetDouble(2),
                    ItemName = reader.GetString(3),
                    Quantity = reader.GetInt32(4),
                    Price = reader.GetDouble(5),
                    Discount = reader.GetString(6)
                };
                historyList.Add(record);
            }

            return historyList;
        }

        public class User
        {
            public int InternalId { get; set; } = -1;
            public string CPF { get; set; } = string.Empty;
            public string IdName { get; set; } = string.Empty;
            public string ContactName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string CEP { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string AdressNumber { get; set; } = string.Empty;
            public string Neighboorhood { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
        }

        public static User? GetUser(int internalId)
        {
            string sql = "SELECT internalId, cpf, idName, contactName, email, phone, cep, address FROM Users WHERE internalId = @internalId LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@internalId", internalId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    IdName = reader.GetString(2),
                    ContactName = reader.GetString(3),
                    Email = reader.GetString(4),
                    Phone = reader.GetString(5),
                    CEP = reader.GetString(6)
                };

                string addressData = reader.GetString(7);
                var addressParts = addressData.Split(',');

                if (addressParts.Length == 4)
                {
                    user.Street = addressParts[0].Trim();
                    user.AdressNumber = addressParts[1].Trim();
                    user.Neighboorhood = addressParts[2].Trim();
                    user.City = addressParts[3].Trim();
                }

                return user;
            }
            return null;
        }

        public static User? GetUserByCpf(string cpf)
        {
            string sql = "SELECT internalId, cpf, idName, contactName, email, phone, cep, address FROM Users WHERE cpf = @cpf LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@cpf", cpf);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    IdName = reader.GetString(2),
                    ContactName = reader.GetString(3),
                    Email = reader.GetString(4),
                    Phone = reader.GetString(5),
                    CEP = reader.GetString(6)
                };

                string addressData = reader.GetString(7);
                var addressParts = addressData.Split(',');

                if (addressParts.Length == 4)
                {
                    user.Street = addressParts[0].Trim();
                    user.AdressNumber = addressParts[1].Trim();
                    user.Neighboorhood = addressParts[2].Trim();
                    user.City = addressParts[3].Trim();
                }

                return user;
            }
            return null;
        }

        public static User? GetUserByLastName(string contactName)
        {
            string sql = @"SELECT internalId, cpf, idName, contactName, email, phone, cep, address FROM Users WHERE contactName = @contactName LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@contactName", contactName);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    IdName = reader.GetString(2),
                    ContactName = reader.GetString(3),
                    Email = reader.GetString(4),
                    Phone = reader.GetString(5),
                    CEP = reader.GetString(6)
                };

                string addressData = reader.GetString(7);
                var addressParts = addressData.Split(',');

                if (addressParts.Length == 4)
                {
                    user.Street = addressParts[0].Trim();
                    user.AdressNumber = addressParts[1].Trim();
                    user.Neighboorhood = addressParts[2].Trim();
                    user.City = addressParts[3].Trim();
                }

                return user;
            }
            return null;
        }

        public static User? GetUserByPhone(string phone)
        {
            string sql = "SELECT internalId, cpf, idName, contactName, email, phone, cep, address FROM Users WHERE phone = @phone LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@phone", phone);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    IdName = reader.GetString(2),
                    ContactName = reader.GetString(3),
                    Email = reader.GetString(4),
                    Phone = reader.GetString(5),
                    CEP = reader.GetString(6)
                };

                string addressData = reader.GetString(7);
                var addressParts = addressData.Split(',');

                if (addressParts.Length == 4)
                {
                    user.Street = addressParts[0].Trim();
                    user.AdressNumber = addressParts[1].Trim();
                    user.Neighboorhood = addressParts[2].Trim();
                    user.City = addressParts[3].Trim();
                }

                return user;
            }
            return null;
        }

        public static bool RemoveUser(int internalId)
        {
            string sql = "DELETE FROM Users WHERE internalId = @internalId;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@internalId", internalId);

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public static List<User> GetAllUsers()
        {
            string sql = "SELECT internalId, cpf, idName, contactName, email, phone FROM Users ORDER BY idName, contactName;";

            using var cmd = new SqliteCommand(sql, db);
            var users = new List<User>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    IdName = reader.GetString(2),
                    ContactName = reader.GetString(3),
                    Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Phone = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                });
            }
            return users;
        }

        public static void UpdateUser(int internalId, string idName, string contactName, string email, string phone)
        {
            string sql = @"
                UPDATE Users 
                SET idName = @idName, 
                    contactName = @contactName, 
                    email = @email,
                    phone = @phone
                WHERE internalId = @internalId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@idName", idName);
            cmd.Parameters.AddWithValue("@contactName", contactName);
            cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? string.Empty : email);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@internalId", internalId);

            cmd.ExecuteNonQuery();
        }

        public static void InsertMachine(string machineId, string machineModel, string ownerName, string ownerPhone, string ownerPhone2)
        {
            string sql = @"
                INSERT INTO Machines (machineId, machineModel, ownerName, ownerPhone, ownerPhone2) 
                VALUES (@machineId, @machineModel, @ownerName, @ownerPhone, @ownerPhone2);
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);
            cmd.Parameters.AddWithValue("@machineModel", machineModel);
            cmd.Parameters.AddWithValue("@ownerName", ownerName);
            cmd.Parameters.AddWithValue("@ownerPhone", ownerPhone);
            cmd.Parameters.AddWithValue("@ownerPhone2", ownerPhone2);

            cmd.ExecuteNonQuery();
        }

        public static int InsertRepair(int machineInternalId, string descripition, double price, string date, string serviceOrder)
        {
            string sql = @"
                INSERT INTO Repairs (machineId, description, price, date, serviceOrder, done) 
                VALUES (@machineInternalId, @description, @price, @date, @serviceOrder, @done);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineInternalId", machineInternalId);
            cmd.Parameters.AddWithValue("@description", descripition);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@serviceOrder", serviceOrder);
            cmd.Parameters.AddWithValue("@done", false);

            object? result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public class Machine
        {
            public int InternalId { get; set; } = -1;
            public string MachineId { get; set; } = string.Empty;
            public string MachineModel { get; set; } = string.Empty;
            public string OwnerName { get; set; } = string.Empty;
            public string OwnerPhone { get; set; } = string.Empty;
            public string OwnerPhone2 { get; set; } = string.Empty;
        }

        public class Repair
        {
            public int RepairId { get; set; }
            public string MachineId { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public double Price { get; set; }
            public string Date { get; set; } = string.Empty;
            public string ServiceOrder { get; set; }
            public bool Done { get; set; } = false;

            public override string ToString()
            {
                return $"{Description}";
            }
        }

        public static Machine? GetMachine(int internalId)
        {
            string sql = "SELECT internalId, machineId, machineModel, ownerName, ownerPhone, ownerPhone2 FROM Machines WHERE internalId = @internalId LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@internalId", internalId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Machine
                {
                    InternalId = reader.GetInt32(0),
                    MachineId = reader.GetString(1),
                    MachineModel = reader.GetString(2),
                    OwnerName = reader.GetString(3),
                    OwnerPhone = reader.GetString(4),
                    OwnerPhone2 = reader.GetString(5)
                };
            }
            return null;
        }

        public static Machine? GetMachineById(string machineId)
        {
            string sql = "SELECT internalId, machineId, machineModel, ownerName, ownerPhone, ownerPhone2 FROM Machines WHERE machineId = @machineId LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Machine
                {
                    InternalId = reader.GetInt32(0),
                    MachineId = reader.GetString(1),
                    MachineModel = reader.GetString(2),
                    OwnerName = reader.GetString(3),
                    OwnerPhone = reader.GetString(4),
                    OwnerPhone2 = reader.GetString(5)
                };
            }
            return null;
        }

        public static List<Repair> GetRepairsFromMachine(int machineInternalId)
        {
            string sql = "SELECT repairId, machineId, description, price, date, serviceOrder, done FROM Repairs WHERE machineId = @machineInternalId ORDER BY date DESC;";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineInternalId", machineInternalId);

            var repairList = new List<Repair>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                repairList.Add(new Repair
                {
                    RepairId = reader.GetInt32(0),
                    MachineId = reader.GetString(1),
                    Description = reader.GetString(2),
                    Price = reader.GetDouble(3),
                    Date = reader.GetString(4),
                    ServiceOrder = reader.GetString(5),
                    Done = reader.GetBoolean(6)
                });
            }
            return repairList;
        }

        public static void UpdateMachine(int internalId, string machineModel, string ownerName, string ownerPhone, string ownerPhone2)
        {
            string sql = @"
                UPDATE Machines 
                SET machineModel = @machineModel, 
                    ownerName = @ownerName, 
                    ownerPhone = @ownerPhone, 
                    ownerPhone2 = @ownerPhone2 
                WHERE internalId = @internalId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@internalId", internalId);
            cmd.Parameters.AddWithValue("@machineModel", machineModel);
            cmd.Parameters.AddWithValue("@ownerName", ownerName);
            cmd.Parameters.AddWithValue("@ownerPhone", ownerPhone);
            cmd.Parameters.AddWithValue("@ownerPhone2", ownerPhone2);

            cmd.ExecuteNonQuery();
        }

        public static List<Machine> GetAllMachines()
        {
            string sql = @"
                SELECT internalId, machineId, machineModel, ownerName, ownerPhone, ownerPhone2 
                FROM Machines
                ORDER BY machineModel;
            ";

            var machines = new List<Machine>();

            using var cmd = new SqliteCommand(sql, db);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                machines.Add(new Machine
                {
                    InternalId = reader.GetInt32(0),
                    MachineId = reader.GetString(1),
                    MachineModel = reader.GetString(2),
                    OwnerName = reader.GetString(3),
                    OwnerPhone = reader.GetString(4),
                    OwnerPhone2 = reader.GetString(5)
                });
            }
            return machines;
        }

        public static void RemoveMachine(int internalId)
        {
            string sql = "DELETE FROM Machines WHERE internalId = @internalId;";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@internalId", internalId);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateRepair(int repairId, string description, string serviceOrder, bool done)
        {
            string sql = @"
                UPDATE Repairs
                SET description = @description, serviceOrder = @serviceOrder, done = @done
                WHERE repairId = @repairId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@repairId", repairId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@serviceOrder", serviceOrder);
            cmd.Parameters.AddWithValue("@done", done);

            cmd.ExecuteNonQuery();
        }

        public static void CloseConnection()
        {
            if (db != null)
            {
                db.Close();
                db.Dispose();
                db = null;
            }
        }
    }
}
