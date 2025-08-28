using Verse;

namespace ShowHair
{
    public class PawnRenderSubWorkerBeard : PawnRenderSubWorker
    {
        public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
        {
            CacheEntry cacheEntry;
            if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out cacheEntry)) return true;
            if (!cacheEntry.hatStateParms.HasValue) return true;

            var hatStateParms = cacheEntry.hatStateParms.Value;
            if (!hatStateParms.enabled) return true;

            var tracker = parms.pawn.apparel;
            var worn = tracker != null ? tracker.WornApparel : null;
            if (worn == null || worn.Count == 0) return true;

            bool? decision = null;

            for (int i = 0; i < worn.Count; i++)
            {
                var app = worn[i];
                if (app == null) continue;

                var def = app.def;
                if (def == null) continue;

                var props = def.apparel;
                if (props == null || !props.IsHeadwear()) continue;

                ThingDef hatDef = def;
                switch (ShowHairMod.Settings.GetHatState(hatStateParms.flags, hatDef))
                {
                    case HatEnum.HidesAllHair:
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