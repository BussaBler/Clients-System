using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using Microsoft.Data.Sqlite;
using Windows.Networking;
using Windows.Storage;

namespace Client_System_C_
{
    internal static class DataAcess
    {
        // Single connection for all operations
        private static SqliteConnection? db;

        /// <summary>
        /// Initializes the database file if not exists and opens a single static connection.
        /// </summary>
        public async static void InitDataBase()
        {
            // Ensure database file exists
            await ApplicationData.Current.LocalFolder.CreateFileAsync("clientsDatabase.db", CreationCollisionOption.OpenIfExists);

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "clientsDatabase.db");

            // Create the connection once, store in static field
            db = new SqliteConnection($"Filename={dbPath}");
            db.Open();

            // DDL (CREATE TABLE IF NOT EXISTS ...)
            string userTableCommand = @"
                CREATE TABLE IF NOT EXISTS Users (
                    internalId INTEGER PRIMARY KEY AUTOINCREMENT,
                    phone TEXT,
                    cpf TEXT,
                    firstName TEXT,
                    lastName TEXT,
                    email TEXT,
                    cep TEXT,
                    address TEXT
                );
            ";

            string purchaseTableCommand = @"
                CREATE TABLE IF NOT EXISTS Purchases (
                    purchaseId INTEGER PRIMARY KEY AUTOINCREMENT,
                    userId INTEGER,
                    date TEXT,
                    total_price REAL DEFAULT 0,
                    FOREIGN KEY (userId) REFERENCES Users(internalId) ON DELETE CASCADE
                );
            ";

            string itemTableCommand = @"
                CREATE TABLE IF NOT EXISTS PurchasesItems (
                    itemId INTEGER PRIMARY KEY AUTOINCREMENT,
                    purchaseId INTEGER,
                    itemName TEXT,
                    quantity INTEGER,
                    price REAL,
                    discount TEXT,
                    FOREIGN KEY (purchaseId) REFERENCES Purchases(purchaseId) ON DELETE CASCADE
                );
            ";

            string machineTableCommand = @"
                CREATE TABLE IF NOT EXISTS Machines (
                    internalId INTEGER PRIMARY KEY AUTOINCREMENT,
                    machineId TEXT,
                    machineModel TEXT,
                    ownerName TEXT,
                    ownerPhone TEXT,
                    ownerPhone2 TEXT
                );
            ";

            string repairsTableCommand = @"
                CREATE TABLE IF NOT EXISTS Repairs (
                    repairId INTEGER PRIMARY KEY AUTOINCREMENT,
                    description TEXT,
                    machineId INTEGER,
                    price REAL,
                    date TEXT,
                    done BOOLEAN,
                    FOREIGN KEY (machineId) REFERENCES Machines(internalId) ON DELETE CASCADE
                );
            ";

            using (var createUserTable = new SqliteCommand(userTableCommand, db))
            {
                createUserTable.ExecuteNonQuery();
            }

            using (var createPurchaseTable = new SqliteCommand(purchaseTableCommand, db))
            {
                createPurchaseTable.ExecuteNonQuery();
            }

            using (var createItemTable = new SqliteCommand(itemTableCommand, db))
            {
                createItemTable.ExecuteNonQuery();
            }

            using (var createMachineTable = new SqliteCommand(machineTableCommand, db))
            {
                createMachineTable.ExecuteNonQuery();
            }

            using (var createRepairsTable = new SqliteCommand(repairsTableCommand, db))
            {
                createRepairsTable.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert a new user record into the 'Users' table.
        /// </summary>
        public static void InsertUser(string cpf, string firstName, string lastName, string email, string phone, string cep, string address)
        {
            string sql = @"
                INSERT INTO Users (cpf, firstName, lastName, email, phone, cep, address) 
                VALUES (@cpf, @firstName, @lastName, @email, @phone, @cep, @address);
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@cpf", cpf);
            cmd.Parameters.AddWithValue("@firstName", firstName);
            cmd.Parameters.AddWithValue("@lastName", lastName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@cep", cep);
            cmd.Parameters.AddWithValue("@address", address);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Insert a new purchase record, returning the 'purchaseId'.
        /// </summary>
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

        /// <summary>
        /// Insert an item for a given purchase.
        /// </summary>
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

        /// <summary>
        /// Recalculates total_price for a purchase based on items (quantity * price).
        /// </summary>
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

        /// <summary>
        /// Represents a row in the join of Purchases and PurchasesItems.
        /// </summary>
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

        /// <summary>
        /// Retrieves all purchases + items for a given CPF, ordered by date descending.
        /// </summary>
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
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;    
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string CEP {  get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string AdressNumber {  get; set; } = string.Empty;
            public string Neighboorhood { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
        }

        public static User? GetUser(int internalId)
        {
            string sql = "SELECT internalId, cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE internalId = @internalId LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@internalId", internalId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
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
            string sql = "SELECT internalId, cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE cpf = @cpf LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@cpf", cpf);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
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

        public static User? GetUserByLastName(string lastName)
        {
            string sql = @"SELECT internalId, cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE lastName = @lastName LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@lastName", lastName);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
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
            string sql = "SELECT internalId, cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE phone = @phone LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@phone", phone);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
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
            string sql = "SELECT internalId, cpf, firstName, lastName, email, phone FROM Users ORDER BY firstName, lastName;";

            using var cmd = new SqliteCommand(sql, db);
            var users = new List<User>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    InternalId = reader.GetInt32(0),
                    CPF = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Phone = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                });
            }
            return users;
        }

        public static void UpdateUser(int internalId, string firstName, string lastName, string email, string phone)
        {
            string sql = @"
                UPDATE Users 
                SET firstName = @firstName, 
                    lastName = @lastName, 
                    email = @email,
                    phone = @phone
                WHERE internalId = @internalId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@firstName", firstName);
            cmd.Parameters.AddWithValue("@lastName", lastName);
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

        public static int InsertRepair(int machineInternalId, string descripition, double price, string date)
        {
            string sql = @"
                INSERT INTO Repairs (machineId, description, price, date, done) 
                VALUES (@machineInternalId, @description, @price, @date, @done);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineInternalId", machineInternalId);
            cmd.Parameters.AddWithValue("@description", descripition);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@date", date);
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
            public bool Done { get; set; } = false;
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
            string sql = "SELECT repairId, machineId, description, price, date, done FROM Repairs WHERE machineId = @machineInternalId ORDER BY date DESC;";

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
                    Done = reader.GetBoolean(5)
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

        public static void UpdateRepair(int repairId, string description, bool done)
        {
            string sql = @"
                UPDATE Repairs
                SET description = @description, done = @done
                WHERE repairId = @repairId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@repairId", repairId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@done", done);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// (Optional) Closes the static db connection. Call this when the app is shutting down.
        /// </summary>
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
