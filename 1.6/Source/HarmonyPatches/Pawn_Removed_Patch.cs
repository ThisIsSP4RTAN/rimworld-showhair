using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowHair.HarmonyPatches;

    [HarmonyPatch]
    internal static class Pawn_Destroy_ClearCache_Patch
    {
        [HarmonyTargetMethod]
        static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn), nameof(Pawn.Destroy));
        }

        [HarmonyPostfix]
        static void Postfix(Pawn __instance)
        {
            if (__instance == null) return;
            Utils.pawnCache.TryRemove(__instance.thingIDNumber, out _);
        }
    }