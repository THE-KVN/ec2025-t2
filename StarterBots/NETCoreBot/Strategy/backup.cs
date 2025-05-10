//// Use the combined cluster + multi-pellet method to get a candidate.
//using NETCoreBot.Models;

//if (GAME_STAGE == GameStage.EarlyGame)
//{
//    var singlePellet = SinglePelletPlanEG(myAnimal, GAME_STATE.Cells, MAX_PELLET_CANDIDATES);

//    if (singlePellet != null)
//    {
//        LogMessage($"[SinglePellet] Selected target: ({singlePellet.X}, {singlePellet.Y})");
//        PersistentTarget = singlePellet;
//        PersistentPath = null;
//        return singlePellet;
//    }

//    else
//    {
//        var (candidatePellet, candidatePath) = FindBestClusterMultiPelletPath(myAnimal);
//        if (candidatePellet != null && candidatePath != null)
//        {
//            LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y}) with a path of length {candidatePath.Count}");
//            PersistentTarget = candidatePellet;
//            PersistentPath = candidatePath;
//            PersistentPathScore = EvaluatePathScore(candidatePath);
//            return candidatePellet;
//        }
//    }
//}

//if (GAME_STAGE == GameStage.MidGame)
//{

//    var (candidatePellet, candidatePath) = FindBestClusterMultiPelletPath(myAnimal);
//    if (candidatePellet != null && candidatePath != null)
//    {
//        LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y}) with a path of length {candidatePath.Count}");
//        PersistentTarget = candidatePellet;
//        PersistentPath = candidatePath;
//        PersistentPathScore = EvaluatePathScore(candidatePath);
//        return candidatePellet;
//    }

//    //var wasteLandPellet = FindWastelandPellet(GAME_STATE.Cells, myAnimal);

//    //if (wasteLandPellet != null)
//    //{
//    //    LogMessage($"[WastelandPellet] Selected target: ({wasteLandPellet.X}, {wasteLandPellet.Y})", ConsoleColor.Cyan);
//    //    PersistentTarget = wasteLandPellet;
//    //    PersistentPath = null;
//    //    return wasteLandPellet;
//    //}
//    //else
//    //{
//    //    var (candidatePellet, candidatePath) = FindBestClusterMultiPelletPath(myAnimal);
//    //    if (candidatePellet != null && candidatePath != null)
//    //    {
//    //        LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y}) with a path of length {candidatePath.Count}");
//    //        PersistentTarget = candidatePellet;
//    //        PersistentPath = candidatePath;
//    //        PersistentPathScore = EvaluatePathScore(candidatePath);
//    //        return candidatePellet;
//    //    }
//    //}


//    //OLD SCHOOL
//    //var candidatePellet = FindPelletInLargestConnectedClusterWeighted_Old_School(myAnimal, GAME_STATE.Cells,ALPHA);
//    //    if (candidatePellet != null)
//    //    {
//    //        LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y})");
//    //        PersistentTarget = candidatePellet;
//    //        PersistentPath = null;
//    //        return candidatePellet;
//    //    }


//    //var wasteLandPellet = FindWastelandPellet(GAME_STATE.Cells, myAnimal);

//    //if (wasteLandPellet != null)
//    //{
//    //    LogMessage($"[WastelandPellet] Selected target: ({wasteLandPellet.X}, {wasteLandPellet.Y})", ConsoleColor.Cyan);
//    //    PersistentTarget = wasteLandPellet;
//    //    PersistentPath = null;
//    //    return wasteLandPellet;
//    //}
//    //else
//    //{
//    //    var candidatePellet = FindPelletInLargestConnectedClusterWeighted_Old_School(myAnimal, GAME_STATE.Cells, ALPHA);
//    //    if (candidatePellet != null)
//    //    {
//    //        LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y})");
//    //        PersistentTarget = candidatePellet;
//    //        PersistentPath = null;
//    //        return candidatePellet;
//    //    }
//    //}
//}
//else if (GAME_STAGE == GameStage.LateGame)
//{

//    ////old school
//    var wasteLandPellet = FindWastelandPellet(GAME_STATE.Cells, myAnimal);
//    if (wasteLandPellet != null)
//    {
//        LogMessage($"[WastelandPellet] Selected target: ({wasteLandPellet.X}, {wasteLandPellet.Y})", ConsoleColor.Cyan);
//        PersistentTarget = wasteLandPellet;
//        PersistentPath = null;
//        return wasteLandPellet;
//    }
//    else
//    {
//        var candidatePellet = FindPelletInLargestConnectedClusterWeighted_Old_School(myAnimal, GAME_STATE.Cells, ALPHA);
//        if (candidatePellet != null)
//        {
//            LogMessage($"[ClusterMultiPellet] Selected target: ({candidatePellet.X}, {candidatePellet.Y})");
//            PersistentTarget = candidatePellet;
//            PersistentPath = null;
//        }
//    }
//}