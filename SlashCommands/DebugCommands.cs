using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace botForTRPO.SlashCommands
{
    internal class DebugCommands : ApplicationCommandModule
    {
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
        private async Task DebugUser(InteractionContext ctx)
        {
            
        }
    }
}
