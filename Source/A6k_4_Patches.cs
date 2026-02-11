using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
namespace A6k_CompVariant
{
    [StaticConstructorOnStartup]
    public static class A6k_HarmonyPatches_Init
    {
        static A6k_HarmonyPatches_Init()
        {
            var harmony = new Harmony("A6k.CompVariant.Harmony");
            try
            {
                Log.Message("[A6k_CompVariant] 开始应用Harmony补丁...");
                PatchMethodIfExists(harmony, typeof(Patch_DisableSelected));
                PatchMethodIfExists(harmony, typeof(Patch_ApparelWornGraphicPath));
                PatchMethodIfExists(harmony, typeof(Patch_TryGetGraphicApparel));
                if (GenTypes.GetTypeInAnyAssembly("RimWorld.StyleItemUtility") != null)
                    PatchMethodIfExists(harmony, typeof(Patch_ClearStyleDiversityCache));
                Type unfinishedGizmosType = typeof(Patch_UnfinishedThing_GetGizmos);
                Type unfinishedSpawnType = typeof(Patch_UnfinishedThing_SpawnSetup);
                Type billCompletionType = typeof(Patch_BillCompletion);
                Type billExposeType = typeof(Patch_BillExposeData);
                PatchMethodIfExists(harmony, unfinishedGizmosType);
                PatchMethodIfExists(harmony, unfinishedSpawnType);
                PatchMethodIfExists(harmony, billCompletionType);
                PatchMethodIfExists(harmony, billExposeType);
                // 新增穿戴装备时刷新贴图的补丁
                PatchMethodIfExists(harmony, typeof(Patch_Notify_ApparelAdded));
                // 新增半成品DeSpawn补丁
                PatchMethodIfExists(harmony, typeof(Patch_UnfinishedThing_DeSpawn));
                PatchRespawningAfterLoad(harmony);
                Log.Message("[A6k_CompVariant] Harmony补丁应用完成。");
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] Harmony补丁异常: {ex}");
            }
        }
        private static void PatchMethodIfExists(Harmony harmony, Type patchType)
        {
            try
            {
                if (patchType == null) return;
                var prepareMethod = AccessTools.Method(patchType, "Prepare");
                if (prepareMethod != null && !(bool)prepareMethod.Invoke(null, null))
                    return;
                var targetMethod = AccessTools.Method(patchType, "TargetMethod");
                if (targetMethod != null)
                {
                    var methodInfo = targetMethod.Invoke(null, null) as MethodBase;
                    if (methodInfo != null)
                        harmony.Patch(methodInfo, null, null, null);
                    else
                        Log.Warning($"[A6k_CompVariant] TargetMethod 返回 null，跳过: {patchType.Name}");
                }
                else
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    Log.Message($"[A6k_CompVariant] 直接补丁成功: {patchType.Name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] 补丁处理异常 {patchType.Name}: {ex}");
            }
        }
        private static void PatchRespawningAfterLoad(Harmony harmony)
        {
            var method = AccessTools.Method(typeof(Pawn), "SpawnSetup");
            if (method != null)
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(A6k_PostfixPatch_Respawn), "PostfixMethod"));
            else
                Log.Warning("[A6k_CompVariant] Pawn.SpawnSetup 未找到，跳过 respawn 补丁。");
        }
    }
    public static class A6k_PostfixPatch_Respawn
    {
        public static void PostfixMethod(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (!respawningAfterLoad || !__instance.Spawned) return;
            try
            {
                var variants = __instance.apparel?.WornApparel?
                    .Select(a => a.GetComp<Comp_VariantApparel>())
                    .Where(c => c != null && !c.SelectedGraphicPath.NullOrEmpty());
                if (variants != null && variants.Any())
                {
                    variants.Do(c => c.UpdateGraphic());
                    Log.Message($"[A6k_CompVariant_Debug] respawn后刷新贴图，Pawn: {__instance.Name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] respawn 贴图刷新异常: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.PostLoad))]
    public static class Patch_DisableSelected
    {
        public static void Postfix(ThingDef __instance)
        {
            if (A6kCompVariantMod.Settings?.GetSelectedDefs()?.Contains(__instance.defName) == true)
            {
                __instance.generateCommonality = 0f;
                __instance.researchPrerequisites?.Clear();
            }
        }
    }
    [HarmonyPatch]
    public static class Patch_ApparelWornGraphicPath
    {
        public static MethodBase TargetMethod() =>
            AccessTools.PropertyGetter(typeof(Apparel), "WornGraphicPath");
        public static bool Prefix(Apparel __instance, ref string __result)
        {
            // 仅在装备被穿戴时应用自定义路径，避免影响地面贴图
            if (__instance.Wearer != null)
            {
                var c = __instance.GetComp<Comp_VariantApparel>();
                if (c != null && !c.SelectedGraphicPath.NullOrEmpty())
                {
                    __result = c.SelectedGraphicPath;
                    Log.Message($"[A6k_CompVariant_Debug] WornGraphicPath补丁生效，装备: {__instance.def.defName}, 返回自定义路径: {__result}");
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch]
    public static class Patch_TryGetGraphicApparel
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel",
                new[] { typeof(Apparel), typeof(BodyTypeDef), typeof(bool), typeof(ApparelGraphicRecord).MakeByRefType() });
        public static bool Prefix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec)
        {
            // 直接回退到默认贴图逻辑，不尝试自定义加载，也不输出任何日志
            return true;
        }
    }
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded")]
    public static class Patch_Notify_ApparelAdded
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            try
            {
                var comp = apparel.GetComp<Comp_VariantApparel>();
                if (comp != null && !comp.SelectedGraphicPath.NullOrEmpty())
                {
                    comp.UpdateGraphic();
                    Log.Message($"[A6k_CompVariant_Debug] 穿上装备 {apparel.def.defName} 时触发贴图刷新，Pawn: {__instance.pawn.Name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] 穿上装备刷新贴图异常: {ex.Message}");
            }
        }
    }
    [HarmonyPatch(typeof(Bill_Production), "Notify_IterationCompleted")]
    public static class Patch_BillCompletion
    {
        public static void Postfix(Bill_Production __instance, Pawn billDoer, List<Thing> ingredients)
        {
            if (__instance == null || __instance.recipe == null || __instance.recipe.products == null || !__instance.recipe.products.Any())
            {
                Log.Message("[A6k_CompVariant] Bill completed, but recipe or product data is invalid. Skipping variant handling.");
                return;
            }
            var productDef = __instance.recipe.products[0].thingDef;
            if (!productDef.IsApparel || !productDef.defName.StartsWith("A6k_"))
            {
                Log.Message($"[A6k_CompVariant] Bill completed for {productDef.defName}, but it is not a mod apparel. Skipping variant handling.");
                return;
            }
            // 现在不再尝试查找未完成物品和置顶路径，逻辑移至DeSpawn补丁
            Log.Message($"[A6k_CompVariant] Bill completed for {productDef.defName}, variant handling deferred to UnfinishedThing DeSpawn.");
        }
        public static bool Prepare()
        {
            var method = AccessTools.Method(typeof(Bill_Production), "Notify_IterationCompleted");
            if (method == null)
            {
                Log.Warning("[A6k_CompVariant] Bill_Production.Notify_IterationCompleted method not found, skipping Patch_BillCompletion.");
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Bill_Production), "ExposeData")]
    public static class Patch_BillExposeData
    {
        public static void Postfix(Bill_Production __instance)
        {
            // 暂时注释掉缺失类型的引用代码
            /*
            BillVariantHandler handler = BillVariantDataStore.GetHandler(__instance);
            handler.ExposeData();
            */
            Log.Message($"[A6k_CompVariant] Bill expose data for {__instance.recipe?.defName ?? "unknown"}, variant handling skipped (types not defined).");
        }
        public static bool Prepare()
        {
            var method = AccessTools.Method(typeof(Bill_Production), "ExposeData");
            if (method == null)
            {
                Log.Warning("[A6k_CompVariant] Bill_Production.ExposeData method not found, skipping Patch_BillExposeData.");
                return false;
            }
            return true;
        }
    }
}