using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using Faye.Data;
using Faye.Services;

namespace Faye.Commands
{
    public class ProfileModalHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseService _db;

        public ProfileModalHandler(DatabaseService db)
        {
            _db = db;
        }

        [ModalInteraction("profile_modal")]
        public async Task HandleProfileModal(ProfileModal modal)
        {
            // Retrieve or create user data in the database
            var userData = await _db.GetUserAsync(Context.User.Id);
            if (userData == null)
            {
                userData = new UserData
                {
                    UserId = Context.User.Id,
                    Username = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                    XP = 0,
                    Level = 0,
                    LastMessageTime = DateTime.UtcNow.ToString("o")
                };
                await _db.CreateOrUpdateUserAsync(userData);
            }

            // Combine the kinks and limits input into one value.
            var combinedKinks = modal.Kinks ?? "";

            var profileData = new ProfileData
            {
                Bio = modal.Bio ?? "",
                Interests = modal.Interests ?? "",
                Kinks = combinedKinks,
                Limits = combinedKinks, // Both fields store the same combined value.
                Gender = modal.Gender ?? "",
                Age = int.TryParse(modal.Age, out int age) ? age : 0
            };

            await _db.UpdateUserProfileAsync(Context.User.Id, profileData);
            await RespondAsync("Profile updated successfully!", ephemeral: true);
        }
    }
}
