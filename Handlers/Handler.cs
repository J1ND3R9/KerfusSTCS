using botForTRPO.GameClasses;
using botForTRPO.Models;
using botForTRPO.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace botForTRPO.Handlers
{
    public class Handler
    {
        public DiscordClient Client;
        public DiscordRestClient RestClient;
        public KerfusContext Kerfus = new();

        public int maxPages = DebugCommands.maxPages;
        public static int countForPage = DebugCommands.countForPage;
        public static int currentPage = 1;

        public Handler(DiscordClient client, DiscordRestClient restClient)
        {
            Client = client;
            RestClient = restClient;
        }

        public async Task nextPage(ComponentInteractionCreateEventArgs e)
        {
            var embed = e.Message.Embeds[0];
            var newEmbed = new DiscordEmbedBuilder(embed).ClearFields();
            newEmbed.AddField("Код", "Пакеты | Починено раз");

            if (e.Id.Contains("next"))
                currentPage++;
            else
                currentPage--;


            List<Satellite> satellites = Kerfus.Satellites.Where(s => s.ID > (currentPage - 1) * countForPage).ToList(); // Получаем формулу для подсчета количества нужных нам спутников

            for (int i = 0; i < satellites.Count; i++)
            {
                string code = satellites[i].CodeName;
                bool breakNow = satellites[i].IsBreak;
                string breakNowText = breakNow ? $"0/4 {DiscordEmoji.FromName(Client, ":red_circle:")}" : "4/4";
                long? repairs = satellites[i].Repairs;
                newEmbed.AddField($"{DiscordEmoji.FromName(Client, ":satellite:")} " + code.ToString(), $"{breakNowText} | {repairs}", true);
                if (i > countForPage - 2)
                    break;
            }

            var emptyEmoji = DiscordEmoji.FromGuildEmote(Client, 1228355062972153907);
            while ((newEmbed.Fields.Count - 1) % 3 != 0)
                newEmbed.AddField(emptyEmoji, emptyEmoji, true);
            newEmbed.WithFooter($"Страница {currentPage} из {maxPages}");

            var nextPage = new DiscordButtonComponent(ButtonStyle.Secondary, "nextPageStatSattelites", $"{DiscordEmoji.FromName(Client, ":arrow_right:")}");
            var pastPage = new DiscordButtonComponent(ButtonStyle.Secondary, "pastPageStatSattelites", $"{DiscordEmoji.FromName(Client, ":arrow_left:")}");

            if (currentPage == maxPages)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(newEmbed).AddComponents(pastPage));
                return;
            }
            if (currentPage == 1)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(newEmbed).AddComponents(nextPage));
                return;
            }
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(newEmbed).AddComponents(pastPage, nextPage));
        }
        #region Ремонт серверов игра
        private static Dictionary<ulong, GameClasses.ServerFixGame> fixServersDict = new();
        public async Task startFixServerGame(ComponentInteractionCreateEventArgs e)
        {
            var embed = e.Message.Embeds[0];
            var embedTitle = embed.Title;

            int index = embedTitle.IndexOf('[');
            var satelliteCodeName = embedTitle.Substring(index).Remove(0, 1);
            satelliteCodeName = satelliteCodeName.Remove(satelliteCodeName.Length - 1);

            Satellite satellite = Kerfus.Satellites.First(s => s.CodeName == satelliteCodeName);

            if (!satellite.IsBreak)
            {
                var whoopsieEmbed = new DiscordEmbedBuilder().WithTitle("Упс! Данный сервер уже починили").WithColor(DiscordColor.HotPink);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(whoopsieEmbed));
            }

            ServerFixGame gameClass = new(satellite);

            fixServersDict[e.User.Id] = gameClass;

            string mathfunc = gameClass.getMathFunc();

            var lockEmoji = DiscordEmoji.FromName(Client, ":abacus:");
            var newEmbed = new DiscordEmbedBuilder(embed).WithDescription($"{lockEmoji}").AddField(mathfunc, "ОТВЕТ: [null]");

            List<DiscordSelectComponentOption> selectComponents = new()
            {
                new DiscordSelectComponentOption("[0]", "[0]"),
                new DiscordSelectComponentOption("[1]", "[1]"),
                new DiscordSelectComponentOption("[2]", "[2]"),
                new DiscordSelectComponentOption("[3]", "[3]"),
                new DiscordSelectComponentOption("[4]", "[4]"),
                new DiscordSelectComponentOption("[5]", "[5]"),
                new DiscordSelectComponentOption("[6]", "[6]"),
                new DiscordSelectComponentOption("[7]", "[7]"),
                new DiscordSelectComponentOption("[8]", "[8]"),
                new DiscordSelectComponentOption("[9]", "[9]")
            };

            DiscordSelectComponent numericSelect = new("selectFixServers", "Выберите ответ", selectComponents);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(newEmbed).AddComponents(numericSelect));
        }

        public async Task serverFixGame(ComponentInteractionCreateEventArgs e)
        {

            ServerFixGame gameClass = fixServersDict[e.User.Id];
            Satellite satellite = gameClass.getSatellite();

            if (!satellite.IsBreak)
            {
                var whoopsieEmbed = new DiscordEmbedBuilder().WithTitle("Упс! Данный сервер уже починили").WithColor(DiscordColor.HotPink);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(whoopsieEmbed));
            }

            gameClass.taskReady++;
            var embed = e.Message.Embeds[0];
            var fieldCount = embed.Fields.Count;

            string mathFunc = embed.Fields[fieldCount - 1].Name;

            int userAnswer = Convert.ToInt32(e.Values[0].Substring(1, 1));

            gameClass.getAnswer(userAnswer, mathFunc);
            var newEmbed = new DiscordEmbedBuilder(embed);
            mathFunc = gameClass.getMathFunc();
            newEmbed.Fields[fieldCount - 1].Value = $"ОТВЕТ: [{userAnswer}]";
            
            if (gameClass.taskMaxCount == gameClass.taskReady)
            {
                DiscordEmbed notifyFixServerEmbed = null;
                if (gameClass.mistaken)
                {
                    newEmbed.WithTitle($"Сервер [{satellite.CodeName}] не отремонтирован").WithDescription("Вы ошиблись в одном из примеров").WithColor(DiscordColor.Red);
                }
                else
                {
                    satellite.IsBreak = false;
                    satellite.Repairs++;
                    Kerfus.Satellites.Update(satellite).DetectChanges();
                    await Kerfus.SaveChangesAsync();

                    newEmbed.WithTitle($"Вы починили сервер [{satellite.CodeName}]");
                    notifyFixServerEmbed = new DiscordEmbedBuilder().WithTitle($"Пользователь {e.User.Username} отремонтировал сервер {satellite.CodeName}!");
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(newEmbed).AsEphemeral());
                if (notifyFixServerEmbed != null)
                {
                    List<ChannelsForNotification> notificationsChannels = Kerfus.ChannelsForNotifications.ToList();

                    foreach (ChannelsForNotification channelID in notificationsChannels)
                        await Client.SendMessageAsync(await Client.GetChannelAsync((ulong)channelID.ChannelID), notifyFixServerEmbed);
                }
                return;
            }

            List<DiscordSelectComponentOption> selectComponents = new()
            {
                new DiscordSelectComponentOption("[0]", "[0]"),
                new DiscordSelectComponentOption("[1]", "[1]"),
                new DiscordSelectComponentOption("[2]", "[2]"),
                new DiscordSelectComponentOption("[3]", "[3]"),
                new DiscordSelectComponentOption("[4]", "[4]"),
                new DiscordSelectComponentOption("[5]", "[5]"),
                new DiscordSelectComponentOption("[6]", "[6]"),
                new DiscordSelectComponentOption("[7]", "[7]"),
                new DiscordSelectComponentOption("[8]", "[8]"),
                new DiscordSelectComponentOption("[9]", "[9]")
            };

            newEmbed.AddField(mathFunc, "ОТВЕТ: [null]");

            DiscordSelectComponent numericSelect = new("selectFixServers", "Выберите ответ", selectComponents);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(newEmbed).AddComponents(numericSelect));
        }
        #endregion
    }
}
