export class Animal {
    constructor(id, x, y, spawnX, spawnY, score, capturedCounter, distanceCovered, isViable) {
        this.id = id;
        this.x = x;
        this.y = y;
        this.spawnX = spawnX;
        this.spawnY = spawnY;
        this.score = score;
        this.capturedCounter = capturedCounter;
        this.distanceCovered = distanceCovered;
        this.isViable = isViable;
    }
}