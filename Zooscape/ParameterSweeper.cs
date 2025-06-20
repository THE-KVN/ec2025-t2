using NETCoreBot.Strategy;
using System;
using System.Collections.Generic;
using Zooscape; // for Cobra static settings

namespace Zooscape
{
    public static class ParameterSweeper
    {
        private static readonly Random _rand = new Random();

        public static readonly List<double> ALPHA_VALUES =
            new() { 0.8, 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 2.0 };
        public static readonly List<int> VISITED_TILE_PENALTY_FACTOR_VALUES =
            new() { 0, 1, 2, 3, 4, 5 };
        public static readonly List<int> REVERSING_TO_PARENT_PENALTY_VALUES =
            new() { 1, 2, 3, 4, 5 };
        public static readonly List<double> ZOOKEEPER_AVOIDANCE_FACTOR_VALUES =
            new() { 10, 12, 14, 16, 18, 20, 22 };
        public static readonly List<int> ENEMY_PATH_AVOIDANCE_VALUES =
            new() { 0, 1, 2, 3, 4, 5 };
        public static readonly List<int> PELLET_BONUS_VALUES =
            new() { 0, 3, 6, 9, 12, 15 };
        public static readonly List<int> DANGER_THRESHOLD_VALUES =
            new() { 100, 200, 300 };
        public static readonly List<int> MAX_PELLET_CANDIDATES_VALUES =
            new() { 3, 6, 9 };
        public static readonly List<int> CORRIDOR_PENALTY_VALUES =
            new() { 0, 10, 20, 30, 40, 50 };

        /// <summary>
        /// Pulls one random combination from each discrete list.
        /// </summary>
        public static Dictionary<string, object> GenerateRandomParameter()
        {
            return new Dictionary<string, object>
            {
                ["ALPHA"] = ALPHA_VALUES[_rand.Next(ALPHA_VALUES.Count)],
                ["VISITED_TILE_PENALTY_FACTOR"] = VISITED_TILE_PENALTY_FACTOR_VALUES[_rand.Next(VISITED_TILE_PENALTY_FACTOR_VALUES.Count)],
                ["REVERSING_TO_PARENT_PENALTY"] = REVERSING_TO_PARENT_PENALTY_VALUES[_rand.Next(REVERSING_TO_PARENT_PENALTY_VALUES.Count)],
                ["ZOOKEEPER_AVOIDANCE_FACTOR"] = ZOOKEEPER_AVOIDANCE_FACTOR_VALUES[_rand.Next(ZOOKEEPER_AVOIDANCE_FACTOR_VALUES.Count)],
                ["ENEMY_PATH_AVOIDANCE"] = ENEMY_PATH_AVOIDANCE_VALUES[_rand.Next(ENEMY_PATH_AVOIDANCE_VALUES.Count)],
                ["PELLET_BONUS"] = PELLET_BONUS_VALUES[_rand.Next(PELLET_BONUS_VALUES.Count)],
                ["DANGER_THRESHOLD"] = DANGER_THRESHOLD_VALUES[_rand.Next(DANGER_THRESHOLD_VALUES.Count)],
                ["MAX_PELLET_CANDIDATES"] = MAX_PELLET_CANDIDATES_VALUES[_rand.Next(MAX_PELLET_CANDIDATES_VALUES.Count)],
                ["CORRIDOR_PENALTY"] = CORRIDOR_PENALTY_VALUES[_rand.Next(CORRIDOR_PENALTY_VALUES.Count)]
            };
        }

        /// <summary>
        /// Pushes those dictionary values into your Cobra static settings.
        /// </summary>
        public static void ApplyParameters(Dictionary<string, object> p)
        {
            //Cobra.ALPHA = Convert.ToDouble(p["ALPHA"]);
            //Cobra.VISITED_TILE_PENALTY_FACTOR = Convert.ToInt32(p["VISITED_TILE_PENALTY_FACTOR"]);
            //Cobra.REVERSING_TO_PARENT_PENALTY = Convert.ToInt32(p["REVERSING_TO_PARENT_PENALTY"]);
            //Cobra.ZOOKEEPER_AVOIDANCE_FACTOR = Convert.ToDouble(p["ZOOKEEPER_AVOIDANCE_FACTOR"]);
            //Cobra.ENEMY_PATH_AVOIDANCE = Convert.ToInt32(p["ENEMY_PATH_AVOIDANCE"]);
            //Cobra.PELLET_BONUS = Convert.ToInt32(p["PELLET_BONUS"]);
            //Cobra.DANGER_THRESHOLD = Convert.ToInt32(p["DANGER_THRESHOLD"]);
            //Cobra.MAX_PELLET_CANDIDATES = Convert.ToInt32(p["MAX_PELLET_CANDIDATES"]);
            //Cobra.CORRIDOR_PENALTY = Convert.ToInt32(p["CORRIDOR_PENALTY"]);
        }


        /// <summary>
        /// CSV‐friendly one‐line summary in the fixed order.
        /// </summary>
        public static string FormatParametersCsv(Dictionary<string, object> p) =>
            string.Join(",",
                p["ALPHA"],
                p["VISITED_TILE_PENALTY_FACTOR"],
                p["REVERSING_TO_PARENT_PENALTY"],
                p["ZOOKEEPER_AVOIDANCE_FACTOR"],
                p["ENEMY_PATH_AVOIDANCE"],
                p["PELLET_BONUS"],
                p["DANGER_THRESHOLD"],
                p["MAX_PELLET_CANDIDATES"],
                p["CORRIDOR_PENALTY"]
            );
    }
}
