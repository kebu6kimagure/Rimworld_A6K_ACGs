using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace A6k_CompVariant
{
    public static class A6k_RecipeHandler
    {
        public static void RemoveRecipesForSelected()
        {
            if (A6kCompVariantMod.Settings == null)
            {
                Log.Warning("[A6k_CompVariant] Settings 未初始化，跳过配方移除逻辑。");
                return;
            }

            // 延迟执行，确保 DefDatabase 已加载
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    int removedProducts = 0;
                    int hiddenRecipes = 0;
                    var selected = new HashSet<string>(A6kCompVariantMod.Settings.GetSelectedDefs());
                    var allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading.ToList();

                    Log.Message($"[A6k_CompVariant] 开始移除配方，选中的装备定义数量：{selected.Count}，总配方数量：{allRecipes.Count}");

                    if (allRecipes.Count == 0)
                    {
                        Log.Warning("[A6k_CompVariant] 配方数据库为空，可能尚未加载完成，将重新尝试延迟执行。");
                        return;
                    }

                    foreach (var r in allRecipes)
                    {
                        // 只处理与服装制作相关的配方
                        bool isApparelRecipe = false;
                        if (r.products != null && r.products.Any())
                        {
                            foreach (var product in r.products)
                            {
                                if (product.thingDef != null && product.thingDef.IsApparel)
                                {
                                    isApparelRecipe = true;
                                    break;
                                }
                            }
                        }

                        if (!isApparelRecipe)
                        {
                            continue; // 跳过非服装制作相关的配方
                        }

                        // 检查当前地图是否存在正在使用该配方的活跃账单
                        var activeBills = new List<Bill_Production>();
                        if (Find.Maps != null)
                        {
                            activeBills = Find.Maps
                                .SelectMany(m => m.listerBuildings?.allBuildingsColonist ?? Enumerable.Empty<Building>())
                                .SelectMany(b => (b as IBillGiver)?.BillStack?.Bills ?? Enumerable.Empty<Bill>())
                                .OfType<Bill_Production>()
                                .Where(b => b.recipe == r && !b.suspended && !b.deleted)
                                .ToList() ?? new List<Bill_Production>();
                        }

                        bool isActive = activeBills.Any();
                        if (isActive)
                            continue;

                        bool modified = false;
                        if (r.products != null)
                        {
                            for (int i = r.products.Count - 1; i >= 0; i--)
                            {
                                if (selected.Contains(r.products[i].thingDef.defName))
                                {
                                    r.products.RemoveAt(i);
                                    removedProducts++;
                                    modified = true;
                                }
                            }

                            // 如果配方产品为空，则彻底隐藏配方
                            if (r.products.Count == 0)
                            {
                                r.defaultIngredientFilter = new ThingFilter();
                                if (r.recipeUsers != null)
                                {
                                    r.recipeUsers.Clear();
                                }
                                r.skillRequirements = null; // 额外禁用，确保不可用
                                hiddenRecipes++;
                            }
                        }
                    }

                    Log.Message($"[A6k_CompVariant] 完成配方移除：移除了 {removedProducts} 个配方产品，隐藏了 {hiddenRecipes} 个配方。");

                    // 强制刷新工作台的配方缓存（尝试通知游戏更新）
                    if (Find.Maps != null)
                    {
                        foreach (var map in Find.Maps)
                        {
                            if (map.listerBuildings?.allBuildingsColonist != null)
                            {
                                foreach (var building in map.listerBuildings.allBuildingsColonist)
                                {
                                    if (building is IBillGiver billGiver && billGiver.BillStack != null)
                                    {
                                        billGiver.BillStack.Clear(); // 清除当前账单，强制重新生成
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("[A6k_CompVariant] Find.Maps 为 null，无法刷新工作台账单缓存。");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[A6k_CompVariant] 配方移除逻辑执行失败：{ex.Message}\n{ex.StackTrace}");
                }
            });
        }
    }
}
