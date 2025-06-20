using NETCoreBot.Models;
using NETCoreBot.Strategy;

namespace NETCoreBot.Services
{
    public class BotService
    {
        private Guid _botId;

        public void SetBotId(Guid botId)
        {
            _botId = botId;
        }

        public Guid GetBotId()
        {
            return _botId;
        }

        public BotCommand ProcessState(GameState gameStateDTO)
        {
            //732 / 1412 / 1233
            //var numberofPellets = gameStateDTO.Cells.Count(c => c.Content == Enums.CellContent.Pellet);
           
            return Cobra.ProcessState(gameStateDTO, _botId);

        }
    }
}
