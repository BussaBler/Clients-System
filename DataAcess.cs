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
                    phone TEXT PRIMARY KEY,
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
                    phone TEXT,
                    date TEXT,
                    total_price REAL DEFAULT 0,
                    FOREIGN KEY (phone) REFERENCES Users(phone) ON DELETE CASCADE
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
                    machineId TEXT PRIMARY KEY,
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
                    machineID TEXT,
                    price REAL,
                    date TEXT,
                    done BOOLEAN,
                    FOREIGN KEY (machineId) REFERENCES Machines(machineId) ON DELETE CASCADE
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
        public static int InsertPurchase(string phone, string date)
        {
            string sql = @"
                INSERT INTO Purchases (phone, date) VALUES (@phone, @date);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@phone", phone);
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
        public static List<PurchaseHistory> GetPurchaseHistory(string phone)
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
                WHERE P.phone = @phone
                ORDER BY P.date DESC;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@phone", phone);

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

        public static User? GetUser(string cpf)
        {
            string sql = "SELECT cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE cpf = @cpf LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@cpf", cpf);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    CPF = reader.GetString(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.GetString(3),
                    Phone = reader.GetString(4),
                    CEP = reader.GetString(5)
                };

                string addressData = reader.GetString(6);
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
            string sql = @"SELECT cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE lastName = @lastName LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@lastName", lastName);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    CPF = reader.GetString(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.GetString(3),
                    Phone = reader.GetString(4),
                    CEP = reader.GetString(5)
                };

                string addressData = reader.GetString(6);
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
            string sql = "SELECT cpf, firstName, lastName, email, phone, cep, address FROM Users WHERE phone = @phone LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@phone", phone);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    CPF = reader.GetString(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.GetString(3),
                    Phone = reader.GetString(4),
                    CEP = reader.GetString(5)
                };

                string addressData = reader.GetString(6);
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

        public static bool RemoveUser(string cpf)
        {
            string sql = "DELETE FROM Users WHERE cpf = @cpf;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@cpf", cpf);

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public static List<User> GetAllUsers()
        {
            string sql = "SELECT cpf, firstName, lastName, email, phone FROM Users ORDER BY firstName, lastName;";

            using var cmd = new SqliteCommand(sql, db);
            var users = new List<User>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    CPF = reader.GetString(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                });
            }
            return users;
        }

        public static void UpdateUser(string id, string firstName, string lastName, string email, string phone)
        {
            string sql = @"
                UPDATE Users 
                SET firstName = @firstName, 
                    lastName = @lastName, 
                    email = @email 
                WHERE phone = @phone;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@firstName", firstName);
            cmd.Parameters.AddWithValue("@lastName", lastName);
            cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? string.Empty : email);
            cmd.Parameters.AddWithValue("@phone", phone);

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

        public static int InsertRepair(string machineId, string descripition, double price, string date)
        {
            string sql = @"
                INSERT INTO Repairs (machineId, description, price, date, done) 
                VALUES (@machineId, @description, @price, @date, @done);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);
            cmd.Parameters.AddWithValue("@description", descripition);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@done", false);

            object? result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public class Machine
        {
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

        public static Machine? GetMachine(string machineId)
        {
            string sql = "SELECT machineId, machineModel, ownerName, ownerPhone, ownerPhone2 FROM Machines WHERE machineId = @machineId LIMIT 1;";
            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Machine
                {
                    MachineId = reader.GetString(0),
                    MachineModel = reader.GetString(1),
                    OwnerName = reader.GetString(2),
                    OwnerPhone = reader.GetString(3),
                    OwnerPhone2 = reader.GetString(4)
                };
            }
            return null;
        }

        public static List<Repair> GetRepairsFromMachine(string machineId)
        {
            string sql = "SELECT repairId, machineId, description, price, date, done FROM Repairs WHERE machineId = @machineId ORDER BY date DESC;";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);

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

        public static void UpdateMachine(string machineId, string machineModel, string ownerName, string ownerPhone, string ownerPhone2)
        {
            string sql = @"
                UPDATE Users 
                SET machineModel = @machineModel, 
                    ownerName = @ownerName, 
                    ownerPhone = @ownerPhone, 
                    ownerPhone2 = @ownerPhone2 
                WHERE machineId = @machineId;
            ";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);
            cmd.Parameters.AddWithValue("@machineModel", machineModel);
            cmd.Parameters.AddWithValue("@ownerName", ownerName);
            cmd.Parameters.AddWithValue("@ownerPhone", ownerPhone);
            cmd.Parameters.AddWithValue("@ownerPhone2", ownerPhone2);

            cmd.ExecuteNonQuery();
        }

        public static List<Machine> GetAllMachines()
        {
            string sql = @"
                SELECT machineId, machineModel, ownerName, ownerPhone, ownerPhone2 
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
                    MachineId = reader.GetString(0),
                    MachineModel = reader.GetString(1),
                    OwnerName = reader.GetString(2),
                    OwnerPhone = reader.GetString(3),
                    OwnerPhone2 = reader.GetString(4)
                });
            }
            return machines;
        }

        public static void RemoveMachine(string machineId)
        {
            string sql = "DELETE FROM Machines WHERE machineId = @machineId;";

            using var cmd = new SqliteCommand(sql, db);
            cmd.Parameters.AddWithValue("@machineId", machineId);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateRepair(string repairId, string description, bool done)
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
