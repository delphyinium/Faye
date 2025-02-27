using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace Faye.Commands
{
    [Group("mod", "Moderation commands")]
    public partial class ModCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ban", "Ban a user from the server")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BanUser(
            [Summary("user", "The user to ban")] SocketGuildUser user,
            [Summary("reason", "Reason for the ban")] string reason = "No reason provided",
            [Summary("days", "Number of days of messages to delete")] int days = 0)
        {
            // No need to resolve – the parameter is already a guild member.
            await Context.Guild.AddBanAsync(user.Id, days, reason);

            var embed = new EmbedBuilder()
                .WithTitle("User Banned")
                .WithDescription($"{user.Mention} has been banned.")
                .AddField("Reason", reason)
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("kick", "Kick a user from the server")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task KickUser(
            [Summary("user", "The user to kick")] SocketGuildUser user,
            [Summary("reason", "Reason for the kick")] string reason = "No reason provided")
        {
            await user.KickAsync(reason);

            var embed = new EmbedBuilder()
                .WithTitle("User Kicked")
                .WithDescription($"{user.Mention} has been kicked.")
                .AddField("Reason", reason)
                .WithColor(Color.Orange)
                .WithCurrentTimestamp()
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .Build();

            await RespondAsync(embed: embed);
        }
        
        [SlashCommand("missingrole", "Find users who don't have a specific role")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task MissingRole([Summary("rolename", "Name of the role to check")] string roleName)
        {
            // Defer the response since this might take some time
            await DeferAsync();
            
            // Get the role by name
            var role = Context.Guild.Roles.FirstOrDefault(r => 
                r.Name.ToLower() == roleName.ToLower());
            
            if (role == null)
            {
                await FollowupAsync($"Role '{roleName}' not found. Please check the spelling.", ephemeral: true);
                return;
            }
            
            // Get all members who don't have this role
            var usersWithoutRole = Context.Guild.Users
                .Where(u => !u.IsBot && !u.Roles.Any(r => r.Id == role.Id))
                .ToList();
            
            if (usersWithoutRole.Count == 0)
            {
                await FollowupAsync($"All users have the role '{role.Name}'!", ephemeral: true);
                return;
            }
            
            // Build response
            var embed = new EmbedBuilder()
                .WithTitle($"Users Missing Role: {role.Name}")
                .WithColor(Color.Orange)
                .WithFooter($"Total users without role: {usersWithoutRole.Count}");
            
            var description = new StringBuilder();
            
            // Add users to the description, up to 25 max to avoid hitting Discord limits
            var displayedUsers = usersWithoutRole.Take(25).ToList();
            foreach (var user in displayedUsers)
            {
                description.AppendLine($"• {user.Mention} ({user.Username})");
            }
            
            if (usersWithoutRole.Count > 25)
            {
                description.AppendLine($"*...and {usersWithoutRole.Count - 25} more users*");
            }
            
            embed.WithDescription(description.ToString());
            
            await FollowupAsync(embed: embed.Build());
        }
    }
}