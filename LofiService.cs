using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Faye.Services;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class LofiService
{
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokenSources = new ConcurrentDictionary<ulong, CancellationTokenSource>();
    private readonly DatabaseService _db;

    public LofiService(DiscordSocketClient client, DatabaseService db)
    {
        _client = client;
        _db = db;
        _client.Ready += RestoreLofiSessions;
    }

    // Attempt to restore lofi sessions on bot restart
    private async Task<Task> RestoreLofiSessions()
    {
        try
        {
            var activeLofiChannels = await _db.GetActiveLofiChannels();
            foreach (var channelData in activeLofiChannels)
            {
                var guild = _client.GetGuild(channelData.GuildId);
                if (guild != null)
                {
                    var channel = guild.GetVoiceChannel(channelData.ChannelId);
                    if (channel != null && channel.ConnectedUsers.Count > 0)
                    {
                        // Restart lofi in this channel
                        await JoinAndPlayLofi(channel);
                    }
                    else
                    {
                        // Remove stale entry if the channel is empty or no longer exists
                        await _db.RemoveActiveLofiChannel(channelData.GuildId, channelData.ChannelId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restoring lofi sessions: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    public async Task<IUserMessage> JoinChannel(IVoiceChannel channel, IMessageChannel textChannel)
    {
        if (channel == null)
            return await textChannel.SendMessageAsync("You need to be in a voice channel to use this command!");

        // Check if already connected to this channel
        if (_connectedChannels.TryGetValue(channel.GuildId, out var existingClient) && existingClient.ConnectionState == ConnectionState.Connected)
        {
            var existingChannel = await channel.Guild.GetVoiceChannelAsync(existingClient.ConnectionState.VoiceSessionId ?? 0);
            if (existingChannel?.Id == channel.Id)
                return await textChannel.SendMessageAsync("I'm already playing lofi in this channel!");
            
            // Disconnect from previous channel first
            await LeaveChannel(channel.GuildId);
        }

        var audioClient = await JoinAndPlayLofi(channel);
        
        if (audioClient != null)
        {
            await _db.AddActiveLofiChannel(channel.GuildId, channel.Id);
            return await textChannel.SendMessageAsync($"🎵 Now playing lofi in {channel.Name}! Enjoy the chill vibes~");
        }
        else
        {
            return await textChannel.SendMessageAsync("There was a problem connecting to the voice channel.");
        }
    }

    private async Task<IAudioClient> JoinAndPlayLofi(IVoiceChannel channel)
    {
        var audioClient = await channel.ConnectAsync();
        
        if (_connectedChannels.TryAdd(channel.GuildId, audioClient))
        {
            // Create a cancellation token for this session
            var disconnectToken = new CancellationTokenSource();
            _disconnectTokenSources.TryAdd(channel.GuildId, disconnectToken);
            
            // Start playing lofi in a background task
            _ = Task.Run(async () => 
            {
                try
                {
                    await SendLofiAudio(audioClient, disconnectToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error streaming lofi: {ex.Message}");
                    
                    // Try to reconnect if not deliberately stopped
                    if (!disconnectToken.IsCancellationRequested)
                    {
                        await Task.Delay(5000);
                        await JoinAndPlayLofi(channel);
                    }
                }
            });
            
            return audioClient;
        }
        
        return null;
    }

    private async Task SendLofiAudio(IAudioClient client, CancellationToken cancellationToken)
    {
        // Using FFmpeg to stream a lofi radio URL
        // This approach streams from a lofi YouTube live stream - you may need to update the URL periodically
        using (var ffmpeg = CreateLofiStream())
        using (var output = ffmpeg.StandardOutput.BaseStream)
        using (var discord = client.CreatePCMStream(AudioApplication.Music))
        {
            try
            {
                await output.CopyToAsync(discord, cancellationToken);
            }
            finally
            {
                await discord.FlushAsync();
            }
        }
    }

    private Process CreateLofiStream()
    {
        // You can use a specific YouTube livestream URL or other lofi source
        string lofiUrl = "https://www.youtube.com/watch?v=jfKfPfyJRdk"; // Example: Lofi Girl stream
        
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{lofiUrl}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
    }

    public async Task<IUserMessage> LeaveChannel(ulong guildId, IMessageChannel textChannel = null)
    {
        // Cancel any ongoing streaming
        if (_disconnectTokenSources.TryRemove(guildId, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
        
        // Disconnect from voice
        if (_connectedChannels.TryRemove(guildId, out var client))
        {
            await client.StopAsync();
            await _db.RemoveActiveLofiChannel(guildId);
            
            return textChannel != null 
                ? await textChannel.SendMessageAsync("Disconnected from voice channel. Lofi session ended.") 
                : null;
        }
        
        return textChannel != null 
            ? await textChannel.SendMessageAsync("I'm not currently in a voice channel.") 
            : null;
    }
    
    public async Task<IUserMessage> SetVolume(int volume, ulong guildId, IMessageChannel textChannel)
    {
        // Implement volume control logic here
        // This is a bit complex with FFmpeg streams and might require additional handling
        
        // For now, just acknowledge the command
        return await textChannel.SendMessageAsync($"Volume set to {volume}% (Note: Volume control is not yet implemented)");
    }
}