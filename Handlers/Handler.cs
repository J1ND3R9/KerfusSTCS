using botForTRPO.Models;
using botForTRPO.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private static Dictionary<ulong, GameClasses.ServerFixGame> fixServersDict;
        public async Task startFixServerGame(ComponentInteractionCreateEventArgs e)
        {
            var embed = e.Message.Embeds[0];
            var embedTitle = embed.Title;

            int index = embedTitle.IndexOf('[');
            var satelliteCodeName = embedTitle.Substring(index).Remove(0, 1);
            satelliteCodeName = satelliteCodeName.Remove(satelliteCodeName.Length - 1);
        }
    }
}
