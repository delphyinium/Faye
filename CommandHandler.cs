using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;

namespace Faye.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;

        public CommandHandler(
            DiscordSocketClient client,
            InteractionService interactionService,
            IServiceProvider services)
        {
            _client = client;
            _interactionService = interactionService;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            // Subscribe to events
            _client.Ready += OnReady;
            _client.InteractionCreated += OnInteractionCreated;

            // Add modules with slash commands, etc.
            await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

            // Log all commands that were loaded
            foreach (var command in _interactionService.SlashCommands)
            {
                Console.WriteLine($"Loaded slash command: {command.Name}");
            }
        }

        private Task OnReady()
        {
            // Start a background task that won't block the gateway
            _ = Task.Run(async () =>
            {
                try
                {
                    // Delete existing global commands
                    Console.WriteLine("Deleting existing global commands...");
                    var existingCommands = await _client.GetGlobalApplicationCommandsAsync();
                    foreach (var command in existingCommands)
                    {
                        await command.DeleteAsync();
                        Console.WriteLine($"Deleted command: {command.Name}");
                    }
                    Console.WriteLine("Existing global commands deleted successfully.");

                    // Then register globally
                    await _interactionService.RegisterCommandsGloballyAsync(true);
                    Console.WriteLine("Slash commands registered globally. Note: This may take up to an hour to update on Discord.");
                    Console.WriteLine("Bot is ready!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error registering commands: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            });

            return Task.CompletedTask;
        }

        private async Task OnInteractionCreated(SocketInteraction interaction)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                var result = await _interactionService.ExecuteCommandAsync(ctx, _services);

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"Interaction error: {result.Error} - {result.ErrorReason}");

                    // Only send error messages for errors that should be reported to users
                    if (result.Error != InteractionCommandError.UnknownCommand)
                    {
                        if (!interaction.HasResponded)
                        {
                            await interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Interaction exception: {ex}");
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    if (!interaction.HasResponded)
                    {
                        await interaction.RespondAsync("Oops, something went wrong!", ephemeral: true);
                    }
                }
            }
        }
    }
}