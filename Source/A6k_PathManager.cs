using System.Linq;
using Verse;

namespace A6k_CompVariant
{
    public static class A6k_PathManager
    {
        public static void ReinitializeVariantPaths()
        {
            // 清空现有的变体路径列表
            Comp_VariantApparel.HatVariantPaths.Clear();
            Comp_VariantApparel.OuterVariantPaths.Clear();

            // 从 DefDatabase 重新加载所有 HatVariantDef 和 OuterwearVariantDef 的路径
            foreach (var hatDef in DefDatabase<HatVariantDef>.AllDefsListForReading)
            {
                if (!string.IsNullOrEmpty(hatDef.graphicPath))
                {
                    Comp_VariantApparel.HatVariantPaths.Add(new VariantPathInfo { Path = hatDef.graphicPath, ModSource = hatDef.modContentPack?.Name ?? "Unknown" });
                }
            }

            foreach (var outerDef in DefDatabase<OuterwearVariantDef>.AllDefsListForReading)
            {
                if (!string.IsNullOrEmpty(outerDef.graphicPath))
                {
                    Comp_VariantApparel.OuterVariantPaths.Add(new VariantPathInfo { Path = outerDef.graphicPath, ModSource = outerDef.modContentPack?.Name ?? "Unknown" });
                }
            }

            // 现在添加 selectedVariants 中的路径，避免重复
            var settings = A6kCompVariantMod.Settings;
            if (settings != null)
            {
                if (settings.selectedHatVariants != null)
                {
                    foreach (var sv in settings.selectedHatVariants)
                    {
                        if (!string.IsNullOrEmpty(sv.GraphicPath) && !Comp_VariantApparel.HatVariantPaths.Any(v => v.Path == sv.GraphicPath))
                        {
                            Comp_VariantApparel.HatVariantPaths.Add(new VariantPathInfo { Path = sv.GraphicPath, ModSource = sv.ModSource });
                        }
                    }
                }
                if (settings.selectedOuterVariants != null)
                {
                    foreach (var sv in settings.selectedOuterVariants)
                    {
                        if (!string.IsNullOrEmpty(sv.GraphicPath) && !Comp_VariantApparel.OuterVariantPaths.Any(v => v.Path == sv.GraphicPath))
                        {
                            Comp_VariantApparel.OuterVariantPaths.Add(new VariantPathInfo { Path = sv.GraphicPath, ModSource = sv.ModSource });
                        }
                    }
                }
            }

            Log.Message("[A6k_CompVariant] Variant paths reinitialized successfully. Hat paths count: " + Comp_VariantApparel.HatVariantPaths.Count + ", Outer paths count: " + Comp_VariantApparel.OuterVariantPaths.Count);
        }
    }
}
