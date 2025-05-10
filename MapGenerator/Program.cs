using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;

namespace Zooscape.MapGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        // Define the options
        var sizeOption = new Option<int>(
            name: "--size",
            description: "Odd number specifying the width and height of the map"
        )
        {
            IsRequired = true,
        };

        var smoothnessOption = new Option<float>(
            name: "--smoothness",
            description: "Value between 0 and 1 (0 = super curvy, 1 = super straight)",
            getDefaultValue: () => 0.5f
        );

        var opennessOption = new Option<float>(
            name: "--openness",
            description: "Value between 0 and 1 (0 = minimal forks, 1 = lots of forks)",
            getDefaultValue: () => 0.5f
        );
        var teleportsOption = new Option<int>(
            name: "--teleports",
            description: "Number of teleports along the border",
            getDefaultValue: () => 0
        );
        var seedOption = new Option<int>(name: "--seed", description: "Random seed value");

        // Add validation to size
        sizeOption.AddValidator(OddNumberValidator("Size"));

        // Add validation to smoothness
        smoothnessOption.AddValidator(result =>
        {
            if (result.Tokens.Count == 0)
                return;

            if (double.TryParse(result.Tokens[0].Value, out double value))
            {
                if (value < 0 || value > 1)
                {
                    result.ErrorMessage = "Smoothness must be between 0 and 1.";
                }
            }
            else
            {
                result.ErrorMessage =
                    $"Smoothness must be a number ({result.Tokens[0].Value} is not)";
            }
        });

        // Add validation to smoothness
        opennessOption.AddValidator(result =>
        {
            if (result.Tokens.Count == 0)
                return;

            if (double.TryParse(result.Tokens[0].Value, out double value))
            {
                if (value < 0 || value > 1)
                {
                    result.ErrorMessage = "Openness must be between 0 and 1.";
                }
            }
            else
            {
                result.ErrorMessage =
                    $"Openness must be a number ({result.Tokens[0].Value} is not)";
            }
        });

        // Create root command and wire everything up
        var rootCommand = new RootCommand("MapGenerator")
        {
            sizeOption,
            smoothnessOption,
            opennessOption,
            teleportsOption,
            seedOption,
        };

        rootCommand.SetHandler(
            (int size, float smoothness, float openness, int teleports, int seed) =>
            {
                var map = new Map(size, smoothness, openness, teleports, seed);

                Console.WriteLine(map);
            },
            sizeOption,
            smoothnessOption,
            opennessOption,
            teleportsOption,
            seedOption
        );

        // Run the command line parser
        return await rootCommand.InvokeAsync(args);
    }

    static ValidateSymbolResult<OptionResult> OddNumberValidator(string arg)
    {
        return result =>
        {
            if (result.Tokens.Count == 0)
                return;

            if (int.TryParse(result.Tokens[0].Value, out int value))
            {
                if (value / 2 == value / 2.0)
                {
                    result.ErrorMessage = $"{arg} must be an odd number";
                }
            }
            else
            {
                result.ErrorMessage = $"{arg} must be a number ({result.Tokens[0].Value} is not)";
            }
        };
    }
}
