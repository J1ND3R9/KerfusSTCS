using botForTRPO.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace botForTRPO.SlashCommands
{
    class DebugCommands : ApplicationCommandModule
    {

        public static int maxPages = 2;
        public static int countForPage = 21;
        public static KerfusContext Kerfus = new();

        [SlashCommand("пинг", "Показывает задержку соединения с ботом")]
        private async Task Ping(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Мяу! Моя задержка: {ctx.Client.Ping}мс")
                .WithColor(DiscordColor.HotPink);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("статистика", "Показывает статистику")]
        private async Task DebugUser(InteractionContext ctx,
            [Choice("Пойманные сигналы", 0)]
            [Choice("Сервера", 1)]
            [Option("по", "Статистику по каким данным отобразить")] long value)
        {
            switch (value)
            {
                case 0:
                    return;
                case 1:
                    await StatsSatellites(ctx);
                    return;
            }

        }

        public class ServersData : IAutocompleteProvider
        {
            public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
            {
                var satellite = ctx.FocusedOption.Value;
                List<DiscordAutoCompleteChoice> choices = new();

                Satellite? haveBreak = Kerfus.Satellites.FirstOrDefault(s => s.IsBreak);
                if (haveBreak == null)
                {
                    choices.Add(new("Нет сломанных серверов", "n"));
                    return choices;
                }
                List<Satellite> satellites = Kerfus.Satellites.Where(s => s.CodeName.Contains(satellite.ToString()) && s.IsBreak).ToList();
                for (int i = 0; i < satellites.Count; i++)
                {
                    choices.Add(new(satellites[i].CodeName, satellites[i].CodeName));
                }
                return choices;
            }
        }
        [SlashCommand("уведомления", "Куда бот должен отправлять уведомления по игре?")]
        private async Task setNotify(InteractionContext ctx,
            [Option("канал", "Куда отправлять уведомления?")] DiscordChannel channelForNotify)
        {
            if (channelForNotify.Type != ChannelType.Text)
            {
                var embed = new DiscordEmbedBuilder().WithTitle($"Канал, на который будут приходить уведомления, должен быть текстовым!");
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                return;
            }
            ChannelsForNotification allowed = new()
            {
                GuildID = (long)ctx.Guild.Id,
                ChannelID = (long)channelForNotify.Id,
            };
            Kerfus.ChannelsForNotifications.Add(allowed);
            Kerfus.SaveChanges();
            var embedNotify = new DiscordEmbedBuilder().WithTitle($"Я поставила уведомления на новый канал!");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embedNotify).AsEphemeral());
        }

        [SlashCommand("ремонт", "Починить сервер")]
        private async Task FixServer(InteractionContext ctx,
            [Autocomplete(typeof(ServersData))]
            [Option("сервер", "Выберите сервер для ремонта")] string serverID)
        {
            if (serverID == "n")
            {
                var embed = new DiscordEmbedBuilder().WithTitle("Я не могу найти сломанный сервер, мяу!")
                    .WithColor(DiscordColor.HotPink);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed));
                return;
            }
            Satellite server = Kerfus.Satellites.First(s => s.CodeName == serverID);
            var serverInstructionEmbed = new DiscordEmbedBuilder().WithTitle($"Починка сервера [{server.CodeName}]").WithDescription("Керфур не очень самостоятелен, и поэтому ему требуется ваша помощь." +
                "Вы должны правильно отвечать на лёгкие математические вопросы.\nЕсли ответ получается отрицательным - то ответ нужно выбрать не учитывая минус." +
                "\nЕсли же ответ получается больше 9 - то ответ нужно выбрать не учитывая десятки." +
                "\nПример: 1 - 3 (ОТВЕТ: 2)" +
                "\nПример: 9 + 9 (ОТВЕТ: 8)");
            var button = new DiscordButtonComponent(ButtonStyle.Success, "beginServerFix", "Начинаем");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(serverInstructionEmbed).AddComponents(button).AsEphemeral());

        }
        private async Task StatsSatellites(InteractionContext ctx)
        {
            //var lineEmoji = DiscordEmoji.FromGuildEmote(ctx.Client, 1228356438909259786);
            var embed = new DiscordEmbedBuilder().WithTitle($"{DiscordEmoji.FromName(ctx.Client, ":satellite:")} Сервера")
                .WithThumbnail("https://i.imgur.com/qOLHHqD.png")
                .WithDescription("Сервера нужно чинить для ловли сигналов. Каждый сломанный сервер замедляет процесс поиска сигналов, а если сломано больше половины - то процесс полностью останавливается.")
                .AddField("Код", "Пакеты | Починено раз");
            List<Satellite> satellites = Kerfus.Satellites.ToList();
            for (int i = 0; i < satellites.Count; i++)
            {
                string code = satellites[i].CodeName;
                bool breakNow = satellites[i].IsBreak;
                string breakNowText = breakNow ? "0/4" : "4/4";
                long? repairs = satellites[i].Repairs;
                embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":satellite:")} " + code.ToString(), $"{breakNowText} | {repairs}", true);
                if (i > countForPage - 2)
                {
                    decimal satelliteCount = Convert.ToDecimal(satellites.Count);
                    maxPages = (int)Math.Ceiling(satelliteCount / countForPage); // Считаем сколько получится страниц
                    break;
                }
            }
            var emptyEmoji = DiscordEmoji.FromGuildEmote(ctx.Client, 1228355062972153907);
            while ((embed.Fields.Count - 1) % 3 != 0)
                embed.AddField(emptyEmoji, emptyEmoji, true);
            embed.WithFooter($"Страница 1 из {maxPages}");
            if (satellites.Count > countForPage)
            {
                var nextPage = new DiscordButtonComponent(ButtonStyle.Secondary, "nextPageStatSattelites", $"{DiscordEmoji.FromName(ctx.Client, ":arrow_right:")}");
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(nextPage));
                return;
            }
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("test", "testll")]
        public async Task Test(InteractionContext ctx)
        {
            var emptyEmoji = DiscordEmoji.FromGuildEmote(ctx.Client, 1228355062972153907);
            var lineEmoji = DiscordEmoji.FromGuildEmote(ctx.Client, 1228356438909259786);
            var embed = new DiscordEmbedBuilder().WithTitle($"{DiscordEmoji.FromName(ctx.Client, ":satellite:")} Сервера")
                .AddField("Код", $"{emptyEmoji}", true)
                .AddField("Поломан", $"{emptyEmoji}", true)
                .AddField("Починено раз", $"{emptyEmoji}", true)
                .AddField($"{lineEmoji}{lineEmoji}{lineEmoji}{lineEmoji}{lineEmoji}{lineEmoji}{lineEmoji}{lineEmoji}", $"{emptyEmoji}");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}
