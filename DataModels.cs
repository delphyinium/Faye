using Discord;
using Discord.Interactions;

namespace Faye.Data
{
    public class UserData
    {
        public ulong UserId { get; set; }
        public string Username { get; set; } = "";
        public string Discriminator { get; set; } = "";
        public int XP { get; set; }
        public int Level { get; set; }
        public string LastMessageTime { get; set; } = "";
    }

    public class ProfileData
    {
        public string Bio { get; set; } = "";
        public string Interests { get; set; } = "";
        // These two fields remain in the model, but now they will contain the same combined value.
        public string Kinks { get; set; } = "";
        public string Limits { get; set; } = "";
        public int Age { get; set; }
        public string Gender { get; set; } = "";
    }

    // Unified modal definition with 5 fields (combining Kinks and Limits)
    public class ProfileModal : IModal
    {
        public string Title => "Update Your Profile";

        [InputLabel("Bio")]
        [ModalTextInput("bio", TextInputStyle.Paragraph, placeholder: "Tell us about yourself", maxLength: 1000)]
        public string? Bio { get; set; }

        [InputLabel("Age")]
        [ModalTextInput("age", TextInputStyle.Short, placeholder: "Your age", maxLength: 3)]
        public string? Age { get; set; }

        [InputLabel("Gender")]
        [ModalTextInput("gender", TextInputStyle.Short, placeholder: "Your gender", maxLength: 100)]
        public string? Gender { get; set; }

        [InputLabel("Interests")]
        [ModalTextInput("interests", TextInputStyle.Paragraph, placeholder: "Your interests", maxLength: 1000)]
        public string? Interests { get; set; }

        [InputLabel("Kinks & Limits")]
        [ModalTextInput("kinks", TextInputStyle.Paragraph, placeholder: "Your kinks and limits", maxLength: 1000)]
        public string? Kinks { get; set; }
    }

    public class TruthPrompt
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = "";
        public string AddedBy { get; set; } = "";
        public string AddedAt { get; set; } = "";
    }

    public class DarePrompt
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = "";
        public string AddedBy { get; set; } = "";
        public string AddedAt { get; set; } = "";
    }
}
