package Models.Dtos;

import Enums.BotAction;

public class BotCommandDto {
    private BotAction action;

    public BotCommandDto() {}

    public BotCommandDto(BotAction action) {
        this.action = action;
    }

    public BotAction getAction() {
        return action;
    }

    public void setAction(BotAction action) {
        this.action = action;
    }
}
