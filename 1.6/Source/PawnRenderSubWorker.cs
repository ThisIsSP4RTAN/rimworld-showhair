using System.Diagnostics.CodeAnalysis;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair
{
    internal class PawnRenderSubWorkerHair : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            if (ShowHairMod.Settings.onlyApplyToColonists && !parms.pawn.IsColonist)
                return true;

            var story = parms.pawn.story;
            var hairDef = story != null ? story.hairDef : null;
            var hairUI = ShowHairMod.Settings.HairSelectorUI;

            if (hairDef == null || hairUI == null || hairUI.enabledDefs == null || hairUI.enabledDefs.Count == 0)
                return true;

            if (!hairUI.enabledDefs.Contains(hairDef))
                return true;

            var tracker = parms.pawn.apparel;
            var worn = tracker != null ? tracker.WornApparel : null;
            if (worn == null || worn.Count == 0)
                return true;

            CacheEntry ce;
            ulong flags = 0UL;
            bool haveFlags = Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out ce) && ce.hatStateParms.HasValue;
            if (haveFlags)
            {
                HatStateParms hsp = ce.hatStateParms.GetValueOrDefault();
                flags = hsp.flags;
            }

            for (int i = 0; i < worn.Count; i++)
            {
                var app = worn[i];
                if (app == null) continue;

                var def = app.def;
                if (def == null) continue;

                var props = def.apparel;
                if (props == null || !props.IsHeadwear()) continue;

                if (!haveFlags)
                {
                    return true;
                }

                ThingDef hatDef = def;
                var state = ShowHairMod.Settings.GetHatState(flags, hatDef);
                if (state != HatEnum.HideHat)
                {
                    return false;
                }
            }

            return true;
        }

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

                var def = app.def;
                if (def == null) continue;

                var apparelDef = def.apparel;
                if (apparelDef == null) continue;

                var groups = apparelDef.bodyPartGroups;
                if (groups == null) continue;

                if (groups.Contains(BodyPartGroupDefOf.FullHead))
                {
                    hat = app;
                    coversFullHead = true;
                    return true;
                }
            }

            for (int i = 0; i < worn.Count; i++)
            {
                var app = worn[i];
                if (app == null) continue;

                var def = app.def;
                if (def == null) continue;

                var apparelDef = def.apparel;
                if (apparelDef == null) continue;

                var groups = apparelDef.bodyPartGroups;
                if (groups == null) continue;

                if (groups.Contains(BodyPartGroupDefOf.UpperHead))
                {
                    hat = app;
                    coversFullHead = false;
                    return true;
                }
            }

            return false;
        }

        internal class PawnRenderSubWorkerHat : PawnRenderSubWorker
        {
            public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
            {
                CacheEntry cacheEntry;
                if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out cacheEntry) ||
                    !cacheEntry.hatStateParms.HasValue) return true;

                HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
                if (!hatStateParms.enabled || node.apparel == null) return true;

                return ShowHairMod.Settings.GetHatState(hatStateParms.flags, node.apparel.def) != HatEnum.HideHat;
            }
        }
    }
}