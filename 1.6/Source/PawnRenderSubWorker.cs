using System.Diagnostics.CodeAnalysis;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair
{
    internal class PawnRenderSubWorkerHair : PawnRenderSubWorker
    {
        public override void EditMaterial(PawnRenderNode node, PawnDrawParms parms, ref Material material)
        {
            if (!ShowHairMod.Settings.useDontShaveHead) return;

            CacheEntry cacheEntry;
            if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out cacheEntry)) return;
            if (!cacheEntry.hatStateParms.HasValue) return;

            Apparel? hat;
            bool coversFullHead;
            if (!TryGetCurrentHeadgear(parms.pawn, out hat, out coversFullHead)) return;
            var hatNN = hat!;

            HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
            if (!hatStateParms.enabled) return;

            HatEnum state = ShowHairMod.Settings.GetHatState(hatStateParms.flags, hatNN.def);
            if (!(state == HatEnum.ShowsHair || state == HatEnum.ShowsHairHidesBeard)) return;

            if (!ShowHairMod.Settings.GetHatDontShaveHead(hatStateParms.flags, hatNN.def)) return;

            if (cacheEntry.upperGraphic == null && cacheEntry.fullGraphic == null)
            {
                var story = parms.pawn.story;
                if (story != null && story.hairDef != null)
                {
                    story.hairDef.GraphicFor(parms.pawn, story.hairColor);
                }
            }

            Verse.Graphic? g = coversFullHead ? cacheEntry.fullGraphic : cacheEntry.upperGraphic;
            if (g == null) return;

            material = g.MatAt(parms.facing);
        }

        private static bool TryGetCurrentHeadgear(
            Pawn pawn,
            [NotNullWhen(true)] out Apparel? hat,
            out bool coversFullHead)
        {
            hat = null;
            coversFullHead = false;

            var tracker = pawn.apparel;
            var worn = tracker != null ? tracker.WornApparel : null;
            if (worn == null || worn.Count == 0) return false;

            for (int i = 0; i < worn.Count; i++)
            {
                var app = worn[i];
                if (app == null) continue;
                Apparel appNN = app;

                var def = appNN.def;
                if (def == null) continue;

                var apparelDef = def.apparel;
                if (apparelDef == null) continue;

                var groups = apparelDef.bodyPartGroups;
                if (groups == null) continue;

                if (groups.Contains(BodyPartGroupDefOf.FullHead))
                {
                    hat = appNN;
                    coversFullHead = true;
                    return true;
                }
            }

            for (int i = 0; i < worn.Count; i++)
            {
                var app = worn[i];
                if (app == null) continue;
                Apparel appNN = app;

                var def = appNN.def;
                if (def == null) continue;

                var apparelDef = def.apparel;
                if (apparelDef == null) continue;

                var groups = apparelDef.bodyPartGroups;
                if (groups == null) continue;

                if (groups.Contains(BodyPartGroupDefOf.UpperHead))
                {
                    hat = appNN;
                    coversFullHead = false;
                    return true;
                }
            }

            return false;
        }
    }

    internal class PawnRenderSubWorkerHat : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            CacheEntry cacheEntry;
            if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out cacheEntry)) return true;
            if (!cacheEntry.hatStateParms.HasValue) return true;
            if (node.apparel == null) return true;

            HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
            if (!hatStateParms.enabled) return true;

            return ShowHairMod.Settings.GetHatState(hatStateParms.flags, node.apparel.def) != HatEnum.HideHat;
        }
    }

    internal class PawnRenderSubWorkerBeard : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            CacheEntry cacheEntry;
            if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out cacheEntry)) return true;
            if (!cacheEntry.hatStateParms.HasValue) return true;

            HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
            if (!hatStateParms.enabled) return true;

            var tracker = parms.pawn.apparel;
            var worn = tracker != null ? tracker.WornApparel : null;
            if (worn == null || worn.Count == 0) return true;

            bool? decision = null;

            for (int i = 0; i < worn.Count; i++)
            {
                var app = worn[i];
                if (app == null) continue;
                Apparel appNN = app;

                var def = appNN.def;
                if (def == null) continue;

                var apparelDef = def.apparel;
                if (apparelDef == null) continue;
                if (!apparelDef.IsHeadwear()) continue;

                var state = ShowHairMod.Settings.GetHatState(hatStateParms.flags, def);
                switch (state)
                {
                    case HatEnum.HidesAllHair:
                        decision = false;
                        break;
                    case HatEnum.ShowsHairHidesBeard:
                        decision = false;
                        break;
                    case HatEnum.HidesHairShowsBeard:
                        decision = true;
                        break;
                }
            }

            return decision ?? true;
        }
    }
}