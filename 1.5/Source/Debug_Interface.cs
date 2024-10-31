using System;
using LudeonTK;
using RimWorld;
using Verse;


namespace RimworldExploration.Debug
{
    public static class RimworldExploration_DebugTools
    {
        [DebugAction("Exploration Mode", "Toggle Reveal World", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
        private static void WorldReveal()
        {
            if (!VisibilityManager.revealAll)
            {
                VisibilityManager.revealAll = true;
            }
            else
            {
                VisibilityManager.revealAll = false;
            }
            VisibilityManager.CheckAllTiles();
            VisibilityManager.UpdateGraphics();
        }
    }
}