using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Faye.Services
{
    public class WelcomeService
    {
        private readonly DiscordSocketClient _client;

        public WelcomeService(DiscordSocketClient client)
        {
            _client = client;
            _client.UserJoined += UserJoinedAsync;
            
            Console.WriteLine("WelcomeService initialized");
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            try
            {
                Console.WriteLine($"User joined: {user.Username} (ID: {user.Id}) in guild {user.Guild.Name}");
                
                // Create a welcome embed
                var embed = new EmbedBuilder()
                    .WithTitle("Welcome to Amy's Hangout!")
                    .WithDescription("We're excited to have you join our community!")
                    .WithColor(Color.Purple)
                    .WithThumbnailUrl(user.Guild.IconUrl)
                    .WithCurrentTimestamp()
                    .AddField("Getting Started", "Please go to <#read-me> and check the reaction to get access to the server.")
                    .AddField("Set Up Your Profile", "Use the `/profile set` command to customize your profile!")
                    .AddField("Need Help?", "If you have any questions, feel free to ask in the general chat channels.")
                    .WithFooter(footer => footer.Text = $"Welcome, {user.Username}!");
                
                // DM the user
                try
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync("Haiii!!! Welcome to **Amy's Hangout**! 💜", embed: embed.Build());
                    Console.WriteLine($"Welcome message sent to {user.Username}");
                }
                catch (Exception ex)
                {
                    // Handle cases where DMs might be blocked
                    Console.WriteLine($"Failed to send welcome DM to {user.Username}: {ex.Message}");
                    
                    // Optionally, send a message in a welcome channel instead
                    // var welcomeChannel = user.Guild.GetTextChannel(YOUR_WELCOME_CHANNEL_ID);
                    // if (welcomeChannel != null)
                    // {
                    //     await welcomeChannel.SendMessageAsync($"Welcome {user.Mention}! I tried to DM you but couldn't. Please enable DMs for this server.");
                    // }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UserJoinedAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}