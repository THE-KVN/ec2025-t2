using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreBot.Models
{
    // 1) The two states we’ll handle now
    public enum BotState
    {
        CollectingPellets,
        Respawning
    }
}
