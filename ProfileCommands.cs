using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using Faye.Services;
using Faye.Data;

namespace Faye.Commands
{
    [Group("profile", "User profile commands")]
    public class ProfileCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseService _db;

        public ProfileCommands(DatabaseService db)
        {
            _db = db;
        }

        [SlashCommand("set", "Set your profile information")]
        public async Task SetProfile()
        {
            // Build modal with 5 text inputs (Discord modals allow up to 5 fields)
            var modal = new ModalBuilder()
                .WithTitle("Update Your Profile")
                .WithCustomId("profile_modal")
                .AddTextInput("Bio", "bio", TextInputStyle.Paragraph, "Tell us about yourself", required: false, maxLength: 1000)
                .AddTextInput("Age", "age", TextInputStyle.Short, "Your age", required: false, maxLength: 3)
                .AddTextInput("Gender", "gender", TextInputStyle.Short, "Your gender", required: false, maxLength: 100)
                .AddTextInput("Interests", "interests", TextInputStyle.Paragraph, "Your interests", required: false, maxLength: 1000)
                .AddTextInput("Kinks & Limits", "kinks", TextInputStyle.Paragraph, "Your kinks and limits", required: false, maxLength: 1000);

            await RespondWithModalAsync(modal.Build());
        }

        [SlashCommand("view", "View your or someone else's profile")]
        public async Task ViewProfile([Summary("user", "Whose profile to view")] IUser? user = null)
        {
            user ??= Context.User;

            var userData = await _db.GetUserAsync(user.Id);
            if (userData == null)
            {
                await RespondAsync("No user data found. Have they chatted yet?", ephemeral: true);
                return;
            }

            var profileData = await _db.GetUserProfileAsync(user.Id);
            var embed = new EmbedBuilder()
                .WithTitle($"{user.Username}'s Profile")
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithColor(Color.Blue)
                .AddField("Level", userData.Level, inline: true)
                .AddField("XP", userData.XP, inline: true);

            if (profileData != null)
            {
                if (!string.IsNullOrWhiteSpace(profileData.Bio))
                    embed.AddField("Bio", profileData.Bio);
                if (!string.IsNullOrWhiteSpace(profileData.Interests))
                    embed.AddField("Interests", profileData.Interests);
                if (!string.IsNullOrWhiteSpace(profileData.Kinks))
                    embed.AddField("Kinks & Limits", profileData.Kinks);
                if ((!string.IsNullOrWhiteSpace(profileData.Gender)) || (profileData.Age > 0))
                {
                    string info = "";
                    if (profileData.Age > 0)
                        info += $"Age: {profileData.Age} ";
                    if (!string.IsNullOrWhiteSpace(profileData.Gender))
                        info += $"Gender: {profileData.Gender}";
                    embed.AddField("Basic Info", info.Trim());
                }
            }

            embed.WithFooter($"Profile viewed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            await RespondAsync(embed: embed.Build());
        }
    }
}
