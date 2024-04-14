using botForTRPO.Models;
using botForTRPO.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Timers;

namespace botForTRPO
{
    public class Program
    {
        public static KerfusContext Kerfus = new();
        private static DiscordClient Client { get; set; }
        private static DiscordRestClient RestClient { get; set; }
        private static System.Timers.Timer timer = new();
        private static Random r = new();
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
            Client.ComponentInteractionCreated += Client_IntercationHandler;

            SlashCommandsExtension slashConfig = Client.UseSlashCommands();

            slashConfig.RegisterCommands<Null>(); // Метод slashConfig.RefreshCommand() не был использован, т.к. он очень медленный.
            slashConfig.RegisterCommands<DebugCommands>(1228018468516003851);

            slashConfig.SlashCommandErrored += SlashCommandError;
            slashConfig.SlashCommandExecuted += SlashCommandSuccessfullyExecuted;

            timer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            timer.Elapsed += timerServers;
            timer.Start();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static decimal chanceBreak = 5;
        private static async void timerServers(object? sender, ElapsedEventArgs e)
        {
            if (chanceBreak > r.Next(0, 101))
                await Game_RandomBreakServer();
            else
                chanceBreak *= (decimal)1.2;
        }

        private static async Task Game_RandomBreakServer() // Поломка сервера
        {
            List<Satellite> allServers = Kerfus.Satellites.ToList();
            Satellite server = Kerfus.Satellites.First(s => s.ID == r.Next(1, allServers.Count + 1));
            while (server.IsBreak)
                server = Kerfus.Satellites.First(s => s.ID == r.Next(1, allServers.Count + 1));
            server.IsBreak = true;
        }

        private static async Task Game_ServerIsDownNotify(Satellite server) // Уведомление по поломке сервера
        {
            long? channelID = Kerfus.AllowedChannels.Where(s => s.RuleFor == "Серверам").Select(s => s.ChannelID).FirstOrDefault();
            var alarmEmoji = DiscordEmoji.FromName(Client, ":red_circle:");
            string title = $"{alarmEmoji} [{server.CodeName}] перестал отвечать на запросы";
            if (r.Next(0, 2) == 1)
                title = $"{alarmEmoji} Мяу!! [{server.CodeName}] перестал отвечать на запросы!!!";
            var embed = new DiscordEmbedBuilder().WithTitle(title);

            DiscordGuild guild = await Client.GetGuildAsync(1228018468516003851);
            DiscordChannel channel;
            if (channelID == null)
            {
                embed.WithDescription("Вы не настроили на какой сервер я должна отправлять сообщения о поломке серверов, поэтому я создала его сама, мяу!");
                channel = await guild.CreateTextChannelAsync("уведомления-сервера");
            }
            else
                channel = guild.GetChannel((ulong)channelID);

            await channel.SendMessageAsync(embed);
        }

        #region Взаимодействие с интерактивностями
        private static async Task Client_IntercationHandler(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            Handlers.Handler handler = new(Client, RestClient);

            var user = e.Interaction.User;
            ulong messageID = e.Message.Id;
            switch (e.Id)
            {
                case "nextPageStatSattelites":
                case "pastPageStatSattelites":
                    await handler.nextPage(e);
                    return;
            }
        }
        #endregion

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
