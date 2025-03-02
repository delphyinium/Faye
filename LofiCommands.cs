using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

// You can rename this class to match your naming convention if needed
public class LofiCommands : ModuleBase<SocketCommandContext>
{
    private readonly LofiService _lofiService;
    
    public LofiCommands(LofiService lofiService)
    {
        _lofiService = lofiService;
    }
    
    [Command("lofi")]
    [Summary("Controls lofi music playback. Use 'join', 'leave', or 'volume'")]
    public async Task Lofi([Remainder] string command = "")
    {
        // Default behavior - print help if no arguments
        if (string.IsNullOrWhiteSpace(command))
        {
            await ReplyAsync(
                "🎵 **Lofi Commands**\n" +
                "`!lofi join` - Start playing lofi in your voice channel\n" +
                "`!lofi leave` - Stop playing lofi and disconnect\n" +
                "`!lofi volume [1-100]` - Adjust the volume (WIP)\n" +
                "`!lofi status` - Check if lofi is currently playing");
            return;
        }
        
        var args = command.Split(' ');
        var subCommand = args[0].ToLower();
        
        switch (subCommand)
        {
            case "join":
                var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                await _lofiService.JoinChannel(voiceChannel, Context.Channel);
                break;
                
            case "leave":
                await _lofiService.LeaveChannel(Context.Guild.Id, Context.Channel);
                break;
                
            case "volume":
                if (args.Length > 1 && int.TryParse(args[1], out int volume) && volume >= 0 && volume <= 100)
                {
                    await _lofiService.SetVolume(volume, Context.Guild.Id, Context.Channel);
                }
                else
                {
                    await ReplyAsync("Please specify a volume level between 0 and 100.");
                }
                break;
                
            case "status":
                // This is a placeholder - implement status checking in LofiService
                await ReplyAsync("Status command not yet implemented. Check if I'm in a voice channel!");
                break;
                
            default:
                await ReplyAsync($"Unknown lofi command: '{subCommand}'. Try `!lofi` for a list of valid commands.");
                break;
        }
    }
}