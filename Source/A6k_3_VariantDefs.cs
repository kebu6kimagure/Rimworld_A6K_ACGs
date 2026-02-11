using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace A6k_CompVariant
{
    public class HatVariantDef : Def
    {
        public string graphicPath;
    }

    public class OuterwearVariantDef : Def
    {
        public string graphicPath;
    }

    public class CompProperties_VariantApparel : CompProperties
    {
        public string variantType;
        public CompProperties_VariantApparel()
        {
            compClass = typeof(Comp_VariantApparel);
        }
    }

    public class Comp_VariantApparel : ThingComp
    {
        private string selectedGraphicPath;
        public static List<VariantPathInfo> HatVariantPaths = new List<VariantPathInfo>();
        public static List<VariantPathInfo> OuterVariantPaths = new List<VariantPathInfo>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (string.IsNullOrEmpty(selectedGraphicPath) && parent.Spawned)
            {
                selectedGraphicPath = A6k_VariantPresetPool.GetPresetPath(Props.variantType);
            }
            if (!string.IsNullOrEmpty(selectedGraphicPath))
            {
                UpdateGraphic();
            }
        }

        public void SetGraphicPath(string path)
        {
            selectedGraphicPath = path;
            UpdateGraphic();
        }

        public void UpdateGraphic()
        {
            if (parent is Apparel apparel && !selectedGraphicPath.NullOrEmpty())
            {
                try
                {
                    if (apparel.Wearer != null)
                    {
                        apparel.Wearer.Drawer.renderer.SetAllGraphicsDirty();
                        PortraitsCache.SetDirty(apparel.Wearer);
                        apparel.Wearer.Drawer.Notify_DamageApplied(new DamageInfo(DamageDefOf.Scratch, 0));
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[A6k_CompVariant] 强制更新贴图失败给 {parent.def.defName}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref selectedGraphicPath, "selectedGraphicPath");
        }

        public CompProperties_VariantApparel Props => (CompProperties_VariantApparel)props;
        public string SelectedGraphicPath => selectedGraphicPath;
    }

    public class VariantPathInfo
    {
        public string Path { get; set; }
        public string ModSource { get; set; }
    }
}
