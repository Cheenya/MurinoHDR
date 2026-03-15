using System.Collections.Generic;
using MurinoHDR.Core;
using UnityEngine;

namespace MurinoHDR.Inventory
{

[RequireComponent(typeof(Inventory))]
public sealed class CraftingSystem : MonoBehaviour
{
    private readonly List<CraftRecipe> _recipes = new List<CraftRecipe>();
    private Inventory _inventory;

    public IReadOnlyList<CraftRecipe> Recipes => _recipes;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        RefreshRecipes();
    }

    public void RefreshRecipes()
    {
        _recipes.Clear();
        var catalog = MvpRuntimeContent.Catalog;
        for (var i = 0; i < catalog.Recipes.Count; i++)
        {
            _recipes.Add(catalog.Recipes[i]);
        }
    }

    public bool CanCraft(CraftRecipe recipe)
    {
        if (recipe == null || recipe.Output == null)
        {
            return false;
        }

        for (var i = 0; i < recipe.Inputs.Length; i++)
        {
            var input = recipe.Inputs[i];
            if (input.Item == null || !_inventory.HasItem(input.Item.Id, input.Count))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryCraft(CraftRecipe recipe)
    {
        if (!CanCraft(recipe))
        {
            return false;
        }

        for (var i = 0; i < recipe.Inputs.Length; i++)
        {
            var input = recipe.Inputs[i];
            _inventory.TryRemoveItem(input.Item, input.Count);
        }

        _inventory.AddItem(recipe.Output, recipe.OutputCount);
        Debug.Log(string.Format("[INV] Crafted {0}", recipe.Output.DisplayName));
        return true;
    }
}
}
