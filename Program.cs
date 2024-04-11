using botForTRPO.SlashCommands;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace botForTRPO
{
    public class Program
    {
        private static DiscordClient Client { get; set; }
        private static DiscordRestClient RestClient { get; set; }
        static async Task Main(string[] args)
        {
            DiscordConfiguration discordConfig = new()
            {
                Intents = DiscordIntents.All,
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);
            RestClient = new DiscordRestClient(discordConfig);

            Client.Ready += Client_Ready;

            SlashCommandsExtension slashConfig = Client.UseSlashCommands();

            slashConfig.RegisterCommands<Null>(); // Метод slashConfig.RefreshCommand() не был использован, т.к. он очень медленный.
            slashConfig.RegisterCommands<DebugCommands>(1228018468516003851);

            slashConfig.SlashCommandErrored += SlashCommandError;
            slashConfig.SlashCommandExecuted += SlashCommandSuccessfullyExecuted;

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        #region Дебаг слэш команд
        private static Task SlashCommandSuccessfullyExecuted(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandExecutedEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[/{args.Context.CommandName} | {args.Context.Member.Username}] ");
            Console.ResetColor();
            Console.WriteLine("команда выполнена успешно!");
            return Task.CompletedTask;
        }
        private static Task SlashCommandError(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandErrorEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"[{args.Context.CommandName} | {args.Context.Member.Username}] ");
            Console.ResetColor();
            Console.WriteLine($"произошла ошибка при выполнении команды: {args.Exception}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("------------------------------------");
            Console.ResetColor();
            Console.WriteLine(args.Exception.Message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("------------------------------------");
            Console.ResetColor();
            return Task.CompletedTask;
        }
        #endregion

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args) => Task.CompletedTask;
    }
}
