using System;

namespace Faye.Data
{
    public class UserProfile
    {
        public ulong UserId { get; set; }
        public string Bio { get; set; } = "";
        public int Age { get; set; }
        public string Gender { get; set; } = "";
        public string Interests { get; set; } = "";
        public string Kinks { get; set; } = "";
        public string Limits { get; set; } = "";
    }
}