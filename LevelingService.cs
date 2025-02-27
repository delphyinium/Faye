using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Faye.Data;

namespace Faye.Services
{
    public class LevelingService
    {
        private readonly DatabaseService _database;
        private readonly DiscordSocketClient _client;
        private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(1);
        private ITextChannel? _logChannel; // Made nullable

        // Role IDs
        private const ulong SELFIE_ACCESS_ROLE_ID = 1341645975118282772; // replace with your role ID
        private const ulong NUDES_ACCESS_ROLE_ID = 1344586127729758249; // replace with your role ID
        
        // Level requirements
        private const int SELFIE_ACCESS_LEVEL = 1; // Level 1
        private const int NUDES_ACCESS_LEVEL = 3; // Level 3
        
        // Log channel ID
        private const ulong XP_LOG_CHANNEL_ID = 1344760091127189614; // replace with your log channel ID

        public LevelingService(DatabaseService database, DiscordSocketClient client)
        {
            _database = database;
            _client = client;

            // Subscribe to message events
            _client.MessageReceived += OnMessageReceived;
            
            // Set up log channel when client is ready
            _client.Ready += InitializeService;
            
            Console.WriteLine("LevelingService constructor completed - waiting for client ready event");
        }
        
        // Changed to a non-async Task handler that calls an async method
        private Task InitializeService()
        {
            Console.WriteLine("Client Ready event triggered - setting up log channel");
            // Fire-and-forget but with proper error handling
            Task.Run(async () => 
            {
                try 
                {
                    await SetupLogChannel();
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine($"Critical error in InitializeService: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            });
            
            return Task.CompletedTask;
        }
        
        private async Task SetupLogChannel()
        {
            try 
            {
                Console.WriteLine($"Attempting to set up XP logging channel with ID: {XP_LOG_CHANNEL_ID}");
                
                var channel = _client.GetChannel(XP_LOG_CHANNEL_ID);
                Console.WriteLine($"GetChannel returned: {channel?.GetType().Name ?? "null"}");
                
                _logChannel = channel as ITextChannel;
                
                if (_logChannel != null)
                {
                    Console.WriteLine($"XP logging channel set up successfully: {_logChannel.Name} in guild {_logChannel.Guild.Name}");
                    
                    // Check permissions
                    bool hasPermissions = HasChannelPermissions(_logChannel);
                    if (!hasPermissions)
                    {
                        Console.WriteLine("WARNING: Bot may not have the required permissions in the log channel");
                    }
                    
                    // Send a test message to confirm it's working
                    await _logChannel.SendMessageAsync("XP logging system initialized at " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                    Console.WriteLine("Initialization message sent to log channel");
                }
                else
                {
                    Console.WriteLine($"ERROR: Could not find XP logging channel or cast to ITextChannel. Channel ID: {XP_LOG_CHANNEL_ID}");
                    
                    // Log available guilds and channels
                    foreach (var guild in _client.Guilds)
                    {
                        Console.WriteLine($"Guild: {guild.Name} (ID: {guild.Id})");
                        Console.WriteLine("Text channels:");
                        foreach (var textChannel in guild.TextChannels)
                        {
                            Console.WriteLine($"- {textChannel.Name} (ID: {textChannel.Id})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR setting up log channel: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private bool HasChannelPermissions(ITextChannel channel)
        {
            try
            {
                var currentUser = _client.CurrentUser;
                
                // Get guild as SocketGuild and then get user
                var socketGuild = channel.Guild as SocketGuild;
                if (socketGuild == null)
                {
                    Console.WriteLine("Could not cast IGuild to SocketGuild");
                    return false;
                }
                
                var guildUser = socketGuild.GetUser(currentUser.Id);
                if (guildUser == null)
                {
                    Console.WriteLine($"Could not find bot user in guild {socketGuild.Name}");
                    return false;
                }
                
                var permissions = guildUser.GetPermissions(channel);
                
                bool canSendMessages = permissions.SendMessages;
                bool canViewChannel = permissions.ViewChannel;
                bool canEmbedLinks = permissions.EmbedLinks;
                
                Console.WriteLine($"Bot permissions in channel {channel.Name}:");
                Console.WriteLine($"- Can view channel: {canViewChannel}");
                Console.WriteLine($"- Can send messages: {canSendMessages}");
                Console.WriteLine($"- Can embed links: {canEmbedLinks}");
                
                return canViewChannel && canSendMessages && canEmbedLinks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking permissions: {ex.Message}");
                return false;
            }
        }

        private async Task OnMessageReceived(SocketMessage rawMessage)
        {
            try
            {
                // Debug logging to see if messages are being received
                if (rawMessage.Content.Length > 0)
                {
                    Console.WriteLine($"Message received: {rawMessage.Content.Substring(0, Math.Min(10, rawMessage.Content.Length))}... from {rawMessage.Author.Username}");
                }
                else
                {
                    Console.WriteLine($"Empty message received from {rawMessage.Author.Username}");
                }
                
                // Quick filters to avoid unnecessary processing
                if (!(rawMessage is SocketUserMessage message)) 
                {
                    Console.WriteLine("Message is not a SocketUserMessage");
                    return;
                }
                
                if (message.Author.IsBot)
                {
                    Console.WriteLine("Message is from a bot, ignoring");
                    return;
                }
                
                // We only do leveling in guild channels
                var user = message.Author as SocketGuildUser;
                if (user == null)
                {
                    Console.WriteLine("Message is not from a guild user");
                    return;
                }

                // Check if message is a command (starts with prefix like !)
                if (message.Content.StartsWith('!'))
                {
                    Console.WriteLine("Message is a command, ignoring");
                    return;
                }

                Console.WriteLine($"Processing message for XP: {message.Content.Substring(0, Math.Min(20, message.Content.Length))}...");
                await ProcessMessageAsync(user, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnMessageReceived: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task ProcessMessageAsync(SocketGuildUser user, SocketUserMessage message)
        {
            try
            {
                Console.WriteLine($"Getting user data for {user.Username} (ID: {user.Id})");
                var userData = await _database.GetUserAsync(user.Id);
                if (userData == null)
                {
                    Console.WriteLine($"No existing user data for {user.Username}, creating new record");
                    userData = new UserData
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Discriminator = user.Discriminator,
                        XP = 0,
                        Level = 0,
                        LastMessageTime = DateTime.UtcNow.ToString("o")
                    };
                }
                else
                {
                    // Check cooldown
                    if (DateTime.TryParse(userData.LastMessageTime, out DateTime lastMsg))
                    {
                        TimeSpan timeSinceLastMessage = DateTime.UtcNow - lastMsg;
                        Console.WriteLine($"Time since last message: {timeSinceLastMessage.TotalMinutes:F2} minutes");
                        
                        if (timeSinceLastMessage < _cooldown)
                        {
                            Console.WriteLine($"User {user.Username} on cooldown, ignoring message");
                            return;
                        }
                    }
                }

                // Add XP - more for longer, meaningful messages
                int xpToAdd = CalculateXpForMessage(message);
                userData.XP += xpToAdd;

                int oldLevel = userData.Level;
                userData.Level = CalculateLevel(userData.XP);

                // Update last message time
                userData.LastMessageTime = DateTime.UtcNow.ToString("o");

                // Save
                Console.WriteLine($"Saving updated user data for {user.Username}: XP={userData.XP}, Level={userData.Level}");
                await _database.CreateOrUpdateUserAsync(userData);
                
                // Log XP award
                Console.WriteLine($"Logging XP award for {user.Username}: +{xpToAdd} XP");
                await LogXpAsync(user, xpToAdd, "Active chat", userData.XP, userData.Level);

                // Check level up
                if (userData.Level > oldLevel)
                {
                    Console.WriteLine($"User {user.Username} leveled up to level {userData.Level}!");
                    await SendLevelUpMessageAsync(user, userData.Level, message.Channel);

                    // Check for role assignments based on new level
                    if (userData.Level >= SELFIE_ACCESS_LEVEL)
                    {
                        await AssignRoleAsync(user, SELFIE_ACCESS_ROLE_ID, "Selfie Access");
                    }
                    
                    if (userData.Level >= NUDES_ACCESS_LEVEL)
                    {
                        await AssignRoleAsync(user, NUDES_ACCESS_ROLE_ID, "Nudes Access");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessMessageAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private int CalculateXpForMessage(SocketUserMessage message)
        {
            // Base XP
            var rand = new Random();
            int baseXp = rand.Next(10, 26);
            
            // Bonus for longer messages (with a reasonable cap)
            int lengthBonus = Math.Min(message.Content.Length / 50, 5);
            
            // Bonus for attachments/embeds
            int attachmentBonus = message.Attachments.Count > 0 ? 5 : 0;
            
            int totalXp = baseXp + lengthBonus + attachmentBonus;
            Console.WriteLine($"Calculated XP: base={baseXp}, lengthBonus={lengthBonus}, attachmentBonus={attachmentBonus}, total={totalXp}");
            
            return totalXp;
        }

        private int CalculateLevel(int xp)
        {
            // Basic formula: level = floor(sqrt(xp / 100))
            return (int)Math.Floor(Math.Sqrt(xp / 100.0));
        }

        private async Task SendLevelUpMessageAsync(SocketGuildUser user, int newLevel, ISocketMessageChannel sourceChannel)
        {
            try
            {
                // Use the source channel where the user was active
                var channel = sourceChannel ?? user.Guild.DefaultChannel;
                if (channel == null)
                {
                    Console.WriteLine("Could not find a valid channel for level up message");
                    return; // Safely handle null channel
                }
                
                Console.WriteLine($"Sending level up message to {channel.Name} for user {user.Username}");
                
                // Create a nice embed for level up
                var embed = new EmbedBuilder()
                    .WithTitle("Level Up!")
                    .WithDescription($"Congrats {user.Mention}, you're now level {newLevel}!")
                    .WithColor(Color.Gold)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithCurrentTimestamp();
                
                // Add unlocks info if applicable
                if (newLevel == SELFIE_ACCESS_LEVEL)
                {
                    embed.AddField("New Access Unlocked!", "You now have access to the selfie channel!");
                }
                else if (newLevel == NUDES_ACCESS_LEVEL)
                {
                    embed.AddField("New Access Unlocked!", "You now have access to the nudes channel!");
                }
                
                // Send the message and store the reference
                var message = await channel.SendMessageAsync(embed: embed.Build());
                Console.WriteLine("Level up message sent successfully");
                
                // Auto-delete after 30 seconds
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        if (message is IMessage deletableMessage)
                        {
                            await deletableMessage.DeleteAsync();
                            Console.WriteLine("Level up message auto-deleted after timeout");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error auto-deleting level up message: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending level up message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task AssignRoleAsync(SocketGuildUser user, ulong roleId, string roleName)
        {
            if (user.Roles.Any(r => r.Id == roleId))
            {
                Console.WriteLine($"User {user.Username} already has the {roleName} role");
                return;
            }

            var role = user.Guild.GetRole(roleId);
            if (role == null)
            {
                Console.WriteLine($"Role with ID {roleId} not found.");
                return;
            }

            try
            {
                Console.WriteLine($"Assigning {roleName} role to {user.Username}");
                await user.AddRoleAsync(role);
                Console.WriteLine($"Successfully assigned {roleName} role to {user.Username}");
                
                // Log role assignment
                await LogRoleAssignmentAsync(user, role.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not assign role: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private async Task LogXpAsync(SocketGuildUser user, int xpAmount, string reason, int totalXp, int level)
        {
            // Skip if no log channel
            if (_logChannel == null)
            {
                Console.WriteLine("Cannot log XP: _logChannel is null");
                return;
            }
            
            try
            {
                Console.WriteLine($"Creating embed for XP log: {user.Username} +{xpAmount}XP");
                
                // Create an embed for the XP notification
                var embed = new EmbedBuilder()
                    .WithTitle("XP Awarded")
                    .WithDescription($"{user.Username} received **{xpAmount}** XP")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .WithFooter(footer => footer.Text = $"User ID: {user.Id}")
                    .AddField("Current XP", totalXp, true)
                    .AddField("Level", level, true);
                
                // Add reason if provided
                if (!string.IsNullOrEmpty(reason))
                {
                    embed.AddField("Reason", reason);
                }
                
                Console.WriteLine($"Sending XP log to channel {_logChannel.Name}");
                await _logChannel.SendMessageAsync(embed: embed.Build());
                Console.WriteLine("XP log message sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging XP award: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private async Task LogRoleAssignmentAsync(SocketGuildUser user, string roleName)
        {
            // Skip if no log channel
            if (_logChannel == null)
            {
                Console.WriteLine("Cannot log role assignment: _logChannel is null");
                return;
            }
            
            try
            {
                Console.WriteLine($"Creating embed for role assignment log: {user.Username} received {roleName}");
                
                var embed = new EmbedBuilder()
                    .WithTitle("Role Assigned")
                    .WithDescription($"{user.Username} received the **{roleName}** role")
                    .WithColor(Color.Purple)
                    .WithCurrentTimestamp()
                    .WithFooter(footer => footer.Text = $"User ID: {user.Id}");
                
                Console.WriteLine($"Sending role assignment log to channel {_logChannel.Name}");
                await _logChannel.SendMessageAsync(embed: embed.Build());
                Console.WriteLine("Role assignment log message sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging role assignment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Method to manually award XP to a user (for admin commands)
        public async Task<bool> AwardXpAsync(SocketGuildUser user, int xpAmount, string reason)
        {
            if (user == null)
            {
                Console.WriteLine("Cannot award XP: user is null");
                return false;
            }
            
            try
            {
                Console.WriteLine($"Manually awarding {xpAmount} XP to {user.Username} for reason: {reason}");
                
                var userData = await _database.GetUserAsync(user.Id);
                if (userData == null)
                {
                    Console.WriteLine($"No existing user data for {user.Username}, creating new record");
                    userData = new UserData
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Discriminator = user.Discriminator,
                        XP = 0,
                        Level = 0,
                        LastMessageTime = DateTime.UtcNow.ToString("o")
                    };
                }
                
                int oldLevel = userData.Level;
                userData.XP += xpAmount;
                userData.Level = CalculateLevel(userData.XP);
                
                Console.WriteLine($"Saving updated user data: XP={userData.XP}, Level={userData.Level}");
                await _database.CreateOrUpdateUserAsync(userData);
                
                // Log the XP award
                await LogXpAsync(user, xpAmount, reason, userData.XP, userData.Level);
                
                // Handle level up if needed
                if (userData.Level > oldLevel)
                {
                    Console.WriteLine($"User {user.Username} leveled up to level {userData.Level}!");
                    // For manual awards, we don't have a source channel, so use default
                    await SendLevelUpMessageAsync(user, userData.Level, user.Guild.DefaultChannel);
                    
                    // Check for role assignments
                    if (userData.Level >= SELFIE_ACCESS_LEVEL)
                    {
                        await AssignRoleAsync(user, SELFIE_ACCESS_ROLE_ID, "Selfie Access");
                    }
                    
                    if (userData.Level >= NUDES_ACCESS_LEVEL)
                    {
                        await AssignRoleAsync(user, NUDES_ACCESS_ROLE_ID, "Nudes Access");
                    }
                    
                    return true; // User leveled up
                }
                
                return false; // No level up
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AwardXpAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}