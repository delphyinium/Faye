using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Faye.Commands
{
    public class PingCommand : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ping", "Check the bot's response time")]
        public async Task Ping()
        {
            // Get the client latency
            var client = Context.Client as DiscordSocketClient;
            int latency = client.Latency;
            
            // Create timestamp to calculate round-trip time
            var timestamp = DateTimeOffset.UtcNow;
            var message = await ReplyAsync("Calculating ping...");
            
            // Calculate the round-trip time
            var roundTripTime = (DateTimeOffset.UtcNow - timestamp).TotalMilliseconds;
            
            // Create a nice embed for the response
            var embed = new EmbedBuilder()
                .WithTitle("🏓 Pong!")
                .WithDescription("Here's my current response time:")
                .WithColor(latency < 100 ? Color.Green : (latency < 200 ? Color.Gold : Color.Red))
                .AddField("API Latency", $"{latency}ms", true)
                .AddField("Round-trip Time", $"{roundTripTime:F0}ms", true)
                .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                .WithCurrentTimestamp()
                .Build();
            
            // Edit the original message with the embed
            await message.ModifyAsync(msg => {
                msg.Content = string.Empty;
                msg.Embed = embed;
            });
        }
    }
}