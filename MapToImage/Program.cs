using System.IO;
using SkiaSharp;
using Zooscape.Domain.Enums;

const int TILE_SIZE = 32;

List<string> filePaths = Directory.GetFiles("./StarterWorlds", "*.txt").ToList();

foreach (string filePath in filePaths)
{
    string fileContent = File.ReadAllText(filePath);
    int[][] cells = fileContent
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Select(l => l.Select(char.ToString).Select(int.Parse).ToArray())
        .ToArray();
    int height = fileContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
    int width = fileContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].Length;

    string imagePath = Path.ChangeExtension(filePath, "jpg");

    SKImageInfo info = new SKImageInfo(width * TILE_SIZE, height * TILE_SIZE);
    using (SKSurface? surface = SKSurface.Create(info))
    {
        SKCanvas? canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CellContents cell = (CellContents)cells[y][x];
                SKColor color = cell switch
                {
                    CellContents.Wall => SKColors.Black,
                    CellContents.ZookeeperSpawn => SKColors.Brown,
                    CellContents.AnimalSpawn => SKColors.Aqua,
                    _ => SKColors.Empty,
                };

                SKPaint paint = new SKPaint
                {
                    Color = color,
                    IsAntialias = false,
                    IsStroke = false,
                };

                canvas.DrawRect(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE, paint);
            }
        }
        using (SKImage? image = surface.Snapshot())
        using (SKData? data = image.Encode(SKEncodedImageFormat.Jpeg, 100))
        using (FileStream stream = File.OpenWrite(imagePath))
        {
            data.SaveTo(stream);
        }
    }
}
