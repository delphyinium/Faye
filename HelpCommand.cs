using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Faye.Commands
{
    public class HelpCommand : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("help", "Learn what Faye can do for you")]
        public async Task Help()
        {
            await DeferAsync();

            // Create main welcome embed
            var welcomeEmbed = new EmbedBuilder()
                .WithTitle("👋 Hi there! I'm Faye")
                .WithDescription("I'm here to make your experience more fun and rewarding!")
                .WithColor(new Color(247, 168, 184)) // Soft pink color
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .Build();

            // Create leveling system embed
            var levelingEmbed = new EmbedBuilder()
                .WithTitle("⭐ Leveling System")
                .WithDescription("I track your activity and reward your participation!")
                .WithColor(new Color(252, 186, 3)) // Gold color
                .AddField("How to earn XP", 
                    "• Chat actively in the server\n" +
                    "• Post messages with more content\n" +
                    "• Share images and attachments\n" +
                    "• Be consistently active")
                .AddField("Special Rewards", 
                    "• **Level 1**: Access to selfie channels\n" +
                    "• **Level 3**: Access to exclusive content\n" +
                    "• XP is awarded once per minute\n" +
                    "• Level-up announcements are public")
                .Build();
            
            // Create commands embed based on actual commands
            var commandsEmbed = new EmbedBuilder()
                .WithTitle("🔧 Available Commands")
                .WithDescription("Here's what you can do with me:")
                .WithColor(new Color(77, 126, 255)) // Blue color
                .AddField("Leaderboard", 
                    "• `/leaderboard top` - Show the top users by XP")
                .AddField("Moderation",
                    "• `/mod ban` - Ban a user from the server\n" +
                    "• `/mod kick` - Kick a user from the server\n" +
                    "• `/mod missingrole` - Find users who don't have a specific role")
                .AddField("Profile",
                    "• `/profile set` - Set your profile information\n" +
                    "• `/profile view` - View your or someone else's profile")
                .AddField("Truth or Dare",
                    "• `/tod dare` - Get a random dare challenge\n" +
                    "• `/tod truth` - Get a random truth question\n" +
                    "• `/tod add-dare` - Add a new dare challenge\n" +
                    "• `/tod add-truth` - Add a new truth question\n" +
                    "• `/tod remove-dare` - Remove a dare challenge by ID\n" +
                    "• `/tod remove-truth` - Remove a truth question by ID")
                .AddField("System",
                    "• `/ping` - Check bot response time")
                .Build();
            
            // Send all embeds as one message
            await FollowupAsync(embeds: new[] { welcomeEmbed, levelingEmbed, commandsEmbed });
        }
    }
}