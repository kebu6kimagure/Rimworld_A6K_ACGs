using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace A6k_CompVariant
{
    public class A6kCompVariantSettings : ModSettings
    {
        public List<SelectedVariant> selectedHatVariants = new List<SelectedVariant>();
        public List<SelectedVariant> selectedOuterVariants = new List<SelectedVariant>();
        private string searchText = "";
        private Vector2 scrollPos;
        private string selectedModFilter = "All";
        public static A6kCompVariantSettings Instance { get; private set; }
        public event Action SettingsChanged = delegate { };

        public List<string> GetSelectedDefs()
        {
            List<string> combinedDefs = new List<string>();
            if (selectedHatVariants != null) combinedDefs.AddRange(selectedHatVariants.Select(v => v.DefName));
            if (selectedOuterVariants != null) combinedDefs.AddRange(selectedOuterVariants.Select(v => v.DefName));
            return combinedDefs.Distinct().ToList();
        }

        public List<string> GetSelectedGraphicPaths()
        {
            List<string> combinedPaths = new List<string>();
            if (selectedHatVariants != null) combinedPaths.AddRange(selectedHatVariants.Select(v => v.GraphicPath));
            if (selectedOuterVariants != null) combinedPaths.AddRange(selectedOuterVariants.Select(v => v.GraphicPath));
            return combinedPaths.Distinct().ToList();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (selectedHatVariants == null) selectedHatVariants = new List<SelectedVariant>();
            if (selectedOuterVariants == null) selectedOuterVariants = new List<SelectedVariant>();
            Scribe_Collections.Look(ref selectedHatVariants, "selectedHatVariants", LookMode.Deep);
            Scribe_Collections.Look(ref selectedOuterVariants, "selectedOuterVariants", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Instance = this;
                // 延迟执行清理无效设置和路径初始化，确保DefDatabase完全加载
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    try
                    {
                        // 先检查DefDatabase是否完全加载（通过尝试获取一些核心Def来验证）
                        if (DefDatabase<ThingDef>.GetNamedSilentFail("Human") == null) // 示例检查，确保核心Def存在
                        {
                            Log.Warning("[A6k_CompVariant] DefDatabase可能尚未完全加载，延迟清理推迟。");
                            return; // 如果核心Def不见了，说明还不够安全，跳过或重试（这里简单跳过）
                        }
                        // 执行清理无效设置
                        int removedHatVariants = selectedHatVariants.RemoveAll(v => string.IsNullOrEmpty(v.DefName) || DefDatabase<ThingDef>.GetNamedSilentFail(v.DefName) == null);
                        int removedOuterVariants = selectedOuterVariants.RemoveAll(v => string.IsNullOrEmpty(v.DefName) || DefDatabase<ThingDef>.GetNamedSilentFail(v.DefName) == null);
                        if (removedHatVariants > 0 || removedOuterVariants > 0)
                        {
                            Log.Message($"[A6k_CompVariant] Delayed cleanup of invalid settings: {removedHatVariants} Hat variants, {removedOuterVariants} Outer variants removed.");
                        }
                        else
                        {
                            Log.Message("[A6k_CompVariant] Delayed cleanup: No invalid variants removed. Settings should be intact.");
                        }
                        // 重新初始化变体路径和预设池
                        A6k_PathManager.ReinitializeVariantPaths();
                        A6k_VariantPresetPool.InitializePools();
                        Log.Message($"[A6k_CompVariant] PostLoad delayed handling completed: Settings loaded and validated. HatVariants count: {selectedHatVariants?.Count ?? 0}, OuterVariants count: {selectedOuterVariants?.Count ?? 0}. Preset pools reinitialized.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[A6k_CompVariant] Delayed post-load handling failed: {ex.Message}\n{ex.StackTrace}");
                    }
                });
            }
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            var topListing = new Listing_Standard();
            topListing.Begin(inRect);

            topListing.Label(A6k_TranslationHelper.T("A6k_Settings_VariantSelection"));
            topListing.GapLine();

            DoVariantSelectionUI(topListing, inRect);

            topListing.End();

            if (GUI.changed)
            {
                this.Write();
                SettingsChanged?.Invoke();
            }
        }

        private void DoVariantSelectionUI(Listing_Standard listing, Rect inRect)
        {
            listing.Label(A6k_TranslationHelper.T("A6k_Settings_Search"));
            searchText = listing.TextEntry(searchText);
            listing.Gap();
            if (Widgets.ButtonText(listing.GetRect(22f), A6k_TranslationHelper.T("A6k_Settings_ModFilter") + selectedModFilter))
            {
                // 获取所有包含服饰定义的mod
                var modsWithApparel = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(d => d.IsApparel
                                && d.modContentPack?.PackageId != "a6k.compvariant.mod"
                                && d.apparel.layers.Any(l => l == ApparelLayerDefOf.Overhead || l == ApparelLayerDefOf.Shell))
                    .Select(d => d.modContentPack?.Name ?? "Core")
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                var opts = new List<FloatMenuOption> { new FloatMenuOption(A6k_TranslationHelper.T("A6k_Settings_All"), () => selectedModFilter = "All") };
                opts.AddRange(modsWithApparel.Select(m => new FloatMenuOption(m, () => selectedModFilter = m)));
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            listing.GapLine();
            var allDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsApparel
                            && d.modContentPack?.PackageId != "a6k.compvariant.mod"
                            && d.apparel.layers.Any(l => l == ApparelLayerDefOf.Overhead || l == ApparelLayerDefOf.Shell))
                .ToList();
            IEnumerable<ThingDef> filtered = allDefs;
            if (selectedModFilter != "All")
                filtered = filtered.Where(d => (d.modContentPack?.Name ?? "Core") == selectedModFilter);
            if (!searchText.NullOrEmpty())
                filtered = filtered.Where(d => d.label.ToLower().Contains(searchText.ToLower()));
            var defs = filtered.OrderBy(d => d.label).ToList();
            Rect buttonRect = listing.GetRect(30f);
            if (Widgets.ButtonText(buttonRect.LeftPart(0.49f), A6k_TranslationHelper.T("A6k_Settings_SelectAll")))
            {
                foreach (var d in defs)
                    UpdateSelection(d, true, IsHatDef(d));
                SettingsChanged?.Invoke();
            }
            if (Widgets.ButtonText(buttonRect.RightPart(0.49f), A6k_TranslationHelper.T("A6k_Settings_DeselectAll")))
            {
                foreach (var d in defs)
                    UpdateSelection(d, false, IsHatDef(d));
                SettingsChanged?.Invoke();
            }
            listing.Gap();
            var viewRect = new Rect(0f, 0f, inRect.width - 16f, defs.Count * 30f);
            Widgets.BeginScrollView(listing.GetRect(inRect.height - listing.CurHeight - 30f), ref scrollPos, viewRect);
            var innerListing = new Listing_Standard();
            innerListing.Begin(viewRect);
            foreach (var d in defs)
            {
                bool isHat = IsHatDef(d);
                bool selected = isHat ? selectedHatVariants.Any(v => v.DefName == d.defName) : selectedOuterVariants.Any(v => v.DefName == d.defName);
                Rect rowRect = innerListing.GetRect(30f);
                bool newSelected = selected;
                string labelWithType = d.label + " (" + (d.modContentPack?.Name ?? A6k_TranslationHelper.T("A6k_Settings_Core")) + ")" + (isHat ? " [Hat]" : " [Outerwear]");
                Widgets.CheckboxLabeled(rowRect, labelWithType, ref newSelected);
                if (newSelected != selected)
                    UpdateSelection(d, newSelected, isHat);
            }
            innerListing.End();
            Widgets.EndScrollView();
        }

        private void UpdateSelection(ThingDef d, bool isSelected, bool isHat)
        {
            if (d.modContentPack?.PackageId == "a6k.compvariant.mod") return;

            string path = d.apparel?.wornGraphicPath;
            if (string.IsNullOrEmpty(path)) return;

            if (isHat)
            {
                var variantList = selectedHatVariants;
                if (isSelected)
                {
                    if (!variantList.Any(v => v.DefName == d.defName))
                    {
                        var newVariant = new SelectedVariant { DefName = d.defName, GraphicPath = path, ModSource = d.modContentPack.Name };
                        variantList.Add(newVariant);
                        if (!Comp_VariantApparel.HatVariantPaths.Any(v => v.Path == path))
                            Comp_VariantApparel.HatVariantPaths.Add(new VariantPathInfo { Path = path, ModSource = d.modContentPack.Name });
                        Log.Message($"[A6k_CompVariant] Added Hat variant: {d.defName} with path {path}, mod: {d.modContentPack.Name}");
                    }
                }
                else
                {
                    var varToRemove = variantList.FirstOrDefault(v => v.DefName == d.defName);
                    if (varToRemove != null)
                    {
                        variantList.Remove(varToRemove);
                        var pathToRemove = varToRemove.GraphicPath;
                        var variantPathToRemove = Comp_VariantApparel.HatVariantPaths.FirstOrDefault(v => v.Path == pathToRemove);
                        if (variantPathToRemove != null) Comp_VariantApparel.HatVariantPaths.Remove(variantPathToRemove);
                        Log.Message($"[A6k_CompVariant] Removed Hat variant: {d.defName}");
                    }
                }
            }
            else
            {
                var variantList = selectedOuterVariants;
                if (isSelected)
                {
                    if (!variantList.Any(v => v.DefName == d.defName))
                    {
                        var newVariant = new SelectedVariant { DefName = d.defName, GraphicPath = path, ModSource = d.modContentPack.Name };
                        variantList.Add(newVariant);
                        if (!Comp_VariantApparel.OuterVariantPaths.Any(v => v.Path == path))
                            Comp_VariantApparel.OuterVariantPaths.Add(new VariantPathInfo { Path = path, ModSource = d.modContentPack.Name });
                        Log.Message($"[A6k_CompVariant] Added Outerwear variant: {d.defName} with path {path}, mod: {d.modContentPack.Name}");
                    }
                }
                else
                {
                    var varToRemove = variantList.FirstOrDefault(v => v.DefName == d.defName);
                    if (varToRemove != null)
                    {
                        variantList.Remove(varToRemove);
                        var pathToRemove = varToRemove.GraphicPath;
                        var variantPathToRemove = Comp_VariantApparel.OuterVariantPaths.FirstOrDefault(v => v.Path == pathToRemove);
                        if (variantPathToRemove != null) Comp_VariantApparel.OuterVariantPaths.Remove(variantPathToRemove);
                        Log.Message($"[A6k_CompVariant] Removed Outerwear variant: {d.defName}");
                    }
                }
            }
            SettingsChanged?.Invoke();
        }

        private static bool IsHatDef(ThingDef d)
        {
            if (d.IsApparel)
            {
                if (d.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
                    return true;
                if (d.apparel.layers.Contains(ApparelLayerDefOf.Shell))
                    return false;
                return d.apparel.tags.Any(t => t.ToLower().Contains("head"));
            }
            return false;
        }
    }

    public class SelectedVariant : IExposable
    {
        public string DefName;
        public string GraphicPath;
        public string ModSource;

        public void ExposeData()
        {
            Scribe_Values.Look(ref DefName, "defName");
            Scribe_Values.Look(ref GraphicPath, "graphicPath");
            Scribe_Values.Look(ref ModSource, "modSource");
        }
    }
}
