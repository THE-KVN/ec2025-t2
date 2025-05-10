import {CellContent} from "./CellContent.js";

export class Cell {
    constructor(x, y) {
        this.x = x;
        this.y = y;
        this.cellContent = CellContent;
    }
}