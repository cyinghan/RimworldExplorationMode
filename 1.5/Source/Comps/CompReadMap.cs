using Verse;
using System;
using RimWorld;

namespace RimworldExploration
{
    public class ReadMapComp : CompUsable
    {
        public ReadMapCompProperties compProperties_ReadMap => (ReadMapCompProperties) props;

        protected override string FloatMenuOptionLabel(Pawn pawn)
        {
            return string.Format(Translator.Translate("RWE_ReadMapAction"));
        }
    }
    
    public class ReadMapCompProperties : CompProperties_Usable
    {
        public int size;
        public int locations = 1;

        public ReadMapCompProperties()
        {
            compClass = typeof(ReadMapComp);
            useJob = new JobDef();
            useJob.driverClass = typeof(JobDriver_ReadMap);
        }
        
        public ReadMapCompProperties(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}
