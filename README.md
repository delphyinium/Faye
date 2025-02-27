# In-Depth Technical Report: Building a Discord Bot with C#, SQLite, and Discord.Interactions

## Table of Contents

1. [Introduction](#introduction)  
2. [System Architecture and Design Decisions](#architecture)  
   - [Overview](#overview)  
   - [Project Structure](#project-structure)  
3. [Database Integration with SQLite](#database-integration)  
   - [Schema Design and Migration](#schema-design)  
   - [Using SQLite in C#](#using-sqlite)  
   - [Asynchronous Database Access and Resource Management](#async-db)  
4. [Discord Bot Interaction Model](#discord-interactions)  
   - [Slash Commands and Modal Dialogs](#commands-modals)  
   - [Implementing IModal for User Input](#implementing-imodal)  
5. [Command Handling and Profile Management](#command-handling)  
   - [User Data and Profile Data Models](#data-models)  
   - [Profile Command Workflow](#profile-workflow)  
6. [Advanced Topics](#advanced-topics)  
   - [Error Handling and Debugging](#error-handling)  
   - [Performance Considerations and Optimizations](#performance)  
   - [Security: SQL Parameterization and Best Practices](#security)  
7. [Conclusion and Future Work](#conclusion)  

---

## 1. Introduction <a name="introduction"></a>

This project demonstrates how to create a robust Discord bot using C# as the primary language, integrating SQLite for persistent data storage, and leveraging the Discord.Interactions API for modern interaction-based commands. The bot is designed to manage user profiles with rich data (such as bio, interests, age, gender, and a combined field for kinks & limits) while ensuring high performance through asynchronous programming and proper resource management.

This report delves into the architecture, design decisions, and implementation details. It includes code snippets, technical explanations, and discussions of advanced topics to serve as a learning guide for someone seeking to understand C# and modern bot development.

---

## 2. System Architecture and Design Decisions <a name="architecture"></a>

### Overview <a name="overview"></a>

The overall system comprises several key components:

- **Discord Bot Core:** Built using Discord.Net to interact with the Discord API. Commands are implemented as slash commands and modal dialogs are used for interactive user input.
- **Data Layer:** An SQLite database stores user profiles and game-related prompts (Truth or Dare). This is abstracted by a dedicated `DatabaseService`.
- **Business Logic:** Command handlers and modal interaction handlers tie user inputs to database operations, culminating in the construction and display of rich Discord embed messages.

### Project Structure <a name="project-structure"></a>

A typical directory layout might be as follows:

/Faye /Commands ProfileCommands.cs ProfileModalHandler.cs /Data Models.cs /Services DatabaseService.cs Program.cs


- **Commands:** Contains classes that define slash commands and modal interactions.
- **Data:** Contains model classes representing the database schema.
- **Services:** Contains the database service handling all persistence operations.
- **Program.cs:** The entry point for initializing and running the bot.

---

## 3. Database Integration with SQLite <a name="database-integration"></a>

### Schema Design and Migration <a name="schema-design"></a>

The database schema includes tables such as `Users`, `Profiles`, `TruthPrompts`, and `DarePrompts`. In particular, the `Profiles` table was designed to capture a variety of user data. Over time, the schema evolved—for example, a new column `Limits` was added. The `DatabaseService` class checks the schema (using a PRAGMA query) and dynamically alters the table if needed. This migration strategy allows the application to evolve without data loss.

#### Code Snippet: Schema Migration

```csharp
// Check if the 'Limits' column exists in the Profiles table
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
    // If missing, add the column dynamically.
    cmd.CommandText = "ALTER TABLE Profiles ADD COLUMN Limits TEXT;";
    cmd.ExecuteNonQuery();
}
