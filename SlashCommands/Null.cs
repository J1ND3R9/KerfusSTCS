using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace botForTRPO.SlashCommands
{
    internal class Null : ApplicationCommandModule
    {
        // Класс нужен для того, чтобы бот мог синхронизировать Slash команды.

        // Если бы этого класса не было, то бот мог отображать старые команды, которые были уже удалены.
    }
}
