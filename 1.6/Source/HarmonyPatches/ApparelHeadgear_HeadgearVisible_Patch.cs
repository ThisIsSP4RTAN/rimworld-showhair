using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ShowHair.HarmonyPatches
{
    [HarmonyPatch]
    internal static class ApparelHeadgear_HeadgearVisible_Patch
    {
        private static readonly List<MethodBase> Targets = new List<MethodBase>();

        private static readonly string[] CandidateTypes =
        {
            "RimWorld.PawnRenderNodeWorker_Apparel_Headgear",
            "Verse.PawnRenderNodeWorker_Apparel_Headgear",
            "RimWorld.PawnRenderNodeWorker_Apparel_Head",
            "Verse.PawnRenderNodeWorker_Apparel_Head"
        };

        [HarmonyPrepare]
        public static bool Prepare()
        {
            Targets.Clear();
            foreach (var tn in CandidateTypes)
            {
                var t = AccessTools.TypeByName(tn);
                if (t == null) continue;

                var m = AccessTools.Method(t, "HeadgearVisible", new Type[] { typeof(PawnDrawParms) });
                if (m != null) Targets.Add(m);
            }
            return Targets.Count > 0;
        }

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods() => Targets;

        [HarmonyPostfix]
        public static void Postfix(ref bool __result, PawnDrawParms parms)
        {
            try
            {
                if (__result) return;
                var pawn = parms.pawn;
                if (pawn == null || pawn.apparel == null) return;

                bool inBed;
                try { inBed = pawn.InBed(); }
                catch { inBed = (pawn.CurrentBed() != null) || (pawn.CurJobDef == JobDefOf.LayDown); }
                if (!inBed) return;

                var worn = pawn.apparel.WornApparel;
                if (worn == null || worn.Count == 0) return;

                CacheEntry ce;
                if (!Utils.pawnCache.TryGetValue(pawn.thingIDNumber, out ce)) return;
                if (!ce.hatStateParms.HasValue) return;

                ulong flags = ce.hatStateParms.Value.flags;

                for (int i = 0; i < worn.Count; i++)
                {
                    var app = worn[i];
                    if (app == null) continue;

                    // Make def non-null explicitly to satisfy the analyzer
                    var def = app.def;
                    if (def == null) continue;

                    var props = def.apparel;
                    if (props == null || !props.IsHeadwear()) continue;

                    // Use a non-null local for the call
                    ThingDef hatDef = def;
                    if (ShowHairMod.Settings.GetHatState(flags, hatDef) != HatEnum.HideHat)
                    {
                        __result = true;
                        return;
                    }
                }
            }
            catch
            {
                // fail-safe
            }
        }
    }
}