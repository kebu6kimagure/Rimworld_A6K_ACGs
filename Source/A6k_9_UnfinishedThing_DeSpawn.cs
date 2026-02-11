using HarmonyLib;
using RimWorld;
using Verse;
namespace A6k_CompVariant
{
    [HarmonyPatch(typeof(Thing), "DeSpawn")]
    public static class Patch_UnfinishedThing_DeSpawn
    {
        public static void Postfix(Thing __instance)
        {
            if (!(__instance is UnfinishedThing unfinished)) return; // 只处理UnfinishedThing
            var bill = unfinished.BoundBill as Bill_Production;
            if (bill == null) return; // 确保绑定了Bill_Production，避免误触发
            var comp = unfinished.TryGetComp<Comp_VariantApparel>();
            if (comp == null || comp.SelectedGraphicPath.NullOrEmpty()) return; // 检查是否有设置路径
            string selectedPath = comp.SelectedGraphicPath;
            string variantType = bill.recipe.products[0].thingDef.apparel.layers.Contains(ApparelLayerDefOf.Overhead) ? "Hat" : "Outerwear"; // 确定变体类型
            try
            {
                A6k_VariantPresetPool.InsertCustomPath(selectedPath, variantType);
                Log.Message($"[A6k_CompVariant] UnfinishedThing DeSpawn: 半成品消失，路径 {selectedPath} 已置顶到 {variantType} 预设池。");
            }
            catch
            {
                Log.Error($"[A6k_CompVariant] UnfinishedThing DeSpawn: 置顶预设池失败，路径 {selectedPath}");
            }
        }
        public static bool Prepare()
        {
            var method = AccessTools.Method(typeof(Thing), "DeSpawn");
            if (method == null)
            {
                Log.Warning("[A6k_CompVariant] Thing.DeSpawn method not found, skipping Patch_UnfinishedThing_DeSpawn.");
                return false;
            }
            return true;
        }
    }
}