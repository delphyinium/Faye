using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;

namespace Faye.Commands
{
    public class BenchmarkCommand : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly Stopwatch _uptime;
        
        // Bot owner ID - replace with your user ID
        private readonly ulong _ownerId = 1014242032954396764; // Replace with your Discord ID

        public BenchmarkCommand(DiscordSocketClient client, IServiceProvider services)
        {
            _client = client;
            _services = services;
            _uptime = Stopwatch.StartNew();
        }

        [SlashCommand("benchmark", "Run detailed performance benchmarks (Owner only)")]
        public async Task BenchmarkAsync()
        {
            // Check if user is the owner
            if (Context.User.Id != _ownerId)
            {
                await RespondAsync("This command is restricted to the bot owner.", ephemeral: true);
                return;
            }

            // Respond to let the user know benchmarking has started
            await RespondAsync("Running benchmark tests... This may take a moment.", ephemeral: true);

            // Start collecting data
            var benchmarkReport = await CollectBenchmarkDataAsync();

            // Create the embed for the report
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Bot Benchmark Report")
                .WithDescription("Detailed performance metrics and diagnostics")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();

            // Add system information
            embedBuilder.AddField("System Info", benchmarkReport.SystemInfo, inline: false);
            
            // Add memory usage
            embedBuilder.AddField("Memory Usage", benchmarkReport.MemoryUsage, inline: false);
            
            // Add latency information
            embedBuilder.AddField("Network Latency", benchmarkReport.LatencyInfo, inline: false);
            
            // Add Discord connection info
            embedBuilder.AddField("Discord Stats", benchmarkReport.DiscordStats, inline: false);
            
            // Add performance metrics
            embedBuilder.AddField("Performance", benchmarkReport.PerformanceMetrics, inline: false);
            
            // Add database metrics if available
            if (!string.IsNullOrEmpty(benchmarkReport.DatabaseMetrics))
            {
                embedBuilder.AddField("Database", benchmarkReport.DatabaseMetrics, inline: false);
            }

            // Send the follow-up with the benchmark results
            await FollowupAsync(embed: embedBuilder.Build());
        }

        private async Task<BenchmarkReport> CollectBenchmarkDataAsync()
        {
            var report = new BenchmarkReport();
            
            // System Information
            report.SystemInfo = GetSystemInfo();
            
            // Memory Usage
            report.MemoryUsage = GetMemoryUsage();
            
            // Run database benchmark
            report.DatabaseMetrics = await MeasureDatabasePerformanceAsync();
            
            // Discord client stats
            report.DiscordStats = GetDiscordStats();
            
            // Latency tests
            report.LatencyInfo = await GetLatencyInfoAsync();
            
            // General performance metrics
            report.PerformanceMetrics = GetPerformanceMetrics();
            
            return report;
        }

        private string GetSystemInfo()
        {
            var sb = new StringBuilder();
            
            // OS info
            sb.AppendLine($"**OS**: {RuntimeInformation.OSDescription}");
            sb.AppendLine($"**Architecture**: {RuntimeInformation.OSArchitecture}");
            sb.AppendLine($"**Framework**: {RuntimeInformation.FrameworkDescription}");
            
            // Process info
            using (var process = Process.GetCurrentProcess())
            {
                sb.AppendLine($"**Process Uptime**: {_uptime.Elapsed:d\\.hh\\:mm\\:ss}");
                sb.AppendLine($"**Threads**: {process.Threads.Count}");
                sb.AppendLine($"**CPU Time**: {process.TotalProcessorTime:g}");
            }
            
            return sb.ToString();
        }

        private string GetMemoryUsage()
        {
            var sb = new StringBuilder();
            
            using (var process = Process.GetCurrentProcess())
            {
                // Convert bytes to MB for readability
                double workingSetMB = process.WorkingSet64 / 1024.0 / 1024.0;
                double privateMemoryMB = process.PrivateMemorySize64 / 1024.0 / 1024.0;
                double pagedMemoryMB = process.PagedMemorySize64 / 1024.0 / 1024.0;
                double peakWorkingSetMB = process.PeakWorkingSet64 / 1024.0 / 1024.0;
                
                sb.AppendLine($"**Working Set**: {workingSetMB:N2} MB");
                sb.AppendLine($"**Private Memory**: {privateMemoryMB:N2} MB");
                sb.AppendLine($"**Paged Memory**: {pagedMemoryMB:N2} MB");
                sb.AppendLine($"**Peak Working Set**: {peakWorkingSetMB:N2} MB");
                sb.AppendLine($"**GC Total Memory**: {GC.GetTotalMemory(false) / 1024.0 / 1024.0:N2} MB");
                sb.AppendLine($"**GC Collection Counts**: Gen 0: {GC.CollectionCount(0)}, Gen 1: {GC.CollectionCount(1)}, Gen 2: {GC.CollectionCount(2)}");
            }
            
            return sb.ToString();
        }

        private string GetDiscordStats()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"**Connected**: {_client.ConnectionState == ConnectionState.Connected}");
            sb.AppendLine($"**Latency**: {_client.Latency} ms");
            sb.AppendLine($"**Guilds**: {_client.Guilds.Count}");
            sb.AppendLine($"**Channels**: {_client.Guilds.Sum(g => g.Channels.Count)}");
            sb.AppendLine($"**Users**: {_client.Guilds.Sum(g => g.MemberCount)}");
            sb.AppendLine($"**ShardId**: {_client.ShardId}");
            
            return sb.ToString();
        }

        private async Task<string> GetLatencyInfoAsync()
        {
            var sb = new StringBuilder();
            
            // Discord API latency
            sb.AppendLine($"**Discord API**: {_client.Latency} ms");
            
            // Measure round-trip message time
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                var message = await Context.Channel.SendMessageAsync("Measuring latency...");
                stopwatch.Stop();
                
                sb.AppendLine($"**Message Round-Trip**: {stopwatch.ElapsedMilliseconds} ms");
                
                // Clean up the test message
                await message.DeleteAsync();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"**Message Test Failed**: {ex.Message}");
            }
            
            return sb.ToString();
        }

        private string GetPerformanceMetrics()
        {
            var sb = new StringBuilder();
            
            // CPU Usage
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    TimeSpan totalProcessorTime = process.TotalProcessorTime;
                    DateTime startTime = process.StartTime;
                    TimeSpan processUptime = DateTime.Now - startTime;
                    
                    double cpuUsagePercent = (totalProcessorTime.TotalMilliseconds / 
                                            (Environment.ProcessorCount * processUptime.TotalMilliseconds)) * 100;
                    
                    sb.AppendLine($"**CPU Usage**: {cpuUsagePercent:N2}%");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"**CPU Usage Calculation Failed**: {ex.Message}");
            }
            
            // Run garbage collection benchmark
            sb.AppendLine($"**GC Benchmark**: {RunGCBenchmark()} ms");
            
            // Command handling benchmark
            sb.AppendLine($"**Command Registration Count**: {GetRegisteredCommandCount()}");
            
            return sb.ToString();
        }

        private async Task<string> MeasureDatabasePerformanceAsync()
        {
            var sb = new StringBuilder();
            
            try
            {
                // Try to find the DatabaseService using reflection
                var databaseServiceType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "DatabaseService");
                
                if (databaseServiceType != null)
                {
                    // Find a method to test
                    var methods = databaseServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    
                    // Look for specific methods we can benchmark
                    var getTopUsersMethod = methods.FirstOrDefault(m => m.Name == "GetTopUsersAsync");
                    var getUserMethod = methods.FirstOrDefault(m => m.Name == "GetUserAsync");
                    
                    if (getTopUsersMethod != null || getUserMethod != null)
                    {
                        var databaseService = _services.GetService(databaseServiceType);
                        
                        if (databaseService != null)
                        {
                            var stopwatch = new Stopwatch();
                            
                            // Test GetTopUsersAsync if available
                            if (getTopUsersMethod != null)
                            {
                                stopwatch.Restart();
                                await (Task)getTopUsersMethod.Invoke(databaseService, new object[] { 10 });
                                stopwatch.Stop();
                                sb.AppendLine($"**GetTopUsers (10)**: {stopwatch.ElapsedMilliseconds} ms");
                            }
                            
                            // Test GetUserAsync if available
                            if (getUserMethod != null && Context.User != null)
                            {
                                stopwatch.Restart();
                                await (Task)getUserMethod.Invoke(databaseService, new object[] { Context.User.Id });
                                stopwatch.Stop();
                                sb.AppendLine($"**GetUser**: {stopwatch.ElapsedMilliseconds} ms");
                            }
                        }
                    }
                }
                
                if (sb.Length == 0)
                {
                    sb.AppendLine("Database benchmarking not available");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"**Database Benchmark Failed**: {ex.Message}");
            }
            
            return sb.ToString();
        }

        private long RunGCBenchmark()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Force garbage collection and measure how long it takes
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private int GetRegisteredCommandCount()
        {
            try
            {
                // Get the interaction service using reflection
                var interactionServiceType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "InteractionService");
                
                if (interactionServiceType != null)
                {
                    var interactionService = _services.GetService(interactionServiceType);
                    
                    if (interactionService != null)
                    {
                        // Get SlashCommands property
                        var slashCommandsProperty = interactionServiceType.GetProperty("SlashCommands");
                        
                        if (slashCommandsProperty != null)
                        {
                            var slashCommands = slashCommandsProperty.GetValue(interactionService) as IEnumerable<object>;
                            return slashCommands?.Count() ?? 0;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in reflection
            }
            
            return -1;
        }

        private class BenchmarkReport
        {
            public string SystemInfo { get; set; } = string.Empty;
            public string MemoryUsage { get; set; } = string.Empty;
            public string LatencyInfo { get; set; } = string.Empty;
            public string DiscordStats { get; set; } = string.Empty;
            public string PerformanceMetrics { get; set; } = string.Empty;
            public string DatabaseMetrics { get; set; } = string.Empty;
        }
    }
}