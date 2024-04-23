using botForTRPO.Models;
using botForTRPO.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Timers;

namespace botForTRPO
{
    public class Program
    {
        public static KerfusContext Kerfus = new KerfusContext();
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
            Console.ForegroundColor = ConsoleColor.Yellow;  
            List<Satellite> allServers = Kerfus.Satellites.Where(s => !s.IsBreak).ToList();
            if (!allServers.Any())
            {
                await Console.Out.WriteLineAsync("[Все сервера уже сломаны!]");
                Console.ResetColor();
                return;
            }
            
            await Console.Out.WriteLineAsync("[Ломаем сервер...]");
            if (chanceBreak >= r.Next(1, 101))
                await Game_RandomBreakServer();
            else
            {
                chanceBreak *= (decimal)1.2;
                await Console.Out.WriteLineAsync($"[Сервер не был сломан | Текущий шанс: {Math.Round(chanceBreak, 1)}%]");
            }
            Console.ResetColor();
        }

        private static async Task Game_RandomBreakServer() // Поломка сервера
        {
            List<Satellite> allServers = Kerfus.Satellites.ToList();

            long randomServer = r.Next(1, allServers.Count + 1);
            Satellite server = Kerfus.Satellites.First(s => s.ID == randomServer);
            while (server.IsBreak)
                server = Kerfus.Satellites.First(s => s.ID == r.Next(1, allServers.Count + 1));

            server.IsBreak = true;
            

            chanceBreak /= (decimal)3;

            await Console.Out.WriteLineAsync($"[Сервер {server.CodeName} поломан! | Текущий шанс: {Math.Round(chanceBreak, 1)}%]");
            Console.ResetColor();
            if (chanceBreak > 65 && r.Next(1, 3) == 1)
            {
                List<Satellite> serverCheckBreaking = Kerfus.Satellites.Where(s => !s.IsBreak).ToList();
                if (!serverCheckBreaking.Any())
                {
                    await Console.Out.WriteLineAsync("[Все сервера уже сломаны!]");
                    Console.ResetColor();
                    return;
                }
                randomServer = r.Next(1, allServers.Count + 1);
                Satellite server1 = Kerfus.Satellites.First(s => s.ID == randomServer);
                while (server1.IsBreak)
                    server1 = Kerfus.Satellites.First(s => s.ID == r.Next(1, allServers.Count + 1));

                server1.IsBreak = true;
                Kerfus.Update(server1);
                await Game_ServerIsDownNotify(server1);
            }
            Kerfus.Update(server);
            Kerfus.SaveChanges();
            await Game_ServerIsDownNotify(server);
        }

        public static async Task Game_ServerIsDownNotify(Satellite server) // Уведомление по поломке сервера
        {
            var alarmEmoji = DiscordEmoji.FromName(Client, ":red_circle:");
            string title = $"{alarmEmoji} [{server.CodeName}] перестал отвечать на запросы";
            int random = r.Next(0, 10);

            if (random > 5)
                title = $"{alarmEmoji} Мяу!! [{server.CodeName}] перестал отвечать на запросы!!!";
            else if (random > 7)
                title = $"{alarmEmoji} Мяу! Мяу! Мяу! Нам нужно починить сервер [{server.CodeName}]!";
            else
                title = $"Миау! {alarmEmoji} Сервер [{server.CodeName}] нужно починить!";

            var embed = new DiscordEmbedBuilder().WithTitle(title);

            List<ChannelsForNotification> notifications = Kerfus.ChannelsForNotifications.ToList();

            foreach (ChannelsForNotification notification in notifications)
                await Client.SendMessageAsync(await Client.GetChannelAsync((ulong)notification.ChannelID), embed);
        }

        #region Взаимодействие с интерактивностями
        private static async Task Client_IntercationHandler(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            Handlers.Handler handler = new(Client, RestClient);

            var user = e.Interaction.User;
            ulong messageID = e.Message.Id;
            switch (e.Id)
            {
                case string x when (e.Id.EndsWith("PageStatSattelites")):
                    await handler.nextPage(e);
                    return;
                case "beginServerFix":
                    await handler.startFixServerGame(e);
                    return;
                case "selectFixServers":
                    await handler.serverFixGame(e);
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
