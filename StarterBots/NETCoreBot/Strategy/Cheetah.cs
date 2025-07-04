﻿using NETCoreBot.Enums;
using NETCoreBot.Models;
using NETCoreBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NETCoreBot.Strategy
{
    public static class Cheetah
    {
        #region Variables

        public static GameState GAME_STATE { get; set; }
        public static Animal ME { get; set; }
        public static Dictionary<(int, int), CellContent> GRID { get; set; }


        public static int MapWidth = 30;  // Replaced at runtime
        public static int MapHeight = 20; // Replaced at runtime
        public static bool PortalLeft = true;
        public static bool PortalRight = true;
        public static bool PortalUp = true;
        public static bool PortalDown = true;

        public static List<CheetahNode>? PersistentPath { get; set; } = null;
        public static double? PersistentPathScore = null;
        public static Cell? PersistentTarget { get; set; } = null;

        public static bool IsInDanger = false;
        public static BotAction? LastMove { get; set; } = null;
        public static int ExecutionTimeExceedCount { get; private set; } = 0;

        public static int LastTickCommandIssued = 0;
        public static (int X, int Y)? LastSentPosition = null;


        //Maps
        public static HashSet<(int, int)> VISITED_CELLS_MAP = new();
        public static HashSet<(int, int)> CONTESTED_CELLS_MAP = new();
        public static HashSet<(int, int)> CORRIDOR_CELLS_MAP = new();
        public static Dictionary<(int x, int y), int> DISTANCE_MAP = new();
        public static HashSet<(int, int)> ZOOKEEPER_POS_MAP = new();
        public static Dictionary<(int, int), int> PELLET_HEAT_MAP = new();
        public static HashSet<(int, int)> SAFETY_NET_MAP = new();

        public static Dictionary<Guid, Dictionary<(int x, int y), int>> ZOOKEEPER_DISTANCE_MAPS_BY_ID = new();




        public static GameStage GAME_STAGE = GameStage.EarlyGame;
        public static int TOTAL_PELLETS = 0;
        public static int PELLETS_LEFT = 0;
        public static double PELLETS_LEFT_PERCENTAGE = 0;


        public static CheetahCluster BestCluster { get; set; }

        private const int PELLET_MARGIN = 60;   // pellets needed to outweigh flip-flop


        public static int CurrentDeathCounter = 0;

        public static bool PortalEnabled = true; // Enable or disable portal usage
        public static int PortalsLockedUntilTick = -1; // Tick until which portals are locked
        private const int PORTAL_COOLDOWN = 6;

        #endregion

        #region Parameters

        public static double ALPHA = 1.3;
        public static int MAX_PELLET_CANDIDATES = 5;
        public static int DFSDEPTH = 75;
        public static int MAX_CLUSTER_SIZE = 8; // Maximum size of a cluster to consider

        public static int MAX_CLUSTER_PELLETS_LIMIT = 75;


        //locked
        public const int SAFE_DISTANCE = 5;
        //public const int RESPAWN_BORDER = 10;

        //constant based on config
        public const int SCORE_STREAK_RESET = 3;


        //Bonus
        public const int PELLET_SCORE = 1;
        public const int SCORE_STREAK = 1;
        public const int STREAK_WEIGHT = 2;

        //Penalty
        public const int REVERSING_TO_PARENT_PENALTY = 1;
        public const int VISITED_CELL_PENALTY = 1;
        public const int CONTESTED_CELL_PENALTY = 1;
        public const int CORRIDOR_CELL_PENALTY = 1;
        public const double ZOOKEEPER_AVOIDANCE_FACTOR = 20;



        #endregion

        public static BotCommand? ProcessState(GameState gameStateDTO, Guid id)
        {

            GAME_STATE = gameStateDTO;
            ME = GAME_STATE.Animals.FirstOrDefault(x => x.Id == id);
            GRID = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);

            MapWidth = GAME_STATE.Cells.Max(c => c.X) + 1;
            MapHeight = GAME_STATE.Cells.Max(c => c.Y) + 1;



            //using (GameLogger.Benchmark("Tick Total"))
            //{
            //Information 
            SetGameStage();
            ApplyParameters();

            GameLogger.LogInfo("Animal", $"Current Location ({ME.X},{ME.Y}) | Stage: {GAME_STAGE}");

            #region Maps
            using (GameLogger.Benchmark("[Map] Visited Cells"))
            {
                var currentPos = (ME.X, ME.Y);
                if (!VISITED_CELLS_MAP.Contains(currentPos))
                {
                    VISITED_CELLS_MAP.Add(currentPos);
                    //GameLogger.LogCollection("VISITED_CELLS_MAP", VISITED_CELLS_MAP);
                }
            }

            using (GameLogger.Benchmark("[Map] Contested Cells"))
            {
                CONTESTED_CELLS_MAP = MapContestedCell();
                //GameLogger.LogCollection("CONTESTED_CELLS_MAP", CONTESTED_CELLS_MAP);
            }

            using (GameLogger.Benchmark("[Map] Corridor Cells"))
            {
                CORRIDOR_CELLS_MAP = MapCorridorCell();
                //GameLogger.LogCollection("CONTESTED_CELLS_MAP", CONTESTED_CELLS_MAP);
            }

            using (GameLogger.Benchmark("[Map] Distance"))
            {
                DISTANCE_MAP = MapDistance();
                //GameLogger.LogCollection("CONTESTED_CELLS_MAP", CONTESTED_CELLS_MAP);
            }

            using (GameLogger.Benchmark("[Map] Zookeeper Positions"))
            {
                ZOOKEEPER_POS_MAP = MapZookeeperPositions();
                //GameLogger.LogCollection("ZOOKEEPER_POS_MAP", ZOOKEEPER_POS_MAP);
            }

            using (GameLogger.Benchmark("[Map] Pellet Heat"))
            {
                PELLET_HEAT_MAP = MapPelletHeat();
                //GameLogger.LogCollection("ZOOKEEPER_POS_MAP", ZOOKEEPER_POS_MAP);
            }

            using (GameLogger.Benchmark("[Map] Safety Net"))
            {
                SAFETY_NET_MAP = MapSafetyNet();
            }

            using (GameLogger.Benchmark("[Map] Zookeeper Distance"))
            {
                ZOOKEEPER_DISTANCE_MAPS_BY_ID = MapZookeeperDistance();
                //GameLogger.LogCollection("CONTESTED_CELLS_MAP", CONTESTED_CELLS_MAP);
            }

            #endregion


            //Events
            using (GameLogger.Benchmark("[Event] Synch"))
            {
                var synchEventResult = SynchEvent();
                if (synchEventResult == null)
                {
                    GameLogger.LogError("SynchEvent", "Synch Error returning null");
                    return FinaliseAction(null);
                }
            }

            using (GameLogger.Benchmark("[Event] Portal Evaluation"))
            {
                var portalEvalEventResult = PortalEvalEvent();
                if (portalEvalEventResult != null)
                {
                    GameLogger.LogInfo("PortalEvalEvent", "Greener fields detected");
                    return FinaliseAction(portalEvalEventResult);
                }
            }

            using (GameLogger.Benchmark("[Event] Escape Zookeeper"))
            {
                if (GAME_STATE.Tick == PortalsLockedUntilTick)
                {
                    PortalEnabled = true; // Re-enable portals after lock period
                    PersistentPath = null;
                    PersistentTarget = null;
                    PersistentPathScore = null;
                }


                var escapeZookeeperResult = EscapeZookeeperEvent();
                if (escapeZookeeperResult != null)
                {
                    GameLogger.LogInfo("EscapeZookeeperEvent", "Escaping");
                    return FinaliseAction(escapeZookeeperResult);
                }
            }



            //Plan
            using (GameLogger.Benchmark("[Plan]"))
            {
                var plan = Plan();
                if (plan == null)
                {
                    GameLogger.LogError("Plan", "No plan found");
                    return FinaliseAction(null);
                }
            }


            //Move
            using (GameLogger.Benchmark("[Move]"))
            {
                BotAction? action = ComputeNextMove();
                if (!action.HasValue)
                {
                    GameLogger.LogError("Move", "No action found");
                    return FinaliseAction(null);
                }

                PersistentPath?.RemoveAt(0);

                return FinaliseAction(new BotCommand
                {
                    Action = action.Value
                });
            }

            //}
        }
        public static void SetGameStage()
        {
            PELLETS_LEFT = GAME_STATE.Cells.Where(C => C.Content == CellContent.Pellet).Count();
            TOTAL_PELLETS = GAME_STATE.Cells.Where(C => C.Content == CellContent.Pellet || C.Content == CellContent.Empty).Count();
            PELLETS_LEFT_PERCENTAGE = (double)PELLETS_LEFT / TOTAL_PELLETS * 100;

            if (PELLETS_LEFT_PERCENTAGE > 60)
            {
                GAME_STAGE = GameStage.EarlyGame;

            }
            else if (PELLETS_LEFT_PERCENTAGE <= 60 && PELLETS_LEFT_PERCENTAGE > 15)
            {
                GAME_STAGE = GameStage.EarlyGame;
            }
            else
            {
                GAME_STAGE = GameStage.EarlyGame;
            }
        }
        public static void ApplyParameters()
        {
            switch (GAME_STAGE)
            {
                case GameStage.EarlyGame:
                    ALPHA = 1.4;
                    break;
                case GameStage.MidGame:
                    ALPHA = 1.4;
                    DFSDEPTH = 40;
                    break;
                case GameStage.LateGame:
                    ALPHA = 1.4;
                    break;
                default:
                    break;
            }
        }

        #region Maps

        public static HashSet<(int, int)> MapContestedCell()
        {
            if (GAME_STATE.Cells == null)
                return new HashSet<(int, int)>();

            var contested = new HashSet<(int, int)>();

            foreach (var animal in GAME_STATE.Animals)
            {
                if (animal.Id == ME.Id) continue;

                var pellet = FindPelletOponent(animal);
                if (pellet == null) continue;

                int myDist = ManhattanDistance(ME.X, ME.Y, pellet.X, pellet.Y);
                int oppDist = ManhattanDistance(animal.X, animal.Y, pellet.X, pellet.Y);
                if (oppDist < myDist)
                {
                    contested.Add((pellet.X, pellet.Y));
                }


                var path = FindPathOpponent(animal.X, animal.Y, pellet.X, pellet.Y);
                if (path != null && path.Count > 1)
                {
                    // direction of travel for this opponent
                    var start = path[0];
                    var firstStep = path[1];
                    int dirX = firstStep.X - start.X;
                    int dirY = firstStep.Y - start.Y;

                    // mark the first couple of steps as contested
                    for (int i = 1; i < Math.Min(3, path.Count); i++)
                    {
                        int myDist1 = ManhattanDistance(ME.X, ME.Y, path[i].X, path[i].Y);
                        int oppDist1 = ManhattanDistance(animal.X, animal.Y, path[i].X, path[i].Y);
                        if (oppDist1 < myDist1)
                        {
                            contested.Add((path[i].X, path[i].Y));
                        }
                    }

                    // extend up to 10 pellets in line
                    int chainX = pellet.X + dirX;
                    int chainY = pellet.Y + dirY;
                    for (int count = 0; count < 3; count++)
                    {
                        if (!GRID.TryGetValue((chainX, chainY), out var content)
                            || content != CellContent.Pellet)
                        {
                            break;
                        }
                        int myDist1 = ManhattanDistance(ME.X, ME.Y, chainX, chainY);
                        int oppDist1 = ManhattanDistance(animal.X, animal.Y, chainX, chainY);
                        if (oppDist1 < myDist1)
                        {
                            contested.Add((chainX, chainY));
                        }
                        chainX += dirX;
                        chainY += dirY;
                    }

                    // 3) last few steps *before* the pellet
                    const int tailCount = 2;
                    // ensure we start at least at index 1 (skip the animal's current pos)
                    int startTail = Math.Max(1, path.Count - 1 - tailCount);
                    // go up to but exclude the pellet itself (path.Count-1)
                    for (int i = startTail; i < path.Count - 1; i++)
                    {
                        int myDist1 = ManhattanDistance(ME.X, ME.Y, path[i].X, path[i].Y);
                        int oppDist1 = ManhattanDistance(animal.X, animal.Y, path[i].X, path[i].Y);
                        if (oppDist1 < myDist1)
                        {
                            contested.Add((path[i].X, path[i].Y));
                        }
                    }



                }
            }

            ////REMOVE THIS IF MULTIPLE ZOOKEEPERS
            //foreach (var zookeeper in GAME_STATE.Zookeepers)
            //{

            //    var animal = FindNearestAnimalToZookeeper(zookeeper.X, zookeeper.Y, GAME_STATE.Animals, null);
            //    if (animal == null) continue;

            //    var path = FindPathZookeeper(zookeeper.X, zookeeper.Y, animal.X, animal.Y, grid);
            //    if (path != null && path.Count > 1)
            //    {
            //        for (int i = 1; i < Math.Min(5, path.Count); i++)
            //        {
            //            var step = path[i];
            //            contested.Add((step.X, step.Y));
            //        }
            //    }
            //}

            return contested;
        }
        public static HashSet<(int, int)> MapCorridorCell()
        {
            var avoid = new HashSet<(int, int)>();
            foreach (var ((x, y), content) in GRID)
            {
                if (content != CellContent.Pellet) continue;
                int wallCount = 0;
                var dirs = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

                foreach (var (dx, dy) in dirs)
                {
                    var neighbor = (x + dx, y + dy);
                    if (GRID.TryGetValue(neighbor, out var nContent) && nContent == CellContent.Wall)
                        wallCount++;
                }

                if (wallCount >= 3)
                {
                    avoid.Add((x, y));
                    foreach (var (dx, dy) in dirs)
                    {
                        var neighbor = (x + dx, y + dy);
                        if (GRID.TryGetValue(neighbor, out var nContent) && nContent == CellContent.Pellet)
                            avoid.Add(neighbor);
                    }
                }
            }
            return avoid;
        }
        public static Dictionary<(int x, int y), int> MapDistance()
        {
            var distMap = new Dictionary<(int, int), int>();
            var queue = new Queue<(int, int)>();
            queue.Enqueue((ME.X, ME.Y));
            distMap[(ME.X, ME.Y)] = 0;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                int cost = distMap[(x, y)];

                foreach (var (nx, ny) in GetNeighbors(x, y))
                {
                    if (!distMap.ContainsKey((nx, ny)) && IsWalkable(nx, ny))
                    {
                        distMap[(nx, ny)] = cost + 1;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return distMap;
        }
        public static HashSet<(int, int)> MapZookeeperPositions()
        {
            var zkPositions = new HashSet<(int, int)>();
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                zkPositions.Add((zk.X, zk.Y));
            }
            return zkPositions;
        }
        private static Dictionary<(int x, int y), int> MapPelletHeat()
        {
            var distMap = new Dictionary<(int, int), int>();

            // BFS from every pellet (multi-source – like keeper map)
            var q = new Queue<(int, int)>();

            foreach (var cell in GAME_STATE.Cells.Where(c => c.Content == CellContent.Pellet))
            {
                var p = (cell.X, cell.Y);
                distMap[p] = 10;          // max score at the pellet itself
                q.Enqueue(p);
            }

            // simple outward decay until it hits walls
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                int score = distMap[(x, y)];

                if (score <= 1) continue;     // nothing left to propagate

                foreach (var nb in GetNeighbors(x, y))
                {
                    if (!IsWalkable(nb.x, nb.y)) continue;
                    // keep the highest heat seen so far for this tile
                    if (!distMap.ContainsKey(nb) || distMap[nb] < score - 1)
                    {
                        distMap[nb] = score - 1;
                        q.Enqueue(nb);
                    }
                }
            }

            return distMap;
        }
        public static HashSet<(int, int)> MapSafetyNet()
        {
            var border = new HashSet<(int, int)>();

            // 1️⃣  Breadth-first search out to maxDistance (walls excluded).
            var distMap = MapDistance((ME.X, ME.Y), SAFE_DISTANCE);

            // 2️⃣  Probe only the ring around the spawn.
            foreach (var (x, y) in EnumerateRing((ME.X, ME.Y), SAFE_DISTANCE))
            {
                if (x < 0 || y < 0) continue;           // discard negatives

                if (distMap.TryGetValue((x, y), out var d) && d <= SAFE_DISTANCE)
                    border.Add((x, y));
            }

            return border;
        }
        public static Dictionary<Guid, Dictionary<(int x, int y), int>> MapZookeeperDistance()
        {
            ZOOKEEPER_DISTANCE_MAPS_BY_ID.Clear();

            var zdm = new Dictionary<Guid, Dictionary<(int x, int y), int>>();

            foreach (var zk in GAME_STATE.Zookeepers)
            {
                var distMap = new Dictionary<(int, int), int>();
                var queue = new Queue<(int x, int y, int dist)>();
                var visited = new HashSet<(int, int)>();

                var start = (zk.X, zk.Y);
                queue.Enqueue((start.X, start.Y, 0));
                visited.Add(start);
                distMap[start] = 0;

                while (queue.Count > 0)
                {
                    var (x, y, d) = queue.Dequeue();

                    foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                    {
                        var (nx, ny) = Warp(x + dx, y + dy); // Handles portal wrap

                        if (!GRID.ContainsKey((nx, ny))) continue;
                        if (GRID[(nx, ny)] == CellContent.Wall) continue;

                        var pos = (nx, ny);
                        if (visited.Contains(pos)) continue;

                        visited.Add(pos);
                        distMap[pos] = d + 1;
                        queue.Enqueue((nx, ny, d + 1));
                    }
                }

                zdm[zk.Id] = distMap;
            }

            return zdm;
        }

        private static IEnumerable<(int x, int y)> GetNeighbors(int x, int y)
        {
            // Up
            if (y > 0)
                yield return (x, y - 1);
            else if (PortalUp)
                yield return (x, MapHeight - 1);

            // Down
            if (y < MapHeight - 1)
                yield return (x, y + 1);
            else if (PortalDown)
                yield return (x, 0);

            // Left
            if (x > 0)
                yield return (x - 1, y);
            else if (PortalLeft)
                yield return (MapWidth - 1, y);

            // Right
            if (x < MapWidth - 1)
                yield return (x + 1, y);
            else if (PortalRight)
                yield return (0, y);
        }
        private static IEnumerable<(int x, int y)> GetNeighborsZK(int x, int y)
        {
            // Up
            if (y > 0)
                yield return (x, y - 1);


            // Down
            if (y < MapHeight - 1)
                yield return (x, y + 1);


            // Left
            if (x > 0)
                yield return (x - 1, y);


            // Right
            if (x < MapWidth - 1)
                yield return (x + 1, y);

        }
        public static bool IsWalkable(int x, int y, bool includeZookeeper = true)
        {
            // basic grid check
            if (!GRID.TryGetValue((x, y), out var content))
                return false;

            // static obstacles
            if (content == CellContent.Wall ||
                content == CellContent.AnimalSpawn ||
                content == CellContent.ZookeeperSpawn)
                return false;

            // dynamic obstacle – keeper standing here
            if (includeZookeeper)
            {
                if (ZOOKEEPER_POS_MAP.Contains((x, y)))
                    return false;
            }

            return true;
        }


        public static IEnumerable<(int x, int y)> EnumerateRing(
        (int x, int y) origin, int maxDistance)
        {
            for (int dx = -maxDistance; dx <= maxDistance; dx++)
            {
                int remaining = maxDistance - Math.Abs(dx);
                for (int dy = -remaining; dy <= remaining; dy++)
                    yield return (origin.x + dx, origin.y + dy);
            }
        }
        public static Dictionary<(int x, int y), int> MapDistance(
        (int x, int y) start, int maxDistance)
        {
            var dist = new Dictionary<(int, int), int>
            {
                [start] = 0
            };

            var queue = new Queue<(int, int)>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                int cost = dist[(x, y)];

                if (cost == maxDistance) continue;      // early-stop BFS

                foreach (var (nx, ny) in GetNeighbors(x, y))
                {
                    if (nx < 0 || ny < 0) continue;     // skip negatives here too
                    if (!IsWalkable(nx, ny, false) || dist.ContainsKey((nx, ny))) continue;

                    dist[(nx, ny)] = cost + 1;
                    queue.Enqueue((nx, ny));
                }
            }

            return dist;
        }




        #endregion

        #region Events

        public static BotAction? SynchEvent()
        {
            if (LastSentPosition.HasValue && LastMove.HasValue && GAME_STATE.Tick == LastTickCommandIssued + 1 && GAME_STATE.Tick > 1)
            {
                var expectedPos = GetExpectedPosition(LastSentPosition.Value, LastMove.Value);
                if (ME.X != expectedPos.X || ME.Y != expectedPos.Y)
                {
                    GameLogger.LogInfo("SynchEvent", $"[Synch Failed] Engine out-of-sync: Expected position ({expectedPos.X}, {expectedPos.Y}) but got ({ME.X}, {ME.Y})");
                    LastTickCommandIssued = GAME_STATE.Tick;
                    LastSentPosition = null;
                    LastMove = null;

                    PersistentPath = null;
                    PersistentTarget = null;
                    PersistentPathScore = null;
                    BestCluster = null;

                    return null;
                }
            }

            return BotAction.Up;
        }
        public static BotCommand? PortalEvalEvent()
        {

            //-----------------------------------------------------------------------
            // 0. cool-down guard
            //-----------------------------------------------------------------------
            if (GAME_STATE.Tick < PortalsLockedUntilTick)
                return null;                       // still locked – skip evaluation

            //-----------------------------------------------------------------------
            // 1. must be standing within 1 tile of a portal square
            //-----------------------------------------------------------------------
            if (!IsNearPortalCell())
                return null;



            //-----------------------------------------------------------------------
            // 2. determine “this” vs “other” side
            //-----------------------------------------------------------------------
            bool isLeft = ME.X <= 5;
            bool isRight = ME.X >= MapWidth - 6;
            bool isTop = ME.Y <= 5;
            bool isBottom = ME.Y >= MapHeight - 6;

            Func<Cell, bool> thisSide = _ => true;
            Func<Cell, bool> otherSide = _ => false;

            if (isLeft || isRight)
            {
                thisSide = c => isLeft ? c.X < MapWidth / 2 : c.X >= MapWidth / 2;
                otherSide = c => !thisSide(c);
            }
            else
            {
                thisSide = c => isTop ? c.Y < MapHeight / 2 : c.Y >= MapHeight / 2;
                otherSide = c => !thisSide(c);
            }

            //-----------------------------------------------------------------------
            // 3. weigh pellets / enemies
            //-----------------------------------------------------------------------
            int thisPellets = GAME_STATE.Cells.Count(c => c.Content == CellContent.Pellet && thisSide(c));
            int otherPellets = GAME_STATE.Cells.Count(c => c.Content == CellContent.Pellet && otherSide(c));

            int thisEnemies = GAME_STATE.Animals.Count(a => a.Id != ME.Id && a.IsViable && thisSide(new Cell { X = a.X, Y = a.Y }));
            int otherEnemies = GAME_STATE.Animals.Count(a => a.Id != ME.Id && a.IsViable && otherSide(new Cell { X = a.X, Y = a.Y }));

            //-----------------------------------------------------------------------
            // 4. safety of portal exit
            //-----------------------------------------------------------------------
            int exitX = ME.X, exitY = ME.Y;
            if (isLeft && PortalRight) exitX = MapWidth - 1;
            else if (isRight && PortalLeft) exitX = 0;
            else if (isTop && PortalDown) exitY = MapHeight - 1;
            else if (isBottom && PortalUp) exitY = 0;

            bool portalExitSafe = GAME_STATE.Zookeepers
                .All(zk => ManhattanDistance(zk.X, zk.Y, exitX, exitY) >= SAFE_DISTANCE + 3);

            //-----------------------------------------------------------------------
            // 5. decide
            //-----------------------------------------------------------------------
            bool pelletsBetter = otherPellets >= thisPellets + PELLET_MARGIN;
            bool enemiesNotWorse = otherEnemies <= thisEnemies;
            bool shouldCross = pelletsBetter && enemiesNotWorse && portalExitSafe;

            if (!shouldCross)
            {
                //GameLogger.LogWatch("PortalEvalEvent",
                //    $"Chose to stay this side [P:{thisPellets} A:{thisEnemies}] / " +
                //    $"[P:{otherPellets} A:{otherEnemies}]");
                return null;
            }

            //-----------------------------------------------------------------------
            // 6. pick the nearest portal cell on the current edge
            //-----------------------------------------------------------------------
            var targetPortal = GAME_STATE.Cells
                .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                .OrderBy(c => ManhattanDistance(ME.X, ME.Y, c.X, c.Y))
                .FirstOrDefault();

            if (targetPortal == null) return null;

            //-----------------------------------------------------------------------
            // 7. build path & move
            //-----------------------------------------------------------------------
            var path = FindPath(ME.X, ME.Y, targetPortal.X, targetPortal.Y);
            if (path == null || path.Count == 0) return null;

            PersistentPath = path;
            PersistentTarget = targetPortal;
            PersistentPathScore = EvaluatePathScore(path);

            // crossing move (if already on portal tile path.Count==1)
            BotAction? crossAct = path.Count == 1
                ? GetDirection(ME.X, ME.Y, exitX, exitY)          // immediate wrap
                : ComputeNextMove();                              // step toward portal

            if (!crossAct.HasValue) return null;

            // --- set cool-down so we don’t bounce back immediately ---
            PortalsLockedUntilTick = GAME_STATE.Tick + PORTAL_COOLDOWN;

            path.RemoveAt(0);                                     // advance path
            return new BotCommand { Action = crossAct.Value };
        }
        public static BotCommand? RespawnEvent()
        {
            if (((CurrentDeathCounter < ME.CapturedCounter) || ME.IsViable == false) && GAME_STATE.Tick > 5)
            {
                GameLogger.LogWatch("RespawnEvent", "Respawning");
                CurrentDeathCounter = ME.CapturedCounter;
                if (!IsSafeToLeaveSpawn())
                {
                    GameLogger.LogError("RespawnEvent", "Not safe to leave spawn");
                    return null; // Not safe to leave spawn
                }
            }

            return new BotCommand() { Action = BotAction.Up }; // Default action after respawn
        }



        public static BotCommand? EscapeZookeeperEvent()
        {
            //if (GAME_STAGE != GameStage.EarlyGame)
            //{
            if (IsZookeeperNear())
            {
                IsInDanger = true;

                GameLogger.LogWatch("EscapeZookeeperEvent", "Zookeeper is near, escaping");

                if (HasPortals())
                {
                    PersistentPath = null;
                    PersistentTarget = null;

                    var escape = EscapeViaPortalMultiZK(); // your existing routine – unchanged
                    if (escape != null)
                    {

                        return FinaliseAction(escape);
                    }
                }
                else
                {
                    //    //TODO
                    GameLogger.LogWatch("EscapeZookeeperEvent", "TODO");
                }

            }
            else
            {
                IsInDanger = false; // No zookeeper nearby, reset danger state
            }
            //}

            return null;
        }

        #region Zookeeper avoidance logic
        public static bool IsZookeeperNear(Animal myAnimal)
        {
            // Loop through each zookeeper in the game state.
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                int distance = ManhattanDistance(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (distance <= (SAFE_DISTANCE * 2))
                {

                    var pathToMe = FindPathZookeeper(zk.X, zk.Y, myAnimal.X, myAnimal.Y, GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content));
                    if (pathToMe != null && pathToMe.Count <= SAFE_DISTANCE)
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        public static List<CheetahNode>? FindPathZookeeper(
            int startX,
            int startY,
            int targetX,
            int targetY,
            Dictionary<(int, int), CellContent> grid)
        {

            var openSet = new List<CheetahNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new CheetahNode(
                startX, startY,
                0,
                ManhattanDistance(startX, startY, targetX, targetY),
                null,
                0,
                0, 0, null
            );
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    int cmp = a.F.CompareTo(b.F);
                    if (cmp == 0)
                    {
                        cmp = a.H.CompareTo(b.H);
                        if (cmp == 0)
                        {
                            cmp = a.TieBreak.CompareTo(b.TieBreak);
                        }
                    }
                    return cmp;
                });

                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current);

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighborsZookeeper(current, grid, targetX, targetY))
                {
                    if (closedSet.Contains((neighbor.X, neighbor.Y)))
                        continue;

                    var existing = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existing != null && existing.G <= neighbor.G)
                        continue;

                    openSet.Add(neighbor);
                }
            }
            return null;
        }
        public static bool HasPortals()
        {
            // Compute the map dimensions from the cells.
            int width = GAME_STATE.Cells.Max(c => c.X) + 1;
            int height = GAME_STATE.Cells.Max(c => c.Y) + 1;

            // A cell is considered passable if it is not a wall.
            // (You may need to adjust this depending on your game's rules.)
            bool leftEdgeOpen = GAME_STATE.Cells.Any(c => c.X == 0 && c.Content != CellContent.Wall);
            bool rightEdgeOpen = GAME_STATE.Cells.Any(c => c.X == width - 1 && c.Content != CellContent.Wall);
            bool topEdgeOpen = GAME_STATE.Cells.Any(c => c.Y == 0 && c.Content != CellContent.Wall);
            bool bottomEdgeOpen = GAME_STATE.Cells.Any(c => c.Y == height - 1 && c.Content != CellContent.Wall);

            // We consider portals available if either the horizontal or vertical edges are open.
            return (leftEdgeOpen && rightEdgeOpen) || (topEdgeOpen && bottomEdgeOpen);
        }
        public static IEnumerable<CheetahNode> GetNeighborsZookeeper(
            CheetahNode current,
            Dictionary<(int, int), CellContent> grid,
            int targetX,
            int targetY)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];



                if (!grid.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn)
                    continue;




                int newG = current.G + 1;
                int newH = ManhattanDistance(nx, ny, targetX, targetY);

                yield return new CheetahNode(
                    nx, ny,
                    newG, newH,
                    current,
                    0,
                    0, 0, null
                );
            }
        }



        public static BotCommand? EscapeZookeeper(Animal myAnimal)
        {

            // 1. Find the closest zookeeper.
            if (GAME_STATE.Zookeepers == null || GAME_STATE.Zookeepers.Count == 0)
                return null;

            Zookeeper closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                int d = ManhattanDistance(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (d < minDistance)
                {
                    minDistance = d;
                    closestZookeeper = zk;
                }
            }
            if (closestZookeeper == null)
                return null;

            // 2. Compute relative direction.
            int deltaX = myAnimal.X - closestZookeeper.X;
            int deltaY = myAnimal.Y - closestZookeeper.Y;

            int targetX = myAnimal.X;
            int targetY = myAnimal.Y;

            // Choose horizontal escape if horizontal difference is greater.
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (deltaX > 0)
                {
                    // Zookeeper is to the left; use right edge if enabled.
                    targetX = PortalRight ? MapWidth - 1 : myAnimal.X;
                }
                else
                {
                    // Zookeeper is to the right; use left edge if enabled.
                    targetX = PortalLeft ? 0 : myAnimal.X;
                }
                targetY = myAnimal.Y;
            }
            else
            {
                // Vertical escape.
                if (deltaY > 0)
                {
                    // Zookeeper is above; use bottom edge if enabled.
                    targetY = PortalDown ? MapHeight - 1 : myAnimal.Y;
                }
                else
                {
                    // Zookeeper is below; use top edge if enabled.
                    targetY = PortalUp ? 0 : myAnimal.Y;
                }
                targetX = myAnimal.X;
            }

            // 3. Find a portal cell on the chosen edge (filtering using IsPortalCell).
            Cell? portalTarget = null;


            var allPortalCellsOrdered = GAME_STATE.Cells
                    .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                    .OrderBy(c => ManhattanDistance(myAnimal.X, myAnimal.Y, c.X, c.Y))
                    .ToList();

            if (ManhattanDistance(myAnimal.X, myAnimal.Y, allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) < SAFE_DISTANCE && (ManhattanDistance(myAnimal.X, myAnimal.Y, allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) > ManhattanDistance(myAnimal.X, myAnimal.Y, closestZookeeper.X, closestZookeeper.Y)))
            {
                portalTarget = allPortalCellsOrdered.FirstOrDefault();
            }
            else if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // Horizontal escape: filter all portal cells on the target edge (left or right).
                int edgeX = (deltaX > 0 && PortalRight) ? MapWidth - 1 :
                            (deltaX <= 0 && PortalLeft) ? 0 : -1;

                if (edgeX != -1)
                {
                    portalTarget = allPortalCellsOrdered
                        .Where(c => c.X == edgeX)
                        .OrderBy(c => ManhattanDistance(myAnimal.X, myAnimal.Y, c.X, c.Y))
                        .FirstOrDefault();
                }
            }
            else
            {
                // Vertical escape: filter all portal cells on the target edge (up or down).
                int edgeY = (deltaY > 0 && PortalDown) ? MapHeight - 1 :
                            (deltaY <= 0 && PortalUp) ? 0 : -1;

                if (edgeY != -1)
                {
                    portalTarget = allPortalCellsOrdered
                        .Where(c => c.Y == edgeY)
                        .OrderBy(c => ManhattanDistance(myAnimal.X, myAnimal.Y, c.X, c.Y))
                        .FirstOrDefault();
                }
            }

            if (portalTarget == null)
            {
                //LogMessage("No valid portal target found.", ConsoleColor.Green);
                return null;
            }

            // 4. Compute a path to the portal target.
            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var path = FindPath(myAnimal.X, myAnimal.Y, portalTarget.X, portalTarget.Y);
            if (path == null || path.Count == 0)
            {
                //LogMessage("No path found to portal.", ConsoleColor.Green);
                return null;
            }



            // Find highest-scoring opponent
            var topOpponent = GAME_STATE.Animals.Where(x => x.Id != myAnimal.Id).OrderByDescending(o => o.Score).FirstOrDefault();
            double opponentScore = topOpponent?.Score ?? 0;

            //if (GAME_STAGE == GameStage.EarlyGame)
            //{
            //    LogMessage("EarlyGame: Skipping escape. Death cost is low.", ConsoleColor.Green);
            //    return null;

            //    //// Estimate the value of escaping vs. dying
            //    //int ticksToEscape = path.Count;
            //    //double averagePelletsPerTick = 0.4; // Tweak this based on testing/data
            //    //double estimatedPelletsLost = ticksToEscape * averagePelletsPerTick;
            //    //double pelletLossIfCaught = myAnimal.Score * 0.10;

            //    //LogMessage($"Escape decision -> Pellets left {PELLETS_LEFT_PERCENTAGE:F1}%, TicksToEscape: {ticksToEscape}, EstPelletsLost: {estimatedPelletsLost:F1}, OnDeathLoss: {pelletLossIfCaught:F1}", ConsoleColor.Green);

            //    //if (pelletLossIfCaught < estimatedPelletsLost)
            //    //{
            //    //    LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);
            //    //    return null; // Don't escape, let the normal logic handle it
            //    //}
            //}

            //LogMessage("EarlyGame: Skipping escape. Death cost is low.", ConsoleColor.Green);
            //return null;

            double hitLoss = myAnimal.Score * 0.10;
            double scoreLeft = PELLETS_LEFT / 11;

            if (GAME_STAGE == GameStage.MidGame)
            {
                if (myAnimal.Score < opponentScore)
                {
                    //LogMessage($"Escape decision -> Pellets left {PELLETS_LEFT_PERCENTAGE:F1}%, Hit Loss: {hitLoss:F1}, Score Left: {scoreLeft:F1}", ConsoleColor.Green);

                    if (scoreLeft > hitLoss)
                    {
                        //LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);
                        return null; // Don't escape, let the normal logic handle it
                    }
                }
                else
                {
                    if ((myAnimal.Score - hitLoss) < opponentScore)
                    {
                        //will loose the lead
                        // LogMessage($"Escape decision -> Keep the lead");
                    }
                    else
                    {
                        // LogMessage($"Escape decision -> Pellets left {PELLETS_LEFT_PERCENTAGE:F1}%, Hit Loss: {hitLoss:F1}, Score Left: {scoreLeft:F1}", ConsoleColor.Green);

                        if (scoreLeft > hitLoss)
                        {
                            // LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);

                        }
                    }
                }


            }
            else if (GAME_STAGE == GameStage.LateGame)
            {
                // LogMessage("LateGame: Always escape.", ConsoleColor.Green);
            }



            // 5. If the computed path has less than 2 nodes (we’re already at the portal),
            // force an escape move that exits the portal.
            if (path.Count < 2)
            {
                BotAction? escapeAction = null;
                // Determine the escape direction based on which edge the portal cell is on.
                if (portalTarget.X == 0 && PortalLeft)
                    escapeAction = BotAction.Left;
                else if (portalTarget.X == MapWidth - 1 && PortalRight)
                    escapeAction = BotAction.Right;
                else if (portalTarget.Y == 0 && PortalUp)
                    escapeAction = BotAction.Up;
                else if (portalTarget.Y == MapHeight - 1 && PortalDown)
                    escapeAction = BotAction.Down;

                if (escapeAction.HasValue)
                {
                    // LogMessage($"Tick: {GAME_STATE.Tick} | Escape move: {escapeAction.Value} (exiting portal)", ConsoleColor.Green);
                    return new BotCommand { Action = escapeAction.Value };
                }
                else
                {
                    //LogMessage("No escape action computed.", ConsoleColor.Green);
                    return null;
                }
            }

            // 6. Otherwise, use the normal computed path.
            PersistentPath = path;
            var action = ComputeNextMove();
            if (!action.HasValue)
            {
                //LogMessage("No action computed from escape path.", ConsoleColor.Green);
                return null;
            }

            // Remove the first node (current position) from the persistent path.
            PersistentPath.RemoveAt(0);

            //LogMessage($"Tick: {GAME_STATE.Tick} | Action: {action.Value} | EscapeZookeeper => move to portal", ConsoleColor.Green);
            return new BotCommand { Action = action.Value };
        }

        public static BotCommand? EscapeChasedZookeeper(Animal myAnimal)
        {
            // 1. Identify the closest zookeeper.
            Zookeeper? closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                int d = ManhattanDistance(myAnimal.X, myAnimal.Y, zk.X, zk.Y);
                if (d < minDistance)
                {
                    minDistance = d;
                    closestZookeeper = zk;
                }
            }
            if (closestZookeeper == null)
                return null;

            // Estimate the value of escaping vs. dying
            int ticksToEscape = 30;
            double averagePelletsPerTick = 0.4; // Tweak this based on testing/data
            double estimatedPelletsLost = ticksToEscape * averagePelletsPerTick;
            double pelletLossIfCaught = myAnimal.Score * 0.10;


            //LogMessage($"Escape decision -> TicksToEscape: {ticksToEscape}, EstPelletsLost: {estimatedPelletsLost:F1}, OnDeathLoss: {pelletLossIfCaught:F1}", ConsoleColor.Green);

            if (pelletLossIfCaught < estimatedPelletsLost)
            {
                //LogMessage("Not worth escaping - better to take the hit.", ConsoleColor.Green);
                return null; // Don't escape, let the normal logic handle it
            }



            // 2. Compute primary escape move.
            int dx = myAnimal.X - closestZookeeper.X;
            int dy = myAnimal.Y - closestZookeeper.Y;
            BotAction primaryAction = Math.Abs(dx) >= Math.Abs(dy)
                ? (dx >= 0 ? BotAction.Right : BotAction.Left)
                : (dy >= 0 ? BotAction.Down : BotAction.Up);

            int newX = myAnimal.X, newY = myAnimal.Y;
            switch (primaryAction)
            {
                case BotAction.Up:
                    newY = myAnimal.Y - 1;
                    break;
                case BotAction.Down:
                    newY = myAnimal.Y + 1;
                    break;
                case BotAction.Left:
                    newX = myAnimal.X - 1;
                    break;
                case BotAction.Right:
                    newX = myAnimal.X + 1;
                    break;
            }

            // 3. Check if primary move is valid.
            if (newX >= 0 && newX < MapWidth && newY >= 0 && newY < MapHeight)
            {
                var cell = GAME_STATE.Cells.FirstOrDefault(c => c.X == newX && c.Y == newY);
                if (cell != null && cell.Content != CellContent.Wall &&
                    cell.Content != CellContent.AnimalSpawn && cell.Content != CellContent.ZookeeperSpawn)
                {
                    //LogMessage($"Chased escape: moving {primaryAction}", ConsoleColor.Green);
                    return new BotCommand { Action = primaryAction };
                }
            }

            // 4. Fallback: Evaluate all valid directions.
            BotAction? bestAction = null;
            int bestDistance = minDistance;
            foreach (BotAction action in new[] { BotAction.Up, BotAction.Down, BotAction.Left, BotAction.Right })
            {
                int altX = myAnimal.X, altY = myAnimal.Y;
                switch (action)
                {
                    case BotAction.Up:
                        altY = myAnimal.Y - 1;
                        break;
                    case BotAction.Down:
                        altY = myAnimal.Y + 1;
                        break;
                    case BotAction.Left:
                        altX = myAnimal.X - 1;
                        break;
                    case BotAction.Right:
                        altX = myAnimal.X + 1;
                        break;
                }
                if (altX < 0 || altX >= MapWidth || altY < 0 || altY >= MapHeight)
                    continue;
                var altCell = GAME_STATE.Cells.FirstOrDefault(c => c.X == altX && c.Y == altY);
                if (altCell == null || altCell.Content == CellContent.Wall ||
                    altCell.Content == CellContent.AnimalSpawn || altCell.Content == CellContent.ZookeeperSpawn)
                    continue;
                int newDist = ManhattanDistance(altX, altY, closestZookeeper.X, closestZookeeper.Y);
                if (newDist > bestDistance)
                {
                    bestDistance = newDist;
                    bestAction = action;
                }
            }
            if (bestAction != null)
            {
                //LogMessage($"Chased escape alternative: moving {bestAction}", ConsoleColor.Green);
                return new BotCommand { Action = bestAction.Value };
            }
            return null;
        }

        #endregion


        #endregion

        #region Path Finding
        public static bool ValidationCheck()
        {

            if (PersistentTarget != null)
            {

                if (ME.X == PersistentTarget.X && ME.Y == PersistentTarget.Y)
                {
                    //GameLogger.LogInfo("Validation", "[Validation Failed] Target reached. Clearing persistent data.");
                    PersistentTarget = null;
                    BestCluster = null;
                    PersistentPath = null;
                    PersistentPathScore = null;
                    return false;
                }


                //Pellets on path 
                var currentScore = EvaluatePathScore(PersistentPath);
                if (PersistentPathScore.HasValue && currentScore < PersistentPathScore.Value)
                {
                    GameLogger.LogInfo("Validation", $"[Validation Failed] Score Drop - Predicted Score {PersistentPathScore} | Current Score {currentScore}");
                    PersistentPath = null;
                    PersistentTarget = null;
                    BestCluster = null;
                    PersistentPathScore = null;
                    return false;
                }

                // Compute the danger along the path.
                //int totalDanger = 0;
                //foreach (var node in PersistentPath)
                //{
                //    totalDanger += ComputeZookeeperPenalty(node.X, node.Y, myAnimal);

                //    //if (GAME_STAGE == GameStage.MidGame)
                //    ///{
                //       // Console.WriteLine($"Total danger is {totalDanger}");
                //    //}
                //    if (totalDanger > DANGER_THRESHOLD)
                //    {
                //        // Path is too dangerous.
                //        LogMessage($"[Validation Failed]: Path is too dangerous Total danger is {totalDanger} and danger threshold is {DANGER_THRESHOLD}", ConsoleColor.Yellow);
                //        PersistentPath = null;
                //        PersistentTarget = null;
                //        PersistentPathScore = null;
                //        return false;
                //    }

                //}

            }

            return true;
        }
        public static Cell? Plan()
        {
            ValidationCheck();

            if (PersistentTarget != null && PersistentPath.Count != 0)
            {
                PersistentPathScore = EvaluatePathScore(PersistentPath) - 1;
                GameLogger.LogInfo("Plan", $"[Persistent target] ({PersistentTarget.X}, {PersistentTarget.Y}) with a path of length {PersistentPath.Count} and score of {PersistentPathScore}");
                //LogCurrentPath(PersistentPath);
                return PersistentTarget;
            }


            if (GAME_STAGE == GameStage.EarlyGame)
            {
                var bestCluster = FindBestClusterEarlyGame();

                if (bestCluster != null && bestCluster.EntryPoint != null && bestCluster.Path != null && bestCluster.Path.Count > 0)
                {
                    BestCluster = bestCluster;
                    PersistentTarget = bestCluster.EntryPoint;
                    PersistentPath = bestCluster.Path;
                    PersistentPathScore = EvaluatePathScore(bestCluster.Path);
                    GameLogger.LogInfo("Plan", $"[New target] ({PersistentTarget.X}, {PersistentTarget.Y}) with a path of length {PersistentPath.Count} and score of {PersistentPathScore}");
                    //LogCurrentPath(PersistentPath);
                    return PersistentTarget;
                }

            }
            else if (GAME_STAGE == GameStage.MidGame)
            {
                var bestCluster = FindBestClusterMidGame();

                if (bestCluster != null && bestCluster.EntryPoint != null && bestCluster.Path != null && bestCluster.Path.Count > 0)
                {
                    BestCluster = bestCluster;
                    PersistentTarget = bestCluster.EntryPoint;
                    PersistentPath = bestCluster.Path;
                    PersistentPathScore = EvaluatePathScore(bestCluster.Path);
                    GameLogger.LogInfo("Plan", $"[New target] ({PersistentTarget.X}, {PersistentTarget.Y}) with a path of length {PersistentPath.Count} and score of {PersistentPathScore}");
                    //LogCurrentPath(PersistentPath);
                    return PersistentTarget;
                }
            }
            else if (GAME_STAGE == GameStage.LateGame)
            {
                var bestCluster = FindBestClusterLateGame();

                if (bestCluster != null && bestCluster.EntryPoint != null && bestCluster.Path != null && bestCluster.Path.Count > 0)
                {
                    BestCluster = bestCluster;
                    PersistentTarget = bestCluster.EntryPoint;
                    PersistentPath = bestCluster.Path;
                    PersistentPathScore = EvaluatePathScore(bestCluster.Path);
                    GameLogger.LogInfo("Plan", $"[New target] ({PersistentTarget.X}, {PersistentTarget.Y}) with a path of length {PersistentPath.Count} and score of {PersistentPathScore}");
                    //LogCurrentPath(PersistentPath);
                    return PersistentTarget;
                }

            }

            var lastResort = FindClosestPellet();
            if (lastResort != null)
            {
                PersistentTarget = lastResort;
                var path = FindPath(ME.X, ME.Y, lastResort.X, lastResort.Y);
                if (path != null && path.Count > 0)
                {
                    PersistentPath = path;
                    PersistentPathScore = EvaluatePathScore(path);
                    GameLogger.LogInfo("Plan", $"[Last resort target] ({PersistentTarget.X}, {PersistentTarget.Y}) with a path of length {PersistentPath.Count} and score of {PersistentPathScore}");
                    //LogCurrentPath(PersistentPath);
                    return PersistentTarget;
                }
            }


            return null;

        }


        public static float ScoreCluster(CheetahCluster cluster)
        {
            if (cluster.Cells.Count == 0)
                return 0;

            int minDist = int.MaxValue;
            int minZkDist = int.MaxValue;

            foreach (var c in cluster.Cells)
            {
                if (DISTANCE_MAP.TryGetValue((c.X, c.Y), out var d))
                    minDist = Math.Min(minDist, d);

                var zkclose = GAME_STATE.Zookeepers.OrderBy(x => GetDistanceFromZookeeper(x.Id, c.X, c.Y)).FirstOrDefault();
                int zkDist =  GetDistanceFromZookeeper(zkclose.Id,c.X, c.Y);
                minZkDist = Math.Min(minZkDist, zkDist);
            }

            if (minDist == int.MaxValue)
                return 0;

            int clusterSize = Math.Min(cluster.Cells.Count, MAX_CLUSTER_SIZE);

            // Add soft penalty for clusters near ZKs
            float zkPenalty = (minZkDist < 5) ? (5 - minZkDist) * 1 : 0;

            return (float)(clusterSize - ALPHA * minDist - zkPenalty);
        }



        //public static float ScoreCluster(CheetahCluster cluster)
        //{
        //    if (cluster.Cells.Count == 0)
        //        return 0;




        //    int clusterSize = Math.Min(cluster.Cells.Count, MAX_CLUSTER_SIZE);
        //    int dist = cluster.Cells.Min(c => ManhattanDistance(ME.X, ME.Y, c.X, c.Y));

        //    float score = (float)(clusterSize - ALPHA * dist);

        //    return score;
        //}


        //public static float ScoreCluster(CheetahCluster cluster)
        //{
        //    if (cluster.Cells.Count == 0)
        //        return 0;

        //    int sumX = 0, sumY = 0;
        //    foreach (var c in cluster.Cells)
        //    {
        //        sumX += c.X;
        //        sumY += c.Y;
        //    }
        //    int clusterSize = cluster.Cells.Count;
        //    int avgX = sumX / clusterSize;
        //    int avgY = sumY / clusterSize;
        //    int dist = Math.Abs(ME.X - avgX) + Math.Abs(ME.Y - avgY);
        //    float score = (float)(clusterSize - ALPHA * dist);


        //    //int clusterSize = cluster.Cells.Count;
        //    //int dist = cluster.Cells.Min(c => ManhattanDistance(ME.X, ME.Y, c.X, c.Y));

        //    //float score = (float)(clusterSize - ALPHA * dist);

        //    return score;
        //}

        public static BotCommand? FinaliseAction(BotCommand? command)
        {
            LastSentPosition = (ME.X, ME.Y); // Save starting position before the move is applied
            LastMove = command == null ? null : command.Action;
            LastTickCommandIssued = GAME_STATE.Tick;

            GameLogger.LogInfo("Move", $"Action: {command?.Action}");
            GameLogger.Flush(GAME_STATE.Tick);

            return command;

        }

        #endregion

        #region Early Game Cluster Based
        public static CheetahCluster FindBestClusterEarlyGame()
        {
            var pelletCells = GAME_STATE.Cells.Where(c => c.Content == CellContent.Pellet).ToList();

            if (pelletCells.Count == 0)
                return null;

            var visited = new HashSet<(int, int)>();
            var clusters = new List<CheetahCluster>();
            foreach (var pcell in pelletCells)
            {
                var pos = (pcell.X, pcell.Y);
                if (!visited.Contains(pos))
                {
                    var cluster = BFSClusterEarlyGame(pos, visited);
                    cluster.Score = ScoreCluster(cluster);
                    clusters.Add(cluster);
                }
            }

            CheetahCluster bestCluster = clusters.OrderByDescending(c => c.Score).FirstOrDefault();

            if (bestCluster == null || bestCluster.Cells.Count == 0)
                return null;


            var clusterPellets = bestCluster.Cells
                .OrderBy(p => GetDistanceFromMe(p.X, p.Y))
                .Take(MAX_PELLET_CANDIDATES)
                .ToList();

            Cell? bestPellet = null;
            List<CheetahNode>? bestPath = null;
            double bestScore = double.NegativeInfinity;

            foreach (var pellet in clusterPellets)
            {
                // Run your existing A* search (FindPath) to get a path from your current location to the pellet.
                var path = FindPath(ME.X, ME.Y, pellet.X, pellet.Y);
                if (path == null || path.Count == 0)
                    continue;

                // Count how many pellets the path passes through.
                int pelletCountOnPath = CountPelletsOnPath(path);

                int pathLength = path.Count;
                // Compute the score: reward higher pellet count and penalize longer paths.
                double score = pelletCountOnPath - (ALPHA * pathLength);

                // 5. If this candidate's score is the best so far, keep it.
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPellet = pellet;
                    bestPath = path;
                }
            }

            bestCluster.EntryPoint = bestPellet;
            bestCluster.Path = bestPath;


            if (bestCluster.EntryPoint == null)
            {
                // No pellets found in the cluster.
                return null;
            }

            var clusterSet = bestCluster.Cells.Select(c => (c.X, c.Y)).ToHashSet();
            var longestPath = FindLongestPelletPathInCluster((bestCluster.EntryPoint.X, bestCluster.EntryPoint.Y), clusterSet);

            var fullPath = new List<CheetahNode>();
            fullPath.AddRange(bestCluster.Path);


            if (longestPath != null && longestPath.Count > 0)
            {

                if (longestPath.Count > 0 && bestCluster.Path.Last().X == longestPath[0].X && bestCluster.Path.Last().Y == longestPath[0].Y)
                {
                    longestPath.RemoveAt(0);
                }

                fullPath.AddRange(longestPath);
            }

            CheetahNode finalPellet = fullPath.Last();

            var newTarget = new Cell { X = finalPellet.X, Y = finalPellet.Y, Content = CellContent.Pellet };

            return new CheetahCluster()
            {
                EntryPoint = newTarget,
                Path = fullPath,
                Cells = bestCluster.Cells
            };






            //return bestCluster;





        }
        public static CheetahCluster BFSClusterEarlyGame((int x, int y) start, HashSet<(int, int)> visited)
        {
            var cluster = new List<Cell>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(new Cell { X = current.x, Y = current.y, Content = CellContent.Pellet });

                if (cluster.Count >= MAX_CLUSTER_PELLETS_LIMIT)
                {
                    // we’ve hit our limit—no need to search further
                    break;
                }



                int[,] dirs = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
                for (int i = 0; i < dirs.GetLength(0); i++)
                {
                    int nx = current.x + dirs[i, 0];
                    int ny = current.y + dirs[i, 1];
                    var npos = (nx, ny);
                    if (!visited.Contains(npos) && GRID.TryGetValue(npos, out var content))
                    {
                        if (content == CellContent.Pellet)
                        {
                            visited.Add(npos);
                            queue.Enqueue(npos);
                        }
                    }
                }
            }

            var result = new CheetahCluster()
            {
                Cells = cluster,
                Score = 0,
                EntryPoint = null
            };

            return result;
        }


        #endregion

        #region Mid Game Path Through Cluster

        public static CheetahCluster FindBestClusterMidGame()
        {

            var pelletCells = GAME_STATE.Cells.Where(c => c.Content == CellContent.Pellet && !CONTESTED_CELLS_MAP.Contains(new(c.X, c.Y)) && !CORRIDOR_CELLS_MAP.Contains(new(c.X, c.Y))).ToList();

            if (pelletCells.Count == 0)
                return null;

            var visited = new HashSet<(int, int)>();
            var clusters = new List<CheetahCluster>();
            foreach (var pcell in pelletCells)
            {
                var pos = (pcell.X, pcell.Y);
                if (!visited.Contains(pos))
                {
                    var cluster = BFSClusterMidGame(pos, visited);
                    cluster.Score = ScoreCluster(cluster);
                    clusters.Add(cluster);
                }
            }

            CheetahCluster bestCluster = clusters.OrderByDescending(c => c.Score).FirstOrDefault();

            if (bestCluster == null || bestCluster.Cells.Count == 0)
                return null;


            var clusterPellets = bestCluster.Cells
                .OrderBy(p => GetDistanceFromMe(p.X, p.Y))
                .Take(MAX_PELLET_CANDIDATES)
                .ToList();

            Cell? bestPellet = null;
            List<CheetahNode>? bestPath = null;
            double bestScore = double.NegativeInfinity;

            foreach (var pellet in clusterPellets)
            {
                // Run your existing A* search (FindPath) to get a path from your current location to the pellet.
                var path = FindPath(ME.X, ME.Y, pellet.X, pellet.Y);
                if (path == null || path.Count == 0)
                    continue;

                // Count how many pellets the path passes through.
                int pelletCountOnPath = CountPelletsOnPath(path);

                int pathLength = path.Count;
                // Compute the score: reward higher pellet count and penalize longer paths.
                double score = pelletCountOnPath - (ALPHA * pathLength);

                // 5. If this candidate's score is the best so far, keep it.
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPellet = pellet;
                    bestPath = path;
                }
            }

            bestCluster.EntryPoint = bestPellet;
            bestCluster.Path = bestPath;

            if (bestCluster.EntryPoint == null)
            {
                // No pellets found in the cluster.
                return null;
            }

            var clusterSet = bestCluster.Cells.Select(c => (c.X, c.Y)).ToHashSet();
            var longestPath = FindLongestPelletPathInCluster((bestCluster.EntryPoint.X, bestCluster.EntryPoint.Y), clusterSet);

            var fullPath = new List<CheetahNode>();
            fullPath.AddRange(bestCluster.Path);


            if (longestPath != null && longestPath.Count > 0)
            {

                if (longestPath.Count > 0 && bestCluster.Path.Last().X == longestPath[0].X && bestCluster.Path.Last().Y == longestPath[0].Y)
                {
                    longestPath.RemoveAt(0);
                }

                fullPath.AddRange(longestPath);
            }

            CheetahNode finalPellet = fullPath.Last();

            var newTarget = new Cell { X = finalPellet.X, Y = finalPellet.Y, Content = CellContent.Pellet };

            return new CheetahCluster()
            {
                EntryPoint = newTarget,
                Path = fullPath,
                Cells = bestCluster.Cells
            };
        }
        public static CheetahCluster BFSClusterMidGame((int x, int y) start, HashSet<(int, int)> visited)
        {
            var cluster = new List<Cell>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(new Cell { X = current.x, Y = current.y, Content = CellContent.Pellet });
                int[,] dirs = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
                for (int i = 0; i < dirs.GetLength(0); i++)
                {
                    int nx = current.x + dirs[i, 0];
                    int ny = current.y + dirs[i, 1];
                    var npos = (nx, ny);
                    if (!visited.Contains(npos) && GRID.TryGetValue(npos, out var content))
                    {
                        if (content == CellContent.Pellet && !CONTESTED_CELLS_MAP.Contains(npos) && !CORRIDOR_CELLS_MAP.Contains(npos))
                        {
                            visited.Add(npos);
                            queue.Enqueue(npos);
                        }
                    }
                }
            }

            var result = new CheetahCluster()
            {
                Cells = cluster,
                Score = 0,
                EntryPoint = null
            };

            return result;
        }
        public static List<CheetahNode> FindLongestPelletPathInCluster((int x, int y) start, HashSet<(int, int)> clusterSet) // ✅ Tune this externally
        {
            var bestPath = new List<CheetahNode>();
            var visited = new HashSet<(int, int)>();

            void DFS((int x, int y) current, List<CheetahNode> currentPath)
            {
                visited.Add(current);
                currentPath.Add(new CheetahNode(current.x, current.y, 0, 0, null, 0, 0, 0, null));

                // ✅ Always track the longest path seen so far
                if (currentPath.Count > bestPath.Count)
                    bestPath = new List<CheetahNode>(currentPath);

                // ✅ Early return if max depth reached — no deeper search
                if (currentPath.Count >= DFSDEPTH)
                {
                    visited.Remove(current);
                    currentPath.RemoveAt(currentPath.Count - 1);
                    return;
                }

                var neighbors = new (int x, int y)[]
                {
                    (current.x, current.y - 1),
                    (current.x, current.y + 1),
                    (current.x - 1, current.y),
                    (current.x + 1, current.y),
                };

                foreach (var neighbor in neighbors)
                {
                    if (clusterSet.Contains(neighbor) && !visited.Contains(neighbor) && !CONTESTED_CELLS_MAP.Contains(neighbor) && !CORRIDOR_CELLS_MAP.Contains(neighbor) && IsSafeFromZookeeper(neighbor))
                    {
                        DFS(neighbor, currentPath);
                    }
                }

                // ✅ Backtrack
                visited.Remove(current);
                currentPath.RemoveAt(currentPath.Count - 1);
            }

            DFS(start, new List<CheetahNode>());

            return bestPath;
        }

        private static bool IsSafeFromZookeeper((int x, int y) cell)
        {
            int minZkDist = int.MaxValue;
            foreach (var zkMap in ZOOKEEPER_DISTANCE_MAPS_BY_ID.Values)
            {
                if (zkMap.TryGetValue(cell, out int dist))
                    minZkDist = Math.Min(minZkDist, dist);
            }

            return minZkDist > 2; // Avoid tiles that are ≤ 2 steps away from a ZK
        }


        #endregion

        #region Late Game Cluster Based
        public static CheetahCluster FindBestClusterLateGame()
        {
            var pelletCells = GAME_STATE.Cells.Where(c => c.Content == CellContent.Pellet && !CONTESTED_CELLS_MAP.Contains(new(c.X, c.Y)) && !CORRIDOR_CELLS_MAP.Contains(new(c.X, c.Y))).ToList();

            if (pelletCells.Count == 0)
                return null;

            var visited = new HashSet<(int, int)>();
            var clusters = new List<CheetahCluster>();
            foreach (var pcell in pelletCells)
            {
                var pos = (pcell.X, pcell.Y);
                if (!visited.Contains(pos))
                {
                    var cluster = BFSClusterLateGame(pos, visited);
                    cluster.Score = ScoreCluster(cluster);
                    clusters.Add(cluster);
                }
            }

            CheetahCluster bestCluster = clusters.OrderByDescending(c => c.Score).FirstOrDefault();

            if (bestCluster == null || bestCluster.Cells.Count == 0)
                return null;


            var clusterPellets = bestCluster.Cells
                .OrderBy(p => GetDistanceFromMe(p.X, p.Y))
                .Take(MAX_PELLET_CANDIDATES)
                .ToList();

            Cell? bestPellet = null;
            List<CheetahNode>? bestPath = null;
            double bestScore = double.NegativeInfinity;

            foreach (var pellet in clusterPellets)
            {
                // Run your existing A* search (FindPath) to get a path from your current location to the pellet.
                var path = FindPath(ME.X, ME.Y, pellet.X, pellet.Y);
                if (path == null || path.Count == 0)
                    continue;

                // Count how many pellets the path passes through.
                int pelletCountOnPath = CountPelletsOnPath(path);

                int pathLength = path.Count;
                // Compute the score: reward higher pellet count and penalize longer paths.
                double score = pelletCountOnPath - (ALPHA * pathLength);

                // 5. If this candidate's score is the best so far, keep it.
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPellet = pellet;
                    bestPath = path;
                }
            }

            bestCluster.EntryPoint = bestPellet;
            bestCluster.Path = bestPath;
            return bestCluster;
        }
        public static CheetahCluster BFSClusterLateGame((int x, int y) start, HashSet<(int, int)> visited)
        {
            var cluster = new List<Cell>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(new Cell { X = current.x, Y = current.y, Content = CellContent.Pellet });
                int[,] dirs = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
                for (int i = 0; i < dirs.GetLength(0); i++)
                {
                    int nx = current.x + dirs[i, 0];
                    int ny = current.y + dirs[i, 1];
                    var npos = (nx, ny);
                    if (!visited.Contains(npos) && GRID.TryGetValue(npos, out var content))
                    {
                        if (content == CellContent.Pellet && !CONTESTED_CELLS_MAP.Contains(npos) && !CORRIDOR_CELLS_MAP.Contains(npos))
                        {
                            visited.Add(npos);
                            queue.Enqueue(npos);
                        }
                    }
                }
            }

            var result = new CheetahCluster()
            {
                Cells = cluster,
                Score = 0,
                EntryPoint = null
            };

            return result;
        }

        #endregion

        #region Path 

        public static List<CheetahNode>? FindPath(int startX, int startY, int targetX, int targetY, bool ignorePortals = false)
        {
            // ─── data-structures ──────────────────────────────────────────────────────
            var open = new PriorityQueue<CheetahNode, int>();               // min-heap by F
            var openDict = new Dictionary<(int, int), CheetahNode>();           // coord → best node
            var closedSet = new HashSet<(int, int)>();                               // expanded

            // ─── seed ────────────────────────────────────────────────────────────────
            var startNode = new CheetahNode(
                startX, startY,
                g: 0,
                h: GetDistanceFromMe(targetX, targetY),
                parent: null,
                tieBreak: ComputeTieBreaker(startX, startY, MinDistanceToAnyKeeper(startX, startY), 0),
                blankStreak: 0,
                pelletStreak: 0,
                dirFromParent: LastMove);

            open.Enqueue(startNode, startNode.F);
            openDict[(startX, startY)] = startNode;

            // ─── main loop ───────────────────────────────────────────────────────────
            while (open.TryDequeue(out var current, out _))
            {
                // node may be stale (see tomb-stone trick) – skip if we’ve already found a better G
                if (closedSet.Contains((current.X, current.Y)) || current.G == int.MaxValue)
                    continue;

                openDict.Remove((current.X, current.Y));
                closedSet.Add((current.X, current.Y));

                // ─── goal reached ────────────────────────────────────────────────────
                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current);

                // ─── neighbours ──────────────────────────────────────────────────────
                foreach (var neighbour in FindPathNeighbors(current, targetX, targetY, ignorePortals))
                {
                    var key = (neighbour.X, neighbour.Y);

                    if (closedSet.Contains(key)) continue;

                    // ─── existing node for this tile? ────────────────────────────────────
                    if (openDict.TryGetValue(key, out var existing))
                    {
                        // if we already have a better (≦ G) path, ignore the new one
                        if (existing.G <= neighbour.G) continue;

                        // ► we found a *better* path: tomb-stone the old node *and*
                        //   DROP the stale reference from the dictionary
                        existing.G = int.MaxValue;          // mark as stale for PQ
                        openDict.Remove(key);               // ← NEW: prevent leak
                    }

                    // insert fresh node
                    open.Enqueue(neighbour, neighbour.F);
                    openDict[key] = neighbour;              // single live entry per tile
                }
            }

            // ─── no path ─────────────────────────────────────────────────────────────
            return null;
        }
        public static IEnumerable<CheetahNode> FindPathNeighbors(CheetahNode current, int targetX, int targetY, bool ignorePortals = false)
        {
            // (dx, dy, heading)
            var dirs = new (int dx, int dy, BotAction dir)[]
            {
                ( 0, -1, BotAction.Up   ),
                ( 0,  1, BotAction.Down ),
                (-1,  0, BotAction.Left ),
                ( 1,  0, BotAction.Right)
            };

            foreach (var (dx, dy, dir) in dirs)
            {
                int nx = current.X + dx;
                int ny = current.Y + dy;

                // ─── portal wrapping ────────────────────────────────────────────────
                if (!ignorePortals && PortalEnabled)
                {
                    if (nx < 0)
                    {
                        if (PortalLeft && current.X == 0) nx = MapWidth - 1;
                        else continue;
                    }
                    else if (nx >= MapWidth)
                    {
                        if (PortalRight && current.X == MapWidth - 1) nx = 0;
                        else continue;
                    }

                    if (ny < 0)
                    {
                        if (PortalUp && current.Y == 0) ny = MapHeight - 1;
                        else continue;
                    }
                    else if (ny >= MapHeight)
                    {
                        if (PortalDown && current.Y == MapHeight - 1) ny = 0;
                        else continue;
                    }
                }

                // ─── tile walkability ──────────────────────────────────────────────
                if (!GRID.TryGetValue((nx, ny), out var content)) continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn) continue;

                //--------------------------------------------------------------------
                // 1.  tracking  (blank-streak, G)
                //--------------------------------------------------------------------
                bool isPellet = content == CellContent.Pellet;
                int nextBlank = isPellet ? 0 : current.BlankStreak + 1;
                int nextStreak = isPellet ? current.PelletStreak + 1 : 0;   // ← NEW
                int newG = current.G + 1;

                //--------------------------------------------------------------------
                // 2.  bonus / penalty
                //--------------------------------------------------------------------
                int bonus = FindPathBonus(new CheetahNode(nx, ny, newG, 0, current, 0, nextBlank, nextStreak, null));
                int penalty = FindPathPenalty(current, new CheetahNode(nx, ny, newG, 0, current, 0, nextBlank, nextStreak, null));

                //--------------------------------------------------------------------
                // 3.  heuristic
                //--------------------------------------------------------------------
                int baseH = GetDistanceFromMe(targetX, targetY);
                int newH = Math.Max(0, baseH + penalty - bonus);            // clamp ≥0

                //--------------------------------------------------------------------
                // 4.  tie-breaker
                //--------------------------------------------------------------------
                int keeperDist = MinDistanceToAnyKeeper(nx, ny);            // helper below
                int turnPenalty = (current.DirFromParent.HasValue && current.DirFromParent.Value != dir) ? 1 : 0;
                int tie = ComputeTieBreaker(nx, ny, keeperDist, turnPenalty);

                //--------------------------------------------------------------------
                // 5.  yield neighbour
                //--------------------------------------------------------------------
                yield return new CheetahNode(
                    x: nx,
                    y: ny,
                    g: newG,
                    h: newH,
                    parent: current,
                    tieBreak: tie,
                    blankStreak: nextBlank,
                    pelletStreak: nextStreak,
                    dirFromParent: dir);
            }
        }

        public static int FindPathBonus(CheetahNode node)
        {
            int bonus = 0;

            if (GRID.TryGetValue((node.X, node.Y), out var content) &&
                content == CellContent.Pellet)
            {
                // 1 pellet base + extra for being the N-th pellet in a combo
                bonus += PELLET_SCORE + node.PelletStreak * STREAK_WEIGHT;
            }

            // little reward for “still inside the combo window”
            if (node.BlankStreak < SCORE_STREAK_RESET)
                bonus += SCORE_STREAK;

            return bonus;
        }
        public static int FindPathPenalty(CheetahNode current, CheetahNode neighbor)
        {
            int penalty = 0;

            //Reversing to parent
            if (current.Parent != null && neighbor.X == current.Parent.X && neighbor.Y == current.Parent.Y)
            {
                penalty += REVERSING_TO_PARENT_PENALTY;
            }

            //Visited cell penalty
            if (VISITED_CELLS_MAP.Contains((neighbor.X, neighbor.Y)))
            {
                penalty += VISITED_CELL_PENALTY;
            }

            //Contested cell penalty
            if (CONTESTED_CELLS_MAP.Contains((neighbor.X, neighbor.Y)))
            {
                penalty += ComputeContestedPenatly(neighbor.X, neighbor.Y);
            }

            //Corridor cell penalty
            if (CORRIDOR_CELLS_MAP.Contains((neighbor.X, neighbor.Y)))
            {
                penalty += CORRIDOR_CELL_PENALTY;
            }

            //Contested cell penalty
            if (CONTESTED_CELLS_MAP.Contains((neighbor.X, neighbor.Y)))
            {
                penalty += ComputeContestedPenatly(neighbor.X, neighbor.Y);
            }

            //penalty += ComputeZookeeperPenalty(neighbor.X, neighbor.Y);

            return penalty;
        }

        public static int ComputeContestedPenatly(int nx, int ny)
        {
            bool otherAnimalColser = false;
            int additonPenalty = 0;
            if (CONTESTED_CELLS_MAP.Contains((nx, ny)))
            {
                int myDist = ManhattanDistance(ME.X, ME.Y, nx, ny);
                foreach (var opp in GAME_STATE.Animals)
                {
                    if (opp.Id == ME.Id || opp.IsViable == false)
                    {
                        continue;
                    }

                    int oppDist = ManhattanDistance(opp.X, opp.Y, nx, ny);

                    if (oppDist < myDist)
                    {
                        otherAnimalColser = true;

                    }

                    if (otherAnimalColser)
                    {
                        additonPenalty += CONTESTED_CELL_PENALTY;
                    }
                }
            }

            return additonPenalty;
        }
        public static int ComputeZookeeperPenalty(int nx, int ny)
        {
            int penalty = 0;
            if (GAME_STATE.Zookeepers != null)
            {
                foreach (var zk in GAME_STATE.Zookeepers)
                {
                    int dist = GetDistanceFromMe(zk.X, zk.Y);
                    // Avoid division by zero and scale penalty relative to risk (current score * 0.1)
                    penalty += (int)(ZOOKEEPER_AVOIDANCE_FACTOR * (ME.Score * 0.1) / Math.Max(dist, 1));
                }
            }
            return penalty;
        }

        #endregion

        #region Distance

        public static int GetDistanceFromMe(int tx, int ty)
        {
            if (DISTANCE_MAP.TryGetValue((tx, ty), out var d))
            {
                return d;
            }
            else
            {
                return ManhattanDistance(ME.X, ME.Y, tx, ty);
            }
        }
        public static int GetDistanceFromZookeeper(Guid zkId, int tx, int ty)
        {
            if (ZOOKEEPER_DISTANCE_MAPS_BY_ID.TryGetValue(zkId, out var map) &&
                map.TryGetValue((tx, ty), out var dist))
            {
                return dist;
            }

            return int.MaxValue;
        }

        public static int ManhattanDistance(int x, int y, int tx, int ty)
        {
            return Math.Abs(x - tx) + Math.Abs(y - ty);
        }
        public static int AstarDistance(int startX, int startY, int targetX, int targetY)
        {
            var openSet = new List<CheetahNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new CheetahNode(
                startX,
                startY,
                0,
                ManhattanDistance(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY, MinDistanceToAnyKeeper(startX, startY), 0),
                0,
                0,
                null
            );
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    int cmp = a.F.CompareTo(b.F);
                    if (cmp == 0)
                    {
                        cmp = a.H.CompareTo(b.H);
                        if (cmp == 0)
                        {
                            cmp = a.TieBreak.CompareTo(b.TieBreak);
                        }
                    }
                    return cmp;
                });


                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current).Count;

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in GetNeighborsClean(current, targetX, targetY))
                {
                    if (closedSet.Contains((neighbor.X, neighbor.Y)))
                        continue;

                    var existing = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existing != null && existing.G <= neighbor.G)
                        continue;

                    openSet.Add(neighbor);
                }
            }
            return int.MaxValue;
        }
        //public static int ComputeTieBreaker(int x, int y)
        //{
        //    // Compute Manhattan distance to the nearest zookeeper; higher distance means safer.
        //    if (GAME_STATE.Zookeepers == null || GAME_STATE.Zookeepers.Count == 0)
        //        return 0;
        //    int minDistance = int.MaxValue;
        //    foreach (var zk in GAME_STATE.Zookeepers)
        //    {
        //        int d = ManhattanDistance(x, y, zk.X, zk.Y);
        //        if (d < minDistance)
        //            minDistance = d;
        //    }
        //    // Use the negative so that a larger distance (safer) yields a lower tie-breaker value.
        //    return -minDistance;
        //}
        // prefer safer tiles (farther from keepers)     ↓ bigger  = safer
        // prefer moves that KEEP the same heading       ↓ 0 turn, 1 turn
        public static int ComputeTieBreaker(int x, int y,
                                    int keeperDist,
                                    int turnPenalty)
        {
            int keeperComponent = -keeperDist;                    // safer better
            int turnComponent = turnPenalty;                    // 0 straight / 1 turn
            int heatComponent = -(PELLET_HEAT_MAP.TryGetValue((x, y), out var h) ? h : 0);
            // more heat ⇒ more negative ⇒ preferred

            return keeperComponent + turnComponent + heatComponent;
        }

        public static List<CheetahNode> ReconstructPath(CheetahNode node)
        {
            var path = new List<CheetahNode>();
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }
        public static IEnumerable<CheetahNode> GetNeighborsClean(CheetahNode current, int targetX, int targetY)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];

                // Wrap horizontally
                if (nx < 0)
                {
                    if (PortalLeft) nx = MapWidth - 1;
                    else continue;
                }
                else if (nx >= MapWidth)
                {
                    if (PortalRight) nx = 0;
                    else continue;
                }

                // Wrap vertically
                if (ny < 0)
                {
                    if (PortalUp) ny = MapHeight - 1;
                    else continue;
                }
                else if (ny >= MapHeight)
                {
                    if (PortalDown) ny = 0;
                    else continue;
                }
                //}

                if (!GRID.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                    continue;


                int newG = current.G + 1;

                int newH = ManhattanDistance(nx, ny, targetX, targetY);

                yield return new CheetahNode(
                    nx,
                    ny,
                    newG,
                    newH,
                    current,
                    ComputeTieBreaker(nx, ny, MinDistanceToAnyKeeper(nx, ny), 0),
                    0,
                    0,
                    null
                );
            }
        }
        #endregion

        #region Utility


        public static double EvaluatePathScore(List<CheetahNode> path)
        {
            int pellets = CountPelletsOnPath(path);
            return pellets;
        }
        public static int CountPelletsOnPath(List<CheetahNode> path)
        {
            int count = 0;
            foreach (var node in path)
            {
                //var npos = (node.X, node.Y);
                if (GRID.TryGetValue((node.X, node.Y), out var content))
                {
                    if (content == CellContent.Pellet)
                    {
                        bool otherAnimalColser = false;
                        if (CONTESTED_CELLS_MAP.Contains((node.X, node.Y)))
                        {
                            int myDist = ManhattanDistance(ME.X, ME.Y, node.X, node.Y);
                            foreach (var opp in GAME_STATE.Animals)
                            {
                                if (opp.Id == ME.Id || opp.IsViable == false)
                                {
                                    continue;
                                }

                                int oppDist = ManhattanDistance(opp.X, opp.Y, node.X, node.Y);

                                if (oppDist < myDist)
                                {
                                    otherAnimalColser = true;

                                }

                                if (!otherAnimalColser)
                                {
                                    count++;

                                }
                            }
                        }
                        else
                        {
                            count++;
                        }




                    }

                }
            }
            return count;
        }
        public static BotAction? ComputeNextMove()
        {
            if (PersistentPath.Count == 0)
            {
                GameLogger.LogError("Move", "Path count is zero");
                return null;
            }

            if (PersistentPath.Count < 2)
            {
                GameLogger.LogError("Move", "Path is too short");
                return null;
            }

            var firstStep = PersistentPath[0];
            var secondStep = PersistentPath[1];
            var action = GetDirection(firstStep.X, firstStep.Y, secondStep.X, secondStep.Y);

            return action;
        }
        public static BotAction? GetDirection(int currentX, int currentY, int nextX, int nextY)
        {

            //0 38
            if (currentX == 0 && nextX == MapWidth - 1) return BotAction.Left;
            if (currentX == MapWidth - 1 && nextX == 0) return BotAction.Right;

            if (currentY == 0 && nextY == MapHeight - 1) return BotAction.Up;
            if (currentY == MapHeight - 1 && nextY == 0) return BotAction.Down;



            if (nextX > currentX) return BotAction.Right;
            if (nextX < currentX) return BotAction.Left;
            if (nextY > currentY) return BotAction.Down;
            if (nextY < currentY) return BotAction.Up;
            return null;
        }
        private static (int X, int Y) GetExpectedPosition((int X, int Y) start, BotAction move)
        {
            int newX = start.X;
            int newY = start.Y;
            switch (move)
            {
                case BotAction.Up:
                    newY--;
                    break;
                case BotAction.Down:
                    newY++;
                    break;
                case BotAction.Left:
                    newX--;
                    break;
                case BotAction.Right:
                    newX++;
                    break;
            }
            // Handle portal wrapping if enabled
            if (newX < 0 && PortalLeft)
                newX = MapWidth - 1;
            else if (newX >= MapWidth && PortalRight)
                newX = 0;
            if (newY < 0 && PortalUp)
                newY = MapHeight - 1;
            else if (newY >= MapHeight && PortalDown)
                newY = 0;
            return (newX, newY);
        }
        private static int MinDistanceToAnyKeeper(int x, int y)
        {
            if (GAME_STATE.Zookeepers.Count == 0) return 999;
            int best = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
                best = Math.Min(best, ManhattanDistance(x, y, zk.X, zk.Y));
            return best;
        }


        public static bool IsNearPortalCell(int distance = 1)
        {
            var portals = GetAllPortals();

            if (portals.Any())
            {
                foreach (var portal in portals)
                {
                    var dist = ManhattanDistance(ME.X, ME.Y, portal.X, portal.Y);
                    if (dist <= distance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public static List<Cell> GetAllPortals()
        {
            int width = GAME_STATE.Cells.Max(c => c.X) + 1;
            int height = GAME_STATE.Cells.Max(c => c.Y) + 1;
            return GAME_STATE.Cells.Where(c => (c.X == 0 || c.X == width - 1 || c.Y == 0 || c.Y == height - 1) && c.Content != CellContent.Wall).ToList();
        }
        public static bool IsPortalCell(Cell cell)
        {
            // A cell is on a portal if it is on any edge of the map.
            return (cell.X == 0 || cell.X == MapWidth - 1 || cell.Y == 0 || cell.Y == MapHeight - 1);
        }
        //public static bool HasPortals()
        //{
        //    // Compute the map dimensions from the cells.
        //    int width = GAME_STATE.Cells.Max(c => c.X) + 1;
        //    int height = GAME_STATE.Cells.Max(c => c.Y) + 1;

        //    // A cell is considered passable if it is not a wall.
        //    // (You may need to adjust this depending on your game's rules.)
        //    bool leftEdgeOpen = GAME_STATE.Cells.Any(c => c.X == 0 && c.Content != CellContent.Wall);
        //    bool rightEdgeOpen = GAME_STATE.Cells.Any(c => c.X == width - 1 && c.Content != CellContent.Wall);
        //    bool topEdgeOpen = GAME_STATE.Cells.Any(c => c.Y == 0 && c.Content != CellContent.Wall);
        //    bool bottomEdgeOpen = GAME_STATE.Cells.Any(c => c.Y == height - 1 && c.Content != CellContent.Wall);

        //    // We consider portals available if either the horizontal or vertical edges are open.
        //    return (leftEdgeOpen && rightEdgeOpen) || (topEdgeOpen && bottomEdgeOpen);
        //}
        public static bool IsSafeToLeaveSpawn()
        {
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                // Compute Manhattan distance between animal and zookeeper
                int distance = GetDistanceFromMe(zk.X, zk.Y);
                if (distance < SAFE_DISTANCE)
                {
                    GameLogger.LogWatch("RespawnEvent", $"Not safe to leave spawn zoo keeper {zk.Id} distance {distance}");
                    return false;
                }

                GameLogger.LogWatch("RespawnEvent", $"Safe to leave spawn zoo keeper {zk.Id} distance {distance}");
            }

            return true;
        }


        #endregion

        #region Path Finding - Opponent

        public static Cell? FindPelletOponent(Animal opponent)
        {
            return GAME_STATE.Cells.Where(c => c.Content == CellContent.Pellet).OrderBy(c => ManhattanDistance(opponent.X, opponent.Y, c.X, c.Y)).FirstOrDefault();
        }
        public static Cell? FindClosestPellet()
        {
            return GAME_STATE.Cells.Where(c => c.Content == CellContent.Pellet).OrderBy(c => ManhattanDistance(ME.X, ME.Y, c.X, c.Y)).FirstOrDefault();
        }
        public static List<CheetahNode>? FindPathOpponent(int startX, int startY, int targetX, int targetY)
        {
            var openSet = new List<CheetahNode>();
            var closedSet = new HashSet<(int, int)>();

            var startNode = new CheetahNode(
                startX,
                startY,
                0,
                ManhattanDistance(startX, startY, targetX, targetY),
                null,
                ComputeTieBreaker(startX, startY, MinDistanceToAnyKeeper(startX, startY), 0),
                0,
                0,
                null
            );
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    int cmp = a.F.CompareTo(b.F);
                    if (cmp == 0)
                    {
                        cmp = a.H.CompareTo(b.H);
                        if (cmp == 0)
                        {
                            cmp = a.TieBreak.CompareTo(b.TieBreak);
                        }
                    }
                    return cmp;
                });


                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.X == targetX && current.Y == targetY)
                    return ReconstructPath(current);

                closedSet.Add((current.X, current.Y));

                foreach (var neighbor in FindPathNeighborsOpponent(current, targetX, targetY))
                {
                    if (closedSet.Contains((neighbor.X, neighbor.Y)))
                        continue;

                    var existing = openSet.FirstOrDefault(n => n.X == neighbor.X && n.Y == neighbor.Y);
                    if (existing != null && existing.G <= neighbor.G)
                        continue;

                    openSet.Add(neighbor);
                }
            }
            return null;
        }
        public static IEnumerable<CheetahNode> FindPathNeighborsOpponent(CheetahNode current, int targetX, int targetY)
        {
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int nx = current.X + directions[i, 0];
                int ny = current.Y + directions[i, 1];

                // ─── handle wrapping on each border ───
                if (nx < 0)
                {
                    // only wrap if current cell is a left-edge portal
                    if (PortalLeft && current.X == 0)
                        nx = MapWidth - 1;
                    else
                        continue;
                }
                else if (nx >= MapWidth)
                {
                    if (PortalRight && current.X == MapWidth - 1)
                        nx = 0;
                    else
                        continue;
                }

                if (ny < 0)
                {
                    if (PortalUp && current.Y == 0)
                        ny = MapHeight - 1;
                    else
                        continue;
                }
                else if (ny >= MapHeight)
                {
                    if (PortalDown && current.Y == MapHeight - 1)
                        ny = 0;
                    else
                        continue;
                }


                if (!GRID.TryGetValue((nx, ny), out var content))
                    continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn)
                    continue;

                var neighbor = new CheetahNode(
                    nx,
                    ny,
                    0,
                    0,
                    current,
                    ComputeTieBreaker(nx, ny, MinDistanceToAnyKeeper(nx, ny), 0),
                    0,
                    0,
                    null
                );





                int newG = current.G + 1;

                int baseH = ManhattanDistance(nx, ny, targetX, targetY);
                int newH = baseH;


                yield return new CheetahNode(
                    nx,
                    ny,
                    newG,
                    newH,
                    current,
                    ComputeTieBreaker(nx, ny, MinDistanceToAnyKeeper(nx, ny), 0),
                    0,
                    0,
                    null
                );
            }
        }



        #endregion

        #region Path Finding - Zookeepers

        private static readonly Dictionary<Guid, CheetahZookeeperTracker> _zkCache = new(); // NEW
        private static int TICKS_BETWEEN_ZK_RETARGET = 20;                    // NEW – overwrite at tick 1

        private static bool IsZookeeperNear()
        {
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                if (SAFETY_NET_MAP.Contains((zk.X, zk.Y)))
                {
                    GameLogger.LogWatch("ZookeeperEvent", $"Zookeeper {zk.Id} is in safety net at ({zk.X}, {zk.Y})");
                    return true;
                }
            }

            return false;
        }

        /// <summary>How many engine steps until keeper <paramref name="zkId"/> reaches ME.</summary>
        private static int KeeperStepsToMe(Guid zkId)
        {
            var zk = GAME_STATE.Zookeepers.First(z => z.Id == zkId);
            if (!_zkCache.TryGetValue(zkId, out var trk))
                _zkCache[zkId] = trk = new CheetahZookeeperTracker();

            // retarget cadence identical to engine
            if (trk.Target == null ||
                !trk.Target.IsViable ||
                trk.TicksSinceCalc >= TICKS_BETWEEN_ZK_RETARGET)
            {
                trk.Target = PickTargetAnimalForKeeper(zk);
                trk.Path = null;
                trk.TicksSinceCalc = 0;
            }

            if (trk.Target == null) return int.MaxValue; // idle keeper

            if (trk.Path == null)
                trk.Path = PathToAnimal(zk, trk.Target);

            trk.TicksSinceCalc++;
            return trk.Path?.Count ?? int.MaxValue;
        }

        private static Animal? PickTargetAnimalForKeeper(Zookeeper zk)
        {
            var viable = GAME_STATE.Animals.Where(a => a.IsViable).ToList();
            if (viable.Count == 0) return null;
            if (viable.Count == 1) return viable[0];

            var dist = viable.ToDictionary(a => a,
                a => Math.Abs(a.X - zk.X) + Math.Abs(a.Y - zk.Y));
            var ordered = viable.OrderBy(a => dist[a]).ToList();
            var best = ordered[0];
            if (ordered.Count > 1 && dist[ordered[1]] == dist[best])
                return null; // tie – no target
            return best;
        }

        private static List<CheetahNode>? PathToAnimal(Zookeeper zk, Animal tgt)
            => FindPathKeeper(zk.X, zk.Y, tgt.X, tgt.Y);

        // ---------------------------------------------------------------- A*
        private static List<CheetahNode>? FindPathKeeper(int sx, int sy, int tx, int ty)
        {
            var open = new PriorityQueue<CheetahNode, int>();
            var best = new Dictionary<(int, int), int>();

            var start = new CheetahNode(sx, sy, 0,
                ManhattanDistance(sx, sy, tx, ty), null, 0, 0, 0, null);
            open.Enqueue(start, start.F);
            best[(sx, sy)] = 0;

            while (open.TryDequeue(out var cur, out _))
            {
                if (cur.X == tx && cur.Y == ty)
                    return ReconstructPath(cur);

                foreach (var nb in KeeperNeighbours(cur, tx, ty))
                {
                    var key = (nb.X, nb.Y);
                    if (best.TryGetValue(key, out var g) && g <= nb.G) continue;
                    best[key] = nb.G;
                    open.Enqueue(nb, nb.F);
                }
            }
            return null;
        }

        private static IEnumerable<CheetahNode> KeeperNeighbours(CheetahNode n, int tx, int ty)
        {
            foreach (var (dx, dy) in new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                var (nx, ny) = Warp(n.X + dx, n.Y + dy);
                if (nx < 0 || nx >= MapWidth || ny < 0 || ny >= MapHeight) continue; // fell off (blocked portal)
                if (!GRID.TryGetValue((nx, ny), out var c) || c == CellContent.Wall) continue;
                yield return new CheetahNode(nx, ny, n.G + 1,
                    ManhattanDistance(nx, ny, tx, ty), n, 0, 0, 0, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int, int) Warp(int x, int y)
        {
            if (x < 0) x = PortalLeft ? MapWidth - 1 : -1;
            else if (x >= MapWidth) x = PortalRight ? 0 : MapWidth;
            if (y < 0) y = PortalUp ? MapHeight - 1 : -1;
            else if (y >= MapHeight) y = PortalDown ? 0 : MapHeight;
            return (x, y);
        }

        public static BotCommand? EscapeViaPortal()
        {

            // 1. Find the closest zookeeper.
            if (GAME_STATE.Zookeepers == null || GAME_STATE.Zookeepers.Count == 0)
                return null;

            Zookeeper closestZookeeper = null;
            int minDistance = int.MaxValue;
            foreach (var zk in GAME_STATE.Zookeepers)
            {
                int d = GetDistanceFromMe(zk.X, zk.Y);
                if (d < minDistance)
                {
                    minDistance = d;
                    closestZookeeper = zk;
                }
            }
            if (closestZookeeper == null)
                return null;

            // 2. Compute relative direction.
            int deltaX = ME.X - closestZookeeper.X;
            int deltaY = ME.Y - closestZookeeper.Y;

            int targetX = ME.X;
            int targetY = ME.Y;

            // Choose horizontal escape if horizontal difference is greater.
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                if (deltaX > 0)
                {
                    // Zookeeper is to the left; use right edge if enabled.
                    targetX = PortalRight ? MapWidth - 1 : ME.X;
                }
                else
                {
                    // Zookeeper is to the right; use left edge if enabled.
                    targetX = PortalLeft ? 0 : ME.X;
                }
                targetY = ME.Y;
            }
            else
            {
                // Vertical escape.
                if (deltaY > 0)
                {
                    // Zookeeper is above; use bottom edge if enabled.
                    targetY = PortalDown ? MapHeight - 1 : ME.Y;
                }
                else
                {
                    // Zookeeper is below; use top edge if enabled.
                    targetY = PortalUp ? 0 : ME.Y;
                }
                targetX = ME.X;
            }

            // 3. Find a portal cell on the chosen edge (filtering using IsPortalCell).
            Cell? portalTarget = null;


            var allPortalCellsOrdered = GAME_STATE.Cells
                    .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                    .OrderBy(c => GetDistanceFromMe(c.X, c.Y))
                    .ToList();

            if (GetDistanceFromMe(allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) < SAFE_DISTANCE && (GetDistanceFromMe(allPortalCellsOrdered.FirstOrDefault().X, allPortalCellsOrdered.FirstOrDefault().Y) > GetDistanceFromMe(closestZookeeper.X, closestZookeeper.Y)))
            {
                portalTarget = allPortalCellsOrdered.FirstOrDefault();
            }
            else if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // Horizontal escape: filter all portal cells on the target edge (left or right).
                int edgeX = (deltaX > 0 && PortalRight) ? MapWidth - 1 :
                            (deltaX <= 0 && PortalLeft) ? 0 : -1;

                if (edgeX != -1)
                {
                    portalTarget = allPortalCellsOrdered
                        .Where(c => c.X == edgeX)
                        .OrderBy(c => GetDistanceFromMe(c.X, c.Y))
                        .FirstOrDefault();
                }
            }
            else
            {
                // Vertical escape: filter all portal cells on the target edge (up or down).
                int edgeY = (deltaY > 0 && PortalDown) ? MapHeight - 1 :
                            (deltaY <= 0 && PortalUp) ? 0 : -1;

                if (edgeY != -1)
                {
                    portalTarget = allPortalCellsOrdered
                        .Where(c => c.Y == edgeY)
                        .OrderBy(c => GetDistanceFromMe(c.X, c.Y))
                        .FirstOrDefault();
                }
            }

            if (portalTarget == null)
            {
                GameLogger.LogWatch("EscapeViaPortal", "No valid portal target found.");
                return null;
            }

            // 4. Compute a path to the portal target.
            var grid = GAME_STATE.Cells.ToDictionary(c => (c.X, c.Y), c => c.Content);
            var path = FindPath(ME.X, ME.Y, portalTarget.X, portalTarget.Y, true);


            if (path == null || path.Count == 0)
            {
                GameLogger.LogWatch("EscapeViaPortal", "No path found to portal.");
                return null;
            }

            GameLogger.LogWatch("EscapeViaPortal", $"Path to portal: {string.Join(" -> ", path.Select(n => $"({n.X}, {n.Y})"))}");


            // Find highest-scoring opponent
            var topOpponent = GAME_STATE.Animals.Where(x => x.Id != ME.Id).OrderByDescending(o => o.Score).FirstOrDefault();
            double opponentScore = topOpponent?.Score ?? 0;


            double hitLoss = ME.Score * 0.10;
            double scoreLeft = PELLETS_LEFT / 11;

            if (GAME_STAGE == GameStage.MidGame)
            {
                if (ME.Score < opponentScore)
                {
                    GameLogger.LogWatch("EscapeViaPortal", $"Escape decision -> Pellets left {PELLETS_LEFT_PERCENTAGE:F1}%, Hit Loss: {hitLoss:F1}, Score Left: {scoreLeft:F1}");

                    if (scoreLeft > hitLoss)
                    {
                        GameLogger.LogWatch("EscapeViaPortal", "Not worth escaping - better to take the hit.");
                        return null; // Don't escape, let the normal logic handle it
                    }
                }
                else
                {
                    if ((ME.Score - hitLoss) < opponentScore)
                    {
                        //will loose the lead
                        GameLogger.LogWatch("EscapeViaPortal", $"Escape decision -> Keep the lead");
                    }
                    else
                    {
                        GameLogger.LogWatch("EscapeViaPortal", $"Escape decision -> Pellets left {PELLETS_LEFT_PERCENTAGE:F1}%, Hit Loss: {hitLoss:F1}, Score Left: {scoreLeft:F1}");


                        if (scoreLeft > hitLoss)
                        {
                            GameLogger.LogWatch("EscapeViaPortal", "Not worth escaping - better to take the hit.");
                        }
                    }
                }


            }
            else if (GAME_STAGE == GameStage.LateGame)
            {
                GameLogger.LogWatch("EscapeViaPortal", "LateGame: Always escape.");
            }



            // 5. If the computed path has less than 2 nodes (we’re already at the portal),
            // force an escape move that exits the portal.
            if (path.Count < 2)
            {
                BotAction? escapeAction = null;
                // Determine the escape direction based on which edge the portal cell is on.
                if (portalTarget.X == 0 && PortalLeft)
                    escapeAction = BotAction.Left;
                else if (portalTarget.X == MapWidth - 1 && PortalRight)
                    escapeAction = BotAction.Right;
                else if (portalTarget.Y == 0 && PortalUp)
                    escapeAction = BotAction.Up;
                else if (portalTarget.Y == MapHeight - 1 && PortalDown)
                    escapeAction = BotAction.Down;

                if (escapeAction.HasValue)
                {
                    GameLogger.LogWatch("EscapeViaPortal", $"Tick: {GAME_STATE.Tick} | Escape move: {escapeAction.Value} (exiting portal)");
                    return new BotCommand { Action = escapeAction.Value };
                }
                else
                {
                    GameLogger.LogWatch("EscapeViaPortal", "No escape action computed.");
                    return null;
                }
            }

            // 6. Otherwise, use the normal computed path.
            PersistentPath = path;
            var action = ComputeNextMove();

            if (!action.HasValue)
            {
                GameLogger.LogWatch("EscapeViaPortal", "No action computed from escape path.");
                return null;
            }

            // Remove the first node (current position) from the persistent path.
            PersistentPath.RemoveAt(0);

            return new BotCommand { Action = action.Value };
        }



        /// <summary>
        /// Detects whether two predicted paths collide
        /// (same-tile or cross-pass) before they end.
        /// </summary>
        private static bool PathsIntersect(List<CheetahNode> pathA, List<CheetahNode> pathB)
        {
            if (pathA == null || pathB == null) return false;

            int limit = Math.Min(pathA.Count, pathB.Count);

            for (int t = 0; t < limit; t++)
            {
                // Head-on: stand on the same tile in the same tick.
                if (pathA[t].X == pathB[t].X && pathA[t].Y == pathB[t].Y)
                    return true;

                // Cross-pass: swap tiles between consecutive ticks.
                if (t + 1 < pathA.Count && t + 1 < pathB.Count &&
                    pathA[t].X == pathB[t + 1].X && pathA[t].Y == pathB[t + 1].Y &&
                    pathA[t + 1].X == pathB[t].X && pathA[t + 1].Y == pathB[t].Y)
                    return true;
            }

            return false;
        }

        public static BotCommand? EscapeViaPortalMultiZK()
        {
            if (GAME_STATE.Zookeepers == null || GAME_STATE.Zookeepers.Count == 0)
                return null;

            // Step 1: Precompute all zookeeper paths to ME
            var zkPathsToMe = new Dictionary<Guid, List<CheetahNode>>();

            var zookeepersNearMe = GAME_STATE.Zookeepers
                .Where(zk => GetDistanceFromMe(zk.X, zk.Y) <= 30)
                .ToList();


            foreach (var zk in zookeepersNearMe)
            {
                var zkPath = FindPathKeeper(zk.X, zk.Y, ME.X, ME.Y);
                if (zkPath != null)
                    zkPathsToMe[zk.Id] = zkPath;
            }

            // Step 2: Find all valid portal cells
            var portalCandidates = GAME_STATE.Cells
                .Where(c => IsPortalCell(c) && c.Content != CellContent.Wall)
                .OrderBy(c => GetDistanceFromMe(c.X, c.Y))
                .ToList();

            foreach (var portal in portalCandidates)
            {
                int myDist = GetDistanceFromMe(portal.X, portal.Y);
                bool anyZkCloser = zookeepersNearMe.Any(zk => GetDistanceFromZookeeper(zk.Id, portal.X, portal.Y) < myDist);
                if (anyZkCloser) continue;

                var myPath = FindPath(ME.X, ME.Y, portal.X, portal.Y, true);
                if (myPath == null || myPath.Count == 0) continue;

                // Step 3: Check for intersection with any zk path
                bool intersects = zkPathsToMe.Values.Any(zkPath => PathsIntersect(myPath, zkPath));
                if (intersects) continue;

                // VALID ESCAPE
                if (myPath.Count < 2)
                {
                    return EscapeOutOfPortal(portal);
                }

                PersistentPath = myPath;
                var action = ComputeNextMove();
                if (!action.HasValue) return null;
                PersistentPath.RemoveAt(0);
                return new BotCommand { Action = action.Value };
            }

            GameLogger.LogWatch("EscapeViaPortal", "No safe portal escape found.");

            // No portals worked — use failsafe escape
            GameLogger.LogWatch("EscapeViaPortal", "Attempting failsafe escape from zookeepers");

            var fallbackAction = ComputeFailsafeEscapeFromZookeepers();
            if (fallbackAction.HasValue)
            {
                GameLogger.LogWatch("EscapeViaPortal", $"Failsafe escape action: {fallbackAction.Value}");
                return new BotCommand { Action = fallbackAction.Value };
            }


            return null;
        }

        private static BotCommand? EscapeOutOfPortal(Cell portal)
        {
            BotAction? escapeAction = null;
            if (portal.X == 0 && PortalLeft)
                escapeAction = BotAction.Left;
            else if (portal.X == MapWidth - 1 && PortalRight)
                escapeAction = BotAction.Right;
            else if (portal.Y == 0 && PortalUp)
                escapeAction = BotAction.Up;
            else if (portal.Y == MapHeight - 1 && PortalDown)
                escapeAction = BotAction.Down;

            if (escapeAction.HasValue)
            {
                GameLogger.LogWatch("EscapeViaPortal", $"Tick: {GAME_STATE.Tick} | Escape move: {escapeAction.Value} (exiting portal)");
                PortalEnabled = false;
                PortalsLockedUntilTick = GAME_STATE.Tick + PORTAL_COOLDOWN;
                return new BotCommand { Action = escapeAction.Value };
            }

            GameLogger.LogWatch("EscapeViaPortal", "No escape action computed.");
            return null;
        }

        public static BotAction? ComputeFailsafeEscapeFromZookeepers()
        {
            var directions = new (int dx, int dy, BotAction action)[]
            {
            (0, -1, BotAction.Up),
            (0, 1, BotAction.Down),
            (-1, 0, BotAction.Left),
            (1, 0, BotAction.Right)
            };

            BotAction? bestAction = null;
            int maxMinDist = int.MinValue;

            foreach (var (dx, dy, action) in directions)
            {
                var (nx, ny) = Warp(ME.X + dx, ME.Y + dy);

                // Check if tile is walkable
                if (!GRID.TryGetValue((nx, ny), out var content)) continue;
                if (content == CellContent.Wall ||
                    content == CellContent.AnimalSpawn ||
                    content == CellContent.ZookeeperSpawn) continue;

                // Measure danger: lowest distance from any zookeeper to this tile
                int minDist = int.MaxValue;
                foreach (var zkMap in ZOOKEEPER_DISTANCE_MAPS_BY_ID.Values)
                {
                    if (zkMap.TryGetValue((nx, ny), out int zkDist))
                        minDist = Math.Min(minDist, zkDist);
                }

                // Pick the direction that gives maximum distance from closest zookeeper
                if (minDist > maxMinDist)
                {
                    maxMinDist = minDist;
                    bestAction = action;
                }
            }

            return bestAction;
        }



        #endregion



    }

    public class CheetahNode
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int G;
        public int H;
        public int F => G + H;
        public int TieBreak { get; }
        public CheetahNode? Parent;
        public int BlankStreak;
        public int PelletStreak;
        public BotAction? DirFromParent { get; }

        public CheetahNode(int x, int y, int g, int h, CheetahNode? parent, int tieBreak, int blankStreak, int pelletStreak, BotAction? dirFromParent)
        {
            X = x;
            Y = y;
            G = g;
            H = h;
            TieBreak = tieBreak;
            Parent = parent;
            BlankStreak = blankStreak;
            PelletStreak = pelletStreak;
            DirFromParent = dirFromParent;
        }

    }

    public class CheetahCluster
    {
        public List<Cell> Cells { get; set; }
        public float Score { get; set; }
        public Cell EntryPoint { get; set; }
        public List<CheetahNode>? Path { get; set; }
    }

    public sealed class CheetahZookeeperTracker
    {
        public Animal? Target;
        public List<CheetahNode>? Path;
        public int TicksSinceCalc;
    }

}
