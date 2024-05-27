using System;
using LudeonTK;
using RimWorld;
using Verse;


namespace RimworldExploration.Debug
{
    public static class RimworldExploration_DebugTools
    {
        [DebugAction("Exploration Mode", "toggle world reveal", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
        private static void WorldReveal()
        {
            VisibilityManager.revealAll = true;
            VisibilityManager.RevealWorld();
            VisibilityManager.UpdateGraphics();
        }
    }
}