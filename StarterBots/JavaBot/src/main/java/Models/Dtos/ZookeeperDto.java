package Models.Dtos;

import java.util.UUID;

public class ZookeeperDto {
    public UUID Id;
    public int X;
    public int Y;
    public int spawnX;
    public int spawnY;

    public UUID getId() {
        return Id;
    }

    public void setId(UUID id) {
        Id = id;
    }

    public int getX() {
        return X;
    }

    public void setX(int x) {
        X = x;
    }

    public int getY() {
        return Y;
    }

    public void setY(int y) {
        Y = y;
    }

    public int getSpawnX() {
        return spawnX;
    }

    public void setSpawnX(int spawnX) {
        this.spawnX = spawnX;
    }

    public int getSpawnY() {
        return spawnY;
    }

    public void setSpawnY(int spawnY) {
        this.spawnY = spawnY;
    }
}
