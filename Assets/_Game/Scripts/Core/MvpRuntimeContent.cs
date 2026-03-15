using System.Collections.Generic;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using UnityEngine;

namespace MurinoHDR.Core
{

public sealed class MvpContentCatalog : ScriptableObject
{
    [SerializeField] private ItemDefinition[] _items = System.Array.Empty<ItemDefinition>();
    [SerializeField] private CraftRecipe[] _recipes = System.Array.Empty<CraftRecipe>();
    [SerializeField] private FloorGeneratorSettings _generatorSettings;

    public IReadOnlyList<ItemDefinition> Items => _items;
    public IReadOnlyList<CraftRecipe> Recipes => _recipes;
    public FloorGeneratorSettings GeneratorSettings => _generatorSettings;

    public void Configure(ItemDefinition[] items, CraftRecipe[] recipes, FloorGeneratorSettings generatorSettings)
    {
        _items = items;
        _recipes = recipes;
        _generatorSettings = generatorSettings;
    }
}

public static class MvpRuntimeContent
{
    private static readonly Dictionary<string, ItemDefinition> ItemLookup = new Dictionary<string, ItemDefinition>();
    private static MvpContentCatalog _catalog;

    public static MvpContentCatalog Catalog
    {
        get
        {
            EnsureInitialized();
            return _catalog;
        }
    }

    public static void EnsureInitialized()
    {
        if (_catalog != null)
        {
            return;
        }

        _catalog = CreateRuntimeAsset<MvpContentCatalog>("MvpContentCatalog");

        var keycard = CreateItem("keycard", "Ключ-карта", false, 1, ItemTag.Keycard);
        var fuse = CreateItem("fuse", "Предохранитель", true, 4, ItemTag.Fuse);
        var tape = CreateItem("tape", "Изолента", true, 6, ItemTag.Tape);
        var repairedFuse = CreateItem("repaired_fuse", "Починенный предохранитель", true, 2, ItemTag.RepairedFuse);
        var crowbar = CreateItem("crowbar", "Ломик", false, 1, ItemTag.Tool);
        var rope = CreateItem("rope", "Верёвка", true, 2, ItemTag.Rope);
        var lockpick = CreateItem("lockpick", "Отмычка", true, 3, ItemTag.Lockpick);

        var repairFuseRecipe = CreateRuntimeAsset<CraftRecipe>("RepairFuseRecipe");
        var fuseIngredient = new RecipeIngredient();
        fuseIngredient.Configure(fuse, 1);
        var tapeIngredient = new RecipeIngredient();
        tapeIngredient.Configure(tape, 1);
        repairFuseRecipe.Configure("repair_fuse", "Починить предохранитель", new[] { fuseIngredient, tapeIngredient }, repairedFuse, 1);

        var startTemplate = CreateRoomTemplate("start_room", RoomCategory.Start, new Vector2Int(4, 3), true);
        var corridorTemplate = CreateRoomTemplate("corridor_room", RoomCategory.Corridor, new Vector2Int(2, 2), true);
        var hubTemplate = CreateRoomTemplate("hub_room", RoomCategory.Hub, new Vector2Int(6, 4), true);
        var officeTemplate = CreateRoomTemplate("office_room", RoomCategory.Office, new Vector2Int(4, 3), false);
        var storageTemplate = CreateRoomTemplate("storage_room", RoomCategory.Storage, new Vector2Int(4, 3), false);
        var utilityTemplate = CreateRoomTemplate("utility_room", RoomCategory.Utility, new Vector2Int(3, 3), false);
        var elevatorTemplate = CreateRoomTemplate("exit_elevator", RoomCategory.ExitElevator, new Vector2Int(4, 3), true);
        var shaftTemplate = CreateRoomTemplate("exit_shaft", RoomCategory.ExitShaft, new Vector2Int(4, 3), true);
        var stairsTemplate = CreateRoomTemplate("exit_stairs", RoomCategory.ExitStairs, new Vector2Int(4, 4), true);

        var validationConfig = new ValidationConfig();
        var outsideTheme = CreateRuntimeAsset<OutsideThemeProfile>("DefaultOutsideTheme");
        var officeRules = CreateRuntimeAsset<OfficeGenerationRules>("DefaultOfficeRules");
        officeRules.Configure(
            FloorStyle.CabinetHeavy,
            System.Array.Empty<FloorStyleProfile>(),
            System.Array.Empty<RoomPlacementRule>(),
            System.Array.Empty<AdjacencyRule>(),
            new[] { outsideTheme },
            2,
            1,
            1,
            1,
            5,
            10f,
            20f);
        var settings = CreateRuntimeAsset<FloorGeneratorSettings>("DefaultFloorSettings");
        settings.Configure(4f, 0.24f, 3.25f, 0.28f, 2, 4, 24, new[]
        {
            startTemplate,
            corridorTemplate,
            hubTemplate,
            officeTemplate,
            storageTemplate,
            utilityTemplate,
            elevatorTemplate,
            shaftTemplate,
            stairsTemplate,
        }, validationConfig, outsideTheme, officeRules);

        var items = new[] { keycard, fuse, tape, repairedFuse, crowbar, rope, lockpick };
        var recipes = new[] { repairFuseRecipe };
        _catalog.Configure(items, recipes, settings);

        ItemLookup.Clear();
        for (var i = 0; i < items.Length; i++)
        {
            ItemLookup[items[i].Id] = items[i];
        }
    }

    public static ItemDefinition GetItem(string itemId)
    {
        EnsureInitialized();
        ItemDefinition item;
        return ItemLookup.TryGetValue(itemId, out item) ? item : null;
    }

    private static RoomTemplate CreateRoomTemplate(string roomId, RoomCategory category, Vector2Int size, bool mandatory)
    {
        var template = CreateRuntimeAsset<RoomTemplate>(roomId);
        template.Configure(roomId, category, size, size, 1f, mandatory);
        return template;
    }

    private static ItemDefinition CreateItem(string id, string displayName, bool stackable, int maxStack, params ItemTag[] tags)
    {
        var item = CreateRuntimeAsset<ItemDefinition>(displayName.Replace(' ', '_'));
        item.Configure(id, displayName, stackable, maxStack, tags);
        return item;
    }

    private static T CreateRuntimeAsset<T>(string name) where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        asset.name = name;
        asset.hideFlags = HideFlags.HideAndDontSave;
        return asset;
    }
}
}

