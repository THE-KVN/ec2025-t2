using System;
using System.IO;

namespace Zooscape.Domain.Utilities;

public class WorldUtilities
{
    public static string ReadWorld(string worldPath)
    {
        try
        {
            string path = Path.Combine(Environment.CurrentDirectory, worldPath);
            using StreamReader streamReader = new(path);
            return streamReader.ReadToEnd();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error reading world {worldPath}: {e}");
            throw;
        }
    }
}
