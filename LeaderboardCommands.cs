using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Faye.Services;

namespace Faye.Commands
{
    [Group("leaderboard", "Check who has the highest XP/Levels")]
    public class LeaderboardCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseService _db;

        public LeaderboardCommands(DatabaseService db)
        {
            _db = db;
        }

        [SlashCommand("top", "Show the top users by XP")]
        public async Task ShowTop(int limit = 10)
        {
            // Fetch top users
            var topUsers = await _db.GetTopUsersAsync(limit);

            // If no users, respond ephemeral
            if (topUsers.Count == 0)
            {
                await RespondAsync("No users found in the database.", ephemeral: true);
                return;
            }

            // Build output
            var sb = new StringBuilder();
            int rank = 1;

            foreach (var user in topUsers)
            {
                sb.AppendLine($"{rank}. **{user.Username}#{user.Discriminator}** " +
                              $"- Level: {user.Level}, XP: {user.XP}");
                rank++;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Top {topUsers.Count} Users by XP")
                .WithDescription(sb.ToString())
                .WithColor(Color.Gold)
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embed: embed);
        }
    }
}
