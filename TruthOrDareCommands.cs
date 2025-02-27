using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using Faye.Services;
using Faye.Data;

namespace Faye.Commands
{
    [Group("tod", "Truth or Dare game commands")]
    public class TruthOrDareCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseService _db;
        private readonly Random _random = new Random();

        public TruthOrDareCommands(DatabaseService db)
        {
            _db = db;
        }

        [SlashCommand("truth", "Get a random truth question")]
        public async Task GetTruth()
        {
            var truth = await _db.GetRandomTruthPromptAsync();
            if (truth == null)
            {
                await RespondAsync("No truth questions available yet. Add some with `/tod add truth`!", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🤔 Truth")
                .WithDescription(truth.Prompt)
                .WithColor(Color.Blue)
                .WithFooter($"Question ID: {truth.Id} • Added by: {truth.AddedBy}")
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("dare", "Get a random dare challenge")]
        public async Task GetDare()
        {
            var dare = await _db.GetRandomDarePromptAsync();
            if (dare == null)
            {
                await RespondAsync("No dare challenges available yet. Add some with `/tod add dare`!", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🔥 Dare")
                .WithDescription(dare.Prompt)
                .WithColor(Color.Red)
                .WithFooter($"Challenge ID: {dare.Id} • Added by: {dare.AddedBy}")
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("random", "Get a random truth or dare")]
        public async Task GetRandom()
        {
            // Decide randomly between truth and dare
            bool getTruth = _random.Next(2) == 0;
            
            if (getTruth)
            {
                await GetTruth();
            }
            else
            {
                await GetDare();
            }
        }

        [SlashCommand("add-truth", "Add a new truth question")]
        public async Task AddTruth([Summary("question", "The truth question to add")] string question)
        {
            int id = await _db.AddTruthPromptAsync(question, Context.User.Username);
            
            var embed = new EmbedBuilder()
                .WithTitle("Truth Question Added")
                .WithDescription($"Added: \"{question}\"")
                .WithColor(Color.Green)
                .WithFooter($"Question ID: {id}")
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("add-dare", "Add a new dare challenge")]
        public async Task AddDare([Summary("challenge", "The dare challenge to add")] string challenge)
        {
            int id = await _db.AddDarePromptAsync(challenge, Context.User.Username);
            
            var embed = new EmbedBuilder()
                .WithTitle("Dare Challenge Added")
                .WithDescription($"Added: \"{challenge}\"")
                .WithColor(Color.Green)
                .WithFooter($"Challenge ID: {id}")
                .Build();

            await RespondAsync(embed: embed);
        }

        [SlashCommand("remove-truth", "Remove a truth question by ID")]
        public async Task RemoveTruth([Summary("id", "The ID of the truth question to remove")] int id)
        {
            bool removed = await _db.RemoveTruthPromptAsync(id);
            
            if (removed)
            {
                await RespondAsync($"Truth question with ID {id} has been removed.", ephemeral: true);
            }
            else
            {
                await RespondAsync($"Could not find a truth question with ID {id}.", ephemeral: true);
            }
        }

        [SlashCommand("remove-dare", "Remove a dare challenge by ID")]
        public async Task RemoveDare([Summary("id", "The ID of the dare challenge to remove")] int id)
        {
            bool removed = await _db.RemoveDarePromptAsync(id);
            
            if (removed)
            {
                await RespondAsync($"Dare challenge with ID {id} has been removed.", ephemeral: true);
            }
            else
            {
                await RespondAsync($"Could not find a dare challenge with ID {id}.", ephemeral: true);
            }
        }
    }
}