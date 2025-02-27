using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Faye.Commands
{
    public class OwnerCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [RequireOwner]
        [SlashCommand("status", "Displays detailed bot and system statistics.")]
        public async Task Status()
        {
            await DeferAsync(ephemeral: true);
            
            // Process & system information
            Process process = Process.GetCurrentProcess();
            TimeSpan processUptime = DateTime.Now - process.StartTime;
            double memoryUsageMb = process.WorkingSet64 / (1024.0 * 1024.0);
            int threadCount = process.Threads.Count;
            TimeSpan systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

            // Discord client info
            var client = Context.Client as DiscordSocketClient;
            int latency = client.Latency;
            int guildCount = client.Guilds.Count;
            int totalTextChannels = 0;
            int totalVoiceChannels = 0;
            int totalCategories = 0;
            int totalForumChannels = 0;
            int userCount = 0;
            int botCount = 0;
            int totalRoles = 0;
            int totalEmojis = 0;
            int totalCommands = client.Rest.GetGlobalApplicationCommands().Result.Count;
            
            foreach (var guild in client.Guilds)
            {
                totalTextChannels += guild.TextChannels.Count;
                totalVoiceChannels += guild.VoiceChannels.Count;
                totalCategories += guild.CategoryChannels.Count;
                totalForumChannels += guild.ForumChannels.Count;
                userCount += guild.Users.Count(u => !u.IsBot);
                botCount += guild.Users.Count(u => u.IsBot);
                totalRoles += guild.Roles.Count;
                totalEmojis += guild.Emotes.Count;
            }

            // .NET and OS info
            string runtimeVersion = RuntimeInformation.FrameworkDescription;
            string osDescription = RuntimeInformation.OSDescription;
            string architecture = RuntimeInformation.ProcessArchitecture.ToString();

            // Garbage Collector metrics
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);
            long memoryAllocated = GC.GetTotalMemory(false) / (1024 * 1024);

            // Connection info - simplify sharding info
            string shardInfo = "No Sharding";
            
            // Bot version (you may want to update this to your actual version tracking)
            string botVersion = "v1.0.0";  // Replace with your version tracking

            // Build the embed with all details
            var embed = new EmbedBuilder()
                .WithTitle("📊 Faye Status Dashboard")
                .WithDescription($"**{Context.Client.CurrentUser.Username}** has been online for {FormatTimeSpan(processUptime)}")
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithColor(new Color(114, 137, 218))  // Discord blurple color
                .WithFooter(footer => {
                    footer.Text = $"Requested by {Context.User.Username} • {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                    footer.IconUrl = Context.User.GetAvatarUrl();
                })
                .Build();

            // Build multiple embeds for better organization
            var botEmbed = new EmbedBuilder()
                .WithTitle("🤖 Bot Information")
                .AddField("⏱️ Uptime", FormatTimeSpan(processUptime), true)
                .AddField("🏓 Latency", $"{latency} ms", true)
                .AddField("🔄 Shard", shardInfo, true)
                .AddField("📟 Version", botVersion, true)
                .AddField("📊 Commands", totalCommands, true)
                .AddField("📕 Library", "Discord.Net", true)
                .WithColor(new Color(114, 137, 218))
                .Build();

            var serverEmbed = new EmbedBuilder()
                .WithTitle("🖥️ Server Statistics")
                .AddField("🖧 Servers", guildCount, true)
                .AddField("👥 Users", userCount, true)
                .AddField("🤖 Bots", botCount, true)
                .AddField("💬 Text Channels", totalTextChannels, true)
                .AddField("🔊 Voice Channels", totalVoiceChannels, true)
                .AddField("📂 Categories", totalCategories, true)
                .AddField("📝 Forums", totalForumChannels, true)
                .AddField("🔰 Roles", totalRoles, true)
                .AddField("😀 Emojis", totalEmojis, true)
                .WithColor(new Color(67, 181, 129))  // Green color
                .Build();

            var systemEmbed = new EmbedBuilder()
                .WithTitle("⚙️ System Information")
                .AddField("💻 OS", osDescription, true)
                .AddField("🧠 Architecture", architecture, true)
                .AddField("📚 Runtime", runtimeVersion, true)
                .AddField("📈 CPU Usage", $"N/A", true)
                .AddField("💾 Memory", $"{memoryUsageMb:F2} MB", true)
                .AddField("⚡ GC Memory", $"{memoryAllocated} MB", true)
                .AddField("🧵 Threads", threadCount, true)
                .AddField("♻️ GC (0/1/2)", $"{gen0}/{gen1}/{gen2}", true)
                .AddField("⏰ System Uptime", FormatTimeSpan(systemUptime), true)
                .WithColor(new Color(250, 166, 26))  // Orange color
                .Build();

            await FollowupAsync(embeds: new[] { botEmbed, serverEmbed, systemEmbed }, ephemeral: true);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            else
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }

        // Removed GetCpuUsage method that used PerformanceCounter
    }
}