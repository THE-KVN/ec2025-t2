package Models.Dtos;

import java.util.UUID;

public class AnimalDto {
    public UUID Id;
    public int X;
    public int Y;
    public int spawnX;
    public int spawnY;
    public int Score;
    public int capturedCounter;
    public int distanceCovered;
    public boolean IsViable;

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

    public int getScore() {
        return Score;
    }

    public void setScore(int score) {
        Score = score;
    }

    public int getCapturedCounter() {
        return capturedCounter;
    }

    public void setCapturedCounter(int capturedCounter) {
        this.capturedCounter = capturedCounter;
    }

    public int getDistanceCovered() {
        return distanceCovered;
    }

    public void setDistanceCovered(int distanceCovered) {
        this.distanceCovered = distanceCovered;
    }

    public boolean isViable() {
        return IsViable;
    }

    public void setViable(boolean viable) {
        IsViable = viable;
    }
}
