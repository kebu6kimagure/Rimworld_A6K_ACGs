using HarmonyLib;
using RimWorld;
using Verse;

namespace A6k_CompVariant.Debug
{
    [StaticConstructorOnStartup]
    public static class A6k_Debug_PatchTester
    {
        static A6k_Debug_PatchTester()
        {
            var harmony = new Harmony("A6k.CompVariant.DebugPatchTester");
            try
            {
                harmony.Patch(
                    AccessTools.PropertyGetter(typeof(Apparel), "WornGraphicPath"),
                    prefix: new HarmonyMethod(typeof(A6k_Debug_PatchTester), nameof(Debug_WornGraphicPath_Prefix))
                );
                harmony.Patch(
                    AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel", new[] { typeof(Apparel), typeof(BodyTypeDef), typeof(bool), typeof(ApparelGraphicRecord).MakeByRefType() }),
                    prefix: new HarmonyMethod(typeof(A6k_Debug_PatchTester), nameof(Debug_TryGetGraphicApparel_Prefix))
                );
                harmony.Patch(
                    AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", new[] { typeof(PawnDrawParms) }),
                    prefix: new HarmonyMethod(typeof(A6k_Debug_PatchTester), nameof(Debug_RenderPawnInternal_Prefix))
                );
            }
            catch
            {
                // 静默处理异常，不输出日志
            }
        }

        public static bool Debug_WornGraphicPath_Prefix(Apparel __instance, ref string __result)
        {
            var comp = __instance.GetComp<Comp_VariantApparel>();
            if (comp != null && !comp.SelectedGraphicPath.NullOrEmpty())
            {
                __result = comp.SelectedGraphicPath;
                return false;
            }
            return true;
        }

        public static bool Debug_TryGetGraphicApparel_Prefix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec)
        {
            // 直接调用核心逻辑，避免重复
            return Patch_TryGetGraphicApparel.Prefix(apparel, bodyType, forStatue, ref rec);
        }

        public static void Debug_RenderPawnInternal_Prefix(PawnRenderer __instance, PawnDrawParms parms)
        {
            // 静默处理，不输出日志
        }

        public static void ForceRefreshPawnGraphics(Pawn pawn)
        {
            // 保留刷新功能以防万一
            if (pawn == null || pawn.apparel == null) return;
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(pawn);
            pawn.Drawer.Notify_DamageApplied(new DamageInfo(DamageDefOf.Scratch, 0));
        }
    }
}
