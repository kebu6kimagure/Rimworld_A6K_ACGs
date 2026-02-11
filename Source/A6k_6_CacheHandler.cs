using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace A6k_CompVariant
{
    public static class A6k_CacheHandler
    {
        public static void ClearStyleDiversityCache()
        {
            var type = GenTypes.GetTypeInAnyAssembly("RimWorld.StyleItemUtility");
            if (type == null) return;
            Clear(type, "wornApparelDict");
            Clear(type, "wornApparelStyleDict");
        }

        private static void Clear(Type util, string fieldName)
        {
            var fi = AccessTools.Field(util, fieldName);
            (fi?.GetValue(null) as IDictionary)?.Clear();
        }
    }

    [HarmonyPatch]
    public static class Patch_ClearStyleDiversityCache
    {
        public static MethodBase TargetMethod()
        {
            var t = GenTypes.GetTypeInAnyAssembly("RimWorld.StyleItemUtility");
            if (t == null)
            {
                Log.Warning("[A6k_CompVariant] RimWorld.StyleItemUtility type not found, skipping Patch_ClearStyleDiversityCache.");
                return null;
            }
            var m = AccessTools.Method(t, "WornByAnyoneOfIdeology", new Type[] { typeof(ThingDef), AccessTools.TypeByName("RimWorld.Ideo") });
            if (m != null) return m;
            m = AccessTools.Method(t, "WornByAnyoneOfStyleItem", new Type[] { typeof(ThingDef), AccessTools.TypeByName("RimWorld.StyleItem") });
            if (m == null)
            {
                Log.Warning("[A6k_CompVariant] Target method WornByAnyoneOfIdeology or WornByAnyoneOfStyleItem not found, skipping Patch_ClearStyleDiversityCache.");
            }
            return m;
        }

        public static void Prefix()
        {
            A6k_CacheHandler.ClearStyleDiversityCache();
        }

        public static bool Prepare() => GenTypes.GetTypeInAnyAssembly("RimWorld.StyleItemUtility") != null;
    }
}
