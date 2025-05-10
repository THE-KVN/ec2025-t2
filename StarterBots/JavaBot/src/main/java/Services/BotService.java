package Services;

import Enums.BotAction;
import Models.Dtos.BotCommandDto;
import Models.Dtos.GameStateDto;

import java.util.UUID;

public class BotService {
    private UUID botId;
    private GameStateDto gameStateDto;

    public BotService() {
    }

    public GameStateDto getGameStateDto() {
        return gameStateDto;
    }

    public void setGameStateDto(GameStateDto gameStateDto) {
        this.gameStateDto = gameStateDto;
    }

    public UUID getBotId() {
        return botId;
    }

    public void setBotId(UUID botId) {
        this.botId = botId;
    }

    public BotCommandDto processState(GameStateDto gameState) {
        //Add your bot logic here
        return new BotCommandDto(BotAction.UP);
    }
}
