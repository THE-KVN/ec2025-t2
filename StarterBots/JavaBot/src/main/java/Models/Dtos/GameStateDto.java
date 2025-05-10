package Models.Dtos;

import java.time.LocalDateTime;
import java.util.List;

public class GameStateDto {
    public LocalDateTime timeStamp;
    public int tick;
    public List<CellDto> cells;
    public List<AnimalDto> animals;
    public List<ZookeeperDto> zookeepers;

    public LocalDateTime getTimeStamp() {
        return timeStamp;
    }

    public void setTimeStamp(LocalDateTime timeStamp) {
        this.timeStamp = timeStamp;
    }

    public int getTick() {
        return tick;
    }

    public void setTick(int tick) {
        this.tick = tick;
    }

    public List<CellDto> getCells() {
        return cells;
    }

    public void setCells(List<CellDto> cells) {
        this.cells = cells;
    }

    public List<AnimalDto> getAnimals() {
        return animals;
    }

    public void setAnimals(List<AnimalDto> animals) {
        this.animals = animals;
    }

    public List<ZookeeperDto> getZookeepers() {
        return zookeepers;
    }

    public void setZookeepers(List<ZookeeperDto> zookeepers) {
        this.zookeepers = zookeepers;
    }
}
