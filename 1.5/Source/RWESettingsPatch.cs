using UnityEngine;
using Verse;
using RimworldExploration;

namespace RimworldExplorationMode.Settings
{
    public class RWEMode_Settings : ModSettings
    {
        public bool RevealInitialMap;
        public bool DisableFogOfWar;
            
        public override void ExposeData()
        {
            Scribe_Values.Look(ref RevealInitialMap, "RevealInitialMapTemp", false);
            Scribe_Values.Look(ref DisableFogOfWar, "DisableFogOfWar", false);
            base.ExposeData();
        }
    }

    public class RWEMod : Mod
    {
        RWEMode_Settings settings;
            
        public RWEMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RWEMode_Settings>();
        }
            
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled(Translator.Translate("RWE_InitRevealWorldSetting"), 
                ref settings.RevealInitialMap, Translator.Translate("RWE_InitRevealWorldSetting_ToolTip"));
            listingStandard.CheckboxLabeled(Translator.Translate("RWE_DisableFogOfWarSetting"), 
                ref settings.DisableFogOfWar);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return Translator.Translate("RWE_Settings");
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            if (Current.ProgramState == ProgramState.Playing)
            {
                VisibilityManager.MassCheckTile();
                VisibilityManager.SetUpdateType(TileUpdateType.Full);
                VisibilityManager.UpdateGraphics();
            }
        }
    }
}