# **In-Depth Technical Report: Building a Discord Bot with C#, SQLite, and Discord.Net**

## **Table of Contents**

1. **Introduction**  
2. **System Architecture and Design Decisions**  
   - Overview of the System  
   - Project Structure and Layered Design  
   - Key Technologies Used  
3. **Database Design and Management**  
   - Relational Database Schema  
   - Data Normalization in SQLite  
   - Indexing Strategies and Query Optimization  
   - Handling Schema Migrations  
4. **Asynchronous Programming in C#**  
   - Overview of `async` and `await`  
   - Task Parallelism and `Task.Run()`  
   - Managing Database Locks and Concurrency  
5. **Discord Interaction Model**  
   - Slash Commands and Command Builders  
   - Implementing Modals for User Input  
   - Handling Events and Asynchronous API Calls  
6. **User Data and Profile Management**  
   - User Data Model  
   - Profiles Table Schema  
   - CRUD Operations with SQLite  
   - Transactions and Data Integrity  
7. **Performance Optimization Strategies**  
   - Caching User Data to Minimize Queries  
   - Connection Pooling and Efficient Database Queries  
   - Reducing API Call Overhead  
   - Profiling Memory and CPU Usage  
8. **Security Considerations**  
   - Preventing SQL Injection  
   - Handling User Input Safely  
   - Implementing Rate Limits  
9. **Logging, Debugging, and Monitoring**  
   - Using Serilog for Structured Logging  
   - Debugging Asynchronous Code  
   - Monitoring Performance with Benchmarks  
10. **Future Enhancements and Scalability Considerations**  

---

# **1. Introduction**

This technical report outlines the **architecture, implementation, and optimization techniques** used to build a **Discord bot** using `C#` with `Discord.Net` and `SQLite`. The bot supports **user profiles**, **truth-or-dare prompts**, and **asynchronous command handling**.

The report will cover **database schema design, asynchronous programming challenges, API integration, caching, security, and performance optimizations**, ensuring a **scalable and maintainable system**.

---

# **2. System Architecture and Design Decisions**

## **Overview of the System**

The bot follows a **modular architecture**, ensuring **separation of concerns**:

- **Data Layer**: Manages SQLite interactions (`DatabaseService.cs`).
- **Business Logic Layer**: Implements bot functionality (commands, profile management).
- **API Integration Layer**: Handles interactions with Discord API (`Discord.Net`).

### **Project Structure and Layered Design**
```
/Faye
  /Commands
    ProfileCommands.cs
    ProfileModalHandler.cs
  /Data
    Models.cs
  /Services
    DatabaseService.cs
  Program.cs
```

- **Commands/** → Handles Discord slash commands and user interactions.
- **Data/** → Defines C# models mapping to SQLite tables.
- **Services/** → Implements data persistence logic.

### **Key Technologies Used**
- **C# 10+** → Strongly typed, high-performance backend.
- **Discord.Net** → Official Discord API wrapper for bot interactions.
- **SQLite** → Lightweight, embedded database for persistent storage.
- **Task-Based Asynchronous Pattern (TAP)** → Non-blocking operations.

---

# **3. Database Design and Management**

## **Relational Database Schema**
The database consists of **four primary tables**:

```sql
CREATE TABLE Users (
    UserId TEXT PRIMARY KEY,
    Username TEXT,
    Discriminator TEXT,
    XP INTEGER DEFAULT 0,
    Level INTEGER DEFAULT 0,
    LastMessageTime TEXT
);

CREATE TABLE Profiles (
    UserId TEXT PRIMARY KEY,
    Bio TEXT,
    Interests TEXT,
    Kinks TEXT,
    Limits TEXT,
    Age INTEGER,
    Gender TEXT,
    FOREIGN KEY(UserId) REFERENCES Users(UserId)
);
```

## **Data Normalization in SQLite**
- **1NF (Atomic Columns)**: Each field contains **atomic values** (e.g., `Bio`, `Interests`).
- **2NF (No Partial Dependencies)**: No column is dependent on part of a composite primary key.
- **3NF (No Transitive Dependencies)**: Every non-key column is functionally dependent on `UserId`.

## **Indexing Strategies and Query Optimization**
Indexes reduce lookup times but require extra space:

```sql
CREATE INDEX idx_profiles_userid ON Profiles (UserId);
```
- **Speeds up queries like**:
```sql
SELECT * FROM Profiles WHERE UserId = '123456789';
```

## **Handling Schema Migrations**
SQLite lacks built-in migrations, so we manually check **PRAGMA table_info()**:

```csharp
cmd.CommandText = "PRAGMA table_info(Profiles);";
```
- If the column doesn’t exist, **ALTER TABLE** is used dynamically.

---

# **4. Asynchronous Programming in C#**

## **Overview of `async` and `await`**
C# uses `async`/`await` for **non-blocking** operations:

```csharp
public async Task<UserData?> GetUserAsync(ulong userId)
{
    await using var connection = new SQLiteConnection(_connectionString);
    await connection.OpenAsync();

    const string query = "SELECT * FROM Users WHERE UserId = @UserId";
    using var cmd = new SQLiteCommand(query, connection);
    cmd.Parameters.AddWithValue("@UserId", userId.ToString());

    using var reader = await cmd.ExecuteReaderAsync();
    if (reader.Read())
    {
        return new UserData { UserId = ulong.Parse(reader["UserId"].ToString()) };
    }
    return null;
}
```
- **`await using`** → Ensures proper disposal of resources.
- **`await OpenAsync()`** → Prevents UI thread blocking.

## **Managing Database Locks and Concurrency**
- SQLite locks entire tables for writes → **use transactions carefully**:
```csharp
using var transaction = connection.BeginTransaction();
```
- **Avoid deadlocks** by using **isolated reads** when possible.

---

# **5. Discord Interaction Model**

## **Slash Commands and Command Builders**
Discord slash commands are **registered dynamically**:

```csharp
[SlashCommand("profile", "View your profile")]
public async Task ViewProfile()
{
    var userProfile = await _db.GetUserProfileAsync(Context.User.Id);
    await RespondAsync(embed: BuildProfileEmbed(userProfile));
}
```

## **Implementing Modals for User Input**
```csharp
[ModalInteraction("profile_modal")]
public async Task HandleProfileModal(ProfileModal modal)
{
    var profileData = new ProfileData
    {
        Bio = modal.Bio ?? "",
        Interests = modal.Interests ?? "",
        Kinks = modal.Kinks ?? "",
        Limits = modal.Kinks ?? "",
        Age = int.TryParse(modal.Age, out int age) ? age : 0
    };
    await _db.UpdateUserProfileAsync(Context.User.Id, profileData);
}
```
- **`ModalInteraction`** → Handles UI-based form submissions.

---

# **6. Performance Optimization Strategies**
## **Caching User Data to Minimize Queries**
- **Use in-memory caching** for frequently accessed data:
```csharp
private readonly Dictionary<ulong, UserData> _userCache = new();
```

## **Connection Pooling and Efficient Queries**
- Avoid opening **multiple DB connections** in a single request.
- Use **batch queries** when possible.

---

# **7. Security Considerations**
## **Preventing SQL Injection**
Always use **parameterized queries**:
```csharp
cmd.Parameters.AddWithValue("@UserId", userId.ToString());
```

## **Implementing Rate Limits**
To prevent API spam:
```csharp
if (_rateLimiter.IsLimited(Context.User.Id)) return;
```

---

# **8. Logging, Debugging, and Monitoring**
## **Using Serilog for Structured Logging**
```csharp
Log.Information("User {UserId} updated profile", Context.User.Id);
```
---

# **9. Conclusion and Future Enhancements**
- Implement **web dashboard** for user profiles.
- Optimize **query performance with indexing**.
- Enhance **logging for debugging asynchronous code**.

---
*End of Report*
