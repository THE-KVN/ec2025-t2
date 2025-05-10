using PlayableBot.Models;

namespace PlayableBot.ExtensionMethods;

public static class ExtensionMethods
{
    public static char ToChar(this CellContent cellContent)
    {
        return cellContent switch
        {
            CellContent.Empty => ' ',
            CellContent.Wall => '█',
            CellContent.Pellet => '•',
            CellContent.ZookeeperSpawn => '░',
            CellContent.AnimalSpawn => '※',
            _ => '?',
        };
    }
}
