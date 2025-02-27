using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Faye.Services;
using DotNetEnv; // For .env loading

namespace Faye
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // 1) Load environment variables from .env
            Env.Load();

            // 2) Retrieve the token
            string? token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Error: DISCORD_TOKEN not set!");
                return;
            }

            // 3) Build service provider
            var services = CreateServices();

            // 4) Start the bot
            await RunBotAsync(services, token);
        }

        private static ServiceProvider CreateServices()
        {
            return new ServiceCollection()
                // Discord client with gateway intents
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged
                                     | GatewayIntents.GuildMembers
                                     | GatewayIntents.MessageContent,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 100
                }))
                // Interaction service for slash commands & modals
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                // Your custom services
                .AddSingleton<CommandHandler>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<LevelingService>()
                .BuildServiceProvider();
        }

        private static async Task RunBotAsync(ServiceProvider services, string token)
        {
            var client = services.GetRequiredService<DiscordSocketClient>();
            var interactionService = services.GetRequiredService<InteractionService>();

            // Logging
            client.Log += LogAsync;
            interactionService.Log += LogAsync;

            // Login & connect
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Initialize your command handling & DB
            await services.GetRequiredService<CommandHandler>().InitializeAsync();
            await services.GetRequiredService<DatabaseService>().InitializeAsync();
            
            // Get the LevelingService instance to initialize it
            // This triggers the constructor which subscribes to the events
            Console.WriteLine("Initializing LevelingService...");
            var levelingService = services.GetRequiredService<LevelingService>();
            Console.WriteLine("LevelingService initialized");

            // Keep the application running
            await Task.Delay(-1);
        }

        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}