using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace A6k_CompVariant
{
    [HarmonyPatch(typeof(UnfinishedThing), "GetGizmos")]
    public static class Patch_UnfinishedThing_GetGizmos
    {
        public static void Postfix(UnfinishedThing __instance, ref IEnumerable<Gizmo> __result)
        {
            Bill_Production bill = FindBillForUnfinishedThing(__instance);
            if (bill == null || bill.recipe == null || bill.recipe.products == null || !bill.recipe.products.Any())
            {
                return;
            }
            bool isApparelProduct = bill.recipe.products.Any(p => p.thingDef != null && IsModApparel(p.thingDef));
            if (!isApparelProduct)
            {
                return;
            }
            var comp = __instance.TryGetComp<Comp_VariantApparel>();
            if (comp == null)
            {
                AddCompDynamically(__instance);
                comp = __instance.TryGetComp<Comp_VariantApparel>();
                if (comp == null) return;
            }
            List<Gizmo> gizmos = __result?.ToList() ?? new List<Gizmo>();
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "选择贴图变体",
                defaultDesc = "为该服饰选择一个自定义贴图变体。",
                icon = ContentFinder<Texture2D>.Get("UI/BillVariantHandler", false) ?? ContentFinder<Texture2D>.Get("UI/Commands/Desire", false) ?? ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", false),
                action = () =>
                {
                    var apparelDef = bill.recipe.products.First(p => IsModApparel(p.thingDef)).thingDef;
                    string variantTypeStr = apparelDef.apparel.layers.Contains(ApparelLayerDefOf.Overhead) ? "Hat" : "Outerwear";
                    var pathsInfo = variantTypeStr == "Hat" ? Comp_VariantApparel.HatVariantPaths : Comp_VariantApparel.OuterVariantPaths;
                    if (!pathsInfo.Any())
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("无可用贴图变体，请检查模组配置或日志。"));
                        return;
                    }
                    var grouped = pathsInfo.GroupBy(v => v.ModSource).OrderBy(g => g.Key);
                    List<FloatMenuOption> modOptions = new List<FloatMenuOption>();
                    foreach (var group in grouped)
                    {
                        string modName = group.Key;
                        var subOptions = group.Select(v => new FloatMenuOption(v.Path.Split('/').Last(), () => { comp.SetGraphicPath(v.Path); })).ToList();
                        FloatMenuOption modOption = new FloatMenuOption(modName, () => Find.WindowStack.Add(new FloatMenu(subOptions)));
                        modOptions.Add(modOption);
                    }
                    Find.WindowStack.Add(new FloatMenu(modOptions));
                }
            };
            gizmos.Add(cmd);
            __result = gizmos;
        }

        private static Bill_Production FindBillForUnfinishedThing(UnfinishedThing thing)
        {
            return thing?.BoundBill as Bill_Production;
        }

        private static void AddCompDynamically(UnfinishedThing thing)
        {
            try
            {
                var props = new CompProperties_VariantApparel { compClass = typeof(Comp_VariantApparel) };
                var comp = (Comp_VariantApparel)Activator.CreateInstance(props.compClass);
                comp.props = props;
                comp.parent = thing;
                var comps = thing.AllComps;
                if (comps != null && !comps.Contains(comp))
                    comps.Add(comp);
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] 动态添加组件失败: {ex.Message}");
            }
        }

        private static bool IsModApparel(ThingDef def)
        {
            return def != null && def.IsApparel && def.defName.StartsWith("A6k_");
        }

        public static bool Prepare()
        {
            var method = AccessTools.Method(typeof(UnfinishedThing), "GetGizmos");
            if (method == null)
            {
                Log.Warning("[A6k_CompVariant] UnfinishedThing.GetGizmos method not found, skipping Patch_UnfinishedThing_GetGizmos.");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.SpawnSetup))]
    public static class Patch_UnfinishedThing_SpawnSetup
    {
        static void Postfix(ThingWithComps __instance, Map map, bool respawningAfterLoad)
        {
            if (!(__instance is UnfinishedThing unfinished))
                return;

            try
            {
                var bill = unfinished.BoundBill as Bill_Production;
                if (bill?.recipe?.products == null) return;

                bool hasModApparel = bill.recipe.products.Any(p => p.thingDef != null && IsModApparel(p.thingDef));
                if (!hasModApparel) return;

                if (unfinished.GetComp<Comp_VariantApparel>() != null) return;

                var props = new CompProperties_VariantApparel { compClass = typeof(Comp_VariantApparel) };
                var comp = (Comp_VariantApparel)Activator.CreateInstance(props.compClass);
                comp.props = props;
                comp.parent = unfinished;

                var comps = unfinished.AllComps;
                if (comps != null && !comps.Contains(comp))
                    comps.Add(comp);
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] Patch_UnfinishedThing_SpawnSetup 异常: {ex}");
            }
        }

        private static bool IsModApparel(ThingDef def)
        {
            return def != null && def.IsApparel && def.defName.StartsWith("A6k_");
        }
    }
}
