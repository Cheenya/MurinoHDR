using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class GenerationDefinitions
{
    private static readonly Dictionary<RoomType, RoomTypeDef> RoomDefs = new Dictionary<RoomType, RoomTypeDef>();
    private static bool _initialized;

    public static RoomTypeDef GetRoomDef(RoomType type)
    {
        EnsureInitialized();
        RoomTypeDef definition;
        return RoomDefs.TryGetValue(type, out definition) ? definition : null;
    }

    public static IEnumerable<RoomTypeDef> GetRoomDefs()
    {
        EnsureInitialized();
        return RoomDefs.Values;
    }

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Register(CreateKitchenBreak());
        Register(CreateOpenSpace());
        Register(CreateWarehouse());
        Register(CreateTechCorridor());
        Register(CreateVentSegment());
        Register(CreateMeetingRoom());
        Register(CreateSecurityRoom());
        Register(CreateServerRoom());
        Register(CreatePrintCopyRoom());
        Register(CreateRestroom());
        Register(CreateManagerOffice());
        Register(CreateReception());
        Register(CreateStartCheckpoint());
        Register(CreateElevatorLobby());
        Register(CreateStairsLobby());
        Register(CreateShaftAccess());
    }

    private static void Register(RoomTypeDef definition)
    {
        if (definition != null)
        {
            RoomDefs[definition.Type] = definition;
        }
    }

    private static RoomTypeDef CreateKitchenBreak()
    {
        var patternA = CreatePattern(
            "kitchen_counter_tables",
            AnchorPlan.AlongWall,
            new[]
            {
                Req(PropCategory.KitchenCounter, 1, 2, true, PlacementHint.AlongWall),
                Req(PropCategory.Microwave, 1, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.CoffeeMachine, 1, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.Fridge, 1, 1, true, PlacementHint.Corner),
                Req(PropCategory.TrashBin, 1, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.TableSmall, 1, 2, true, PlacementHint.Center),
                Req(PropCategory.Chair, 2, 6, false, PlacementHint.Center),
            },
            new[]
            {
                Req(PropCategory.Sink, 0, 1, true, PlacementHint.AlongWall),
                Req(PropCategory.VendingMachine, 0, 1, true, PlacementHint.Corner),
                Req(PropCategory.WaterCooler, 0, 1, false, PlacementHint.NearWindow),
                Req(PropCategory.PlantSmall, 0, 2, false, PlacementHint.NearWindow),
                Req(PropCategory.DecalPaper, 0, 2, false, PlacementHint.Center),
            },
            4);

        var patternB = CreatePattern(
            "kitchen_lounge",
            AnchorPlan.Center,
            new[]
            {
                Req(PropCategory.KitchenCounter, 1, 2, true, PlacementHint.AlongWall),
                Req(PropCategory.CoffeeMachine, 1, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.Fridge, 1, 1, true, PlacementHint.Corner),
                Req(PropCategory.Sofa, 1, 1, true, PlacementHint.Center),
                Req(PropCategory.TableSmall, 1, 1, true, PlacementHint.Center),
                Req(PropCategory.Armchair, 1, 2, true, PlacementHint.Center),
                Req(PropCategory.TrashBin, 1, 1, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.WaterCooler, 0, 1, false, PlacementHint.NearWindow),
                Req(PropCategory.PlantSmall, 0, 2, false, PlacementHint.NearWindow),
            },
            3);

        return CreateRoomDef(
            RoomType.KitchenBreak,
            RoomTags.FacadePreferred | RoomTags.FacadeRoom | RoomTags.RequiresWindow | RoomTags.SafeSpotCandidate | RoomTags.SafeSpot,
            4f, 9f, 4f, 7f,
            0.95f, 0.1f, true, 1, 2,
            new[] { patternA, patternB });
    }

    private static RoomTypeDef CreateOpenSpace()
    {
        var patternA = CreatePattern(
            "open_islands",
            AnchorPlan.IslandGrid,
            new[]
            {
                Req(PropCategory.Desk, 2, 4, true, PlacementHint.Island),
                Req(PropCategory.Chair, 2, 4, false, PlacementHint.Island),
                Req(PropCategory.Monitor, 1, 4, false, PlacementHint.Island),
                Req(PropCategory.Cabinet, 1, 1, true, PlacementHint.AlongWall),
                Req(PropCategory.TrashBin, 1, 1, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.DeskDivider, 0, 2, false, PlacementHint.Island),
                Req(PropCategory.PlantSmall, 0, 3, false, PlacementHint.NearWindow),
                Req(PropCategory.PlantLarge, 0, 1, true, PlacementHint.NearWindow),
                Req(PropCategory.Whiteboard, 0, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.WaterCooler, 0, 1, false, PlacementHint.AlongWall),
            },
            5);

        var patternB = CreatePattern(
            "open_rows",
            AnchorPlan.AlongWall,
            new[]
            {
                Req(PropCategory.Desk, 3, 6, true, PlacementHint.AlongWall),
                Req(PropCategory.Chair, 3, 6, false, PlacementHint.AlongWall),
                Req(PropCategory.Monitor, 2, 6, false, PlacementHint.AlongWall),
                Req(PropCategory.Shelf, 1, 1, true, PlacementHint.AlongWall),
                Req(PropCategory.TrashBin, 1, 1, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.PlantSmall, 0, 2, false, PlacementHint.NearWindow),
                Req(PropCategory.Sofa, 0, 1, true, PlacementHint.Center),
                Req(PropCategory.TableSmall, 0, 1, true, PlacementHint.Center),
            },
            4);

        return CreateRoomDef(
            RoomType.OpenSpace,
            RoomTags.FacadePreferred | RoomTags.FacadeRoom | RoomTags.RequiresWindow | RoomTags.SafeSpotCandidate | RoomTags.MainPath,
            6f, 14f, 5f, 12f,
            0.9f, 0.1f, true, 1, 4,
            new[] { patternA, patternB });
    }

    private static RoomTypeDef CreateWarehouse()
    {
        var patternA = CreatePattern(
            "warehouse_parallel",
            AnchorPlan.IslandGrid,
            new[]
            {
                Req(PropCategory.Rack, 2, 4, true, PlacementHint.Island),
                Req(PropCategory.Pallet, 2, 5, true, PlacementHint.Island),
                Req(PropCategory.Box, 8, 16, false, PlacementHint.Island),
                Req(PropCategory.WarningSign, 1, 2, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.Container, 0, 2, true, PlacementHint.Corner),
                Req(PropCategory.HandTruck, 0, 1, true, PlacementHint.AlongWall),
                Req(PropCategory.TableSmall, 0, 1, true, PlacementHint.AlongWall),
            },
            3);

        return CreateRoomDef(
            RoomType.Warehouse,
            RoomTags.CorePreferred | RoomTags.Support | RoomTags.ResourceRoom | RoomTags.LootSource,
            4f, 9f, 4f, 8f,
            0.1f, 0.95f, false, 1, 3,
            new[] { patternA });
    }

    private static RoomTypeDef CreateTechCorridor()
    {
        var pattern = CreatePattern(
            "tech_corridor_strip",
            AnchorPlan.CeilingLine,
            new[]
            {
                Req(PropCategory.Pipe, 2, 5, false, PlacementHint.AlongWall),
                Req(PropCategory.Duct, 1, 3, false, PlacementHint.AlongWall),
                Req(PropCategory.CableTray, 1, 2, false, PlacementHint.AlongWall),
                Req(PropCategory.VentGrate, 1, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.MaintenanceSign, 1, 1, false, PlacementHint.NearDoor),
            },
            new[]
            {
                Req(PropCategory.WarningSign, 0, 2, false, PlacementHint.NearDoor),
                Req(PropCategory.Ladder, 0, 1, true, PlacementHint.AlongWall),
            },
            2);

        return CreateRoomDef(
            RoomType.TechCorridor,
            RoomTags.CorePreferred | RoomTags.Support | RoomTags.Tech | RoomTags.Corridor | RoomTags.ChokepointsAllowed,
            2f, 8f, 2f, 8f,
            0.05f, 1f, false, 1, 4,
            new[] { pattern });
    }

    private static RoomTypeDef CreateVentSegment()
    {
        var pattern = CreatePattern(
            "vent_segment",
            AnchorPlan.FloorLine,
            new[]
            {
                Req(PropCategory.Duct, 1, 2, false, PlacementHint.AlongWall),
                Req(PropCategory.Hatch, 1, 1, false, PlacementHint.NearDoor),
                Req(PropCategory.MaintenanceSign, 1, 1, false, PlacementHint.NearDoor),
            },
            new[]
            {
                Req(PropCategory.WarningSign, 0, 1, false, PlacementHint.NearDoor),
            },
            1);

        return CreateRoomDef(
            RoomType.VentSegment,
            RoomTags.CorePreferred | RoomTags.Tech | RoomTags.Support | RoomTags.ChokepointsAllowed,
            2f, 5f, 2f, 5f,
            0.05f, 1f, false, 0, 2,
            new[] { pattern });
    }

    private static RoomTypeDef CreateReception()
    {
        var pattern = CreatePattern(
            "reception_minimal",
            AnchorPlan.Center,
            new[]
            {
                Req(PropCategory.ReceptionDesk, 1, 1, true, PlacementHint.Center),
                Req(PropCategory.WaterCooler, 1, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.EvacPlan, 1, 1, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.PlantLarge, 0, 1, true, PlacementHint.NearWindow),
                Req(PropCategory.Turnstile, 0, 1, true, PlacementHint.NearDoor),
            },
            2);

        return CreateRoomDef(
            RoomType.Reception,
            RoomTags.FacadePreferred | RoomTags.FacadeRoom | RoomTags.SafeSpotCandidate | RoomTags.LogicRoom,
            5f, 10f, 4f, 8f,
            0.8f, 0.2f, true, 1, 1,
            new[] { pattern });
    }

    private static RoomTypeDef CreateMeetingRoom()
    {
        var pattern = CreatePattern(
            "meeting_table_board",
            AnchorPlan.Center,
            new[]
            {
                Req(PropCategory.TableLarge, 1, 1, true, PlacementHint.Center),
                Req(PropCategory.Chair, 4, 8, false, PlacementHint.Center),
                Req(PropCategory.Whiteboard, 1, 1, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.TV, 0, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.PlantSmall, 0, 2, false, PlacementHint.NearWindow),
                Req(PropCategory.WaterCooler, 0, 1, false, PlacementHint.AlongWall),
            },
            3);

        return CreateRoomDef(
            RoomType.MeetingRoom,
            RoomTags.FacadePreferred | RoomTags.FacadeRoom | RoomTags.RequiresWindow | RoomTags.LogicRoom,
            4f, 8f, 4f, 8f,
            0.8f, 0.2f, true, 0, 2,
            new[] { pattern });
    }

    private static RoomTypeDef CreateSecurityRoom()
    {
        var pattern = CreatePattern(
            "security_console",
            AnchorPlan.AlongWall,
            new[]
            {
                Req(PropCategory.Desk, 1, 2, true, PlacementHint.AlongWall),
                Req(PropCategory.Monitor, 2, 4, false, PlacementHint.AlongWall),
                Req(PropCategory.Cabinet, 1, 1, true, PlacementHint.Corner),
                Req(PropCategory.WarningSign, 1, 1, false, PlacementHint.NearDoor),
            },
            new[]
            {
                Req(PropCategory.WaterCooler, 0, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.EvacPlan, 0, 1, false, PlacementHint.NearDoor),
            },
            2);

        return CreateRoomDef(
            RoomType.SecurityRoom,
            RoomTags.CorePreferred | RoomTags.LogicRoom | RoomTags.Support,
            3f, 6f, 3f, 6f,
            0.2f, 0.85f, false, 0, 1,
            new[] { pattern });
    }

    private static RoomTypeDef CreateServerRoom()
    {
        var pattern = CreatePattern(
            "server_racks",
            AnchorPlan.AlongWall,
            new[]
            {
                Req(PropCategory.ServerRack, 2, 4, true, PlacementHint.AlongWall),
                Req(PropCategory.NetworkCabinet, 1, 1, true, PlacementHint.Corner),
                Req(PropCategory.UPS, 1, 2, true, PlacementHint.AlongWall),
                Req(PropCategory.CableTray, 1, 2, false, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.WarningSign, 0, 2, false, PlacementHint.NearDoor),
                Req(PropCategory.ACUnit, 0, 1, true, PlacementHint.AlongWall),
            },
            2);

        return CreateRoomDef(
            RoomType.ServerRoom,
            RoomTags.CorePreferred | RoomTags.Support | RoomTags.Tech | RoomTags.ResourceRoom,
            3f, 7f, 3f, 7f,
            0.05f, 0.95f, false, 0, 2,
            new[] { pattern });
    }

    private static RoomTypeDef CreatePrintCopyRoom()
    {
        var pattern = CreatePattern(
            "copy_corner",
            AnchorPlan.AlongWall,
            new[]
            {
                Req(PropCategory.PrinterCopier, 1, 1, true, PlacementHint.AlongWall),
                Req(PropCategory.DocumentStack, 1, 3, false, PlacementHint.AlongWall),
                Req(PropCategory.Cabinet, 1, 1, true, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.TrashBin, 0, 1, false, PlacementHint.AlongWall),
                Req(PropCategory.PlantSmall, 0, 1, false, PlacementHint.NearWindow),
            },
            2);

        return CreateRoomDef(
            RoomType.PrintCopyRoom,
            RoomTags.Support | RoomTags.LootSource,
            3f, 6f, 3f, 6f,
            0.35f, 0.5f, false, 0, 2,
            new[] { pattern });
    }

    private static RoomTypeDef CreateRestroom()
    {
        var pattern = CreatePattern(
            "restroom_compact",
            AnchorPlan.AlongWall,
            new[]
            {
                Req(PropCategory.Sink, 1, 2, true, PlacementHint.AlongWall),
                Req(PropCategory.WarningSign, 1, 1, false, PlacementHint.NearDoor),
            },
            new[]
            {
                Req(PropCategory.TrashBin, 0, 1, false, PlacementHint.AlongWall),
            },
            1);

        return CreateRoomDef(
            RoomType.Restroom,
            RoomTags.CorePreferred | RoomTags.Support,
            3f, 5f, 3f, 5f,
            0.05f, 0.95f, false, 0, 2,
            new[] { pattern });
    }

    private static RoomTypeDef CreateManagerOffice()
    {
        var pattern = CreatePattern(
            "manager_office",
            AnchorPlan.Center,
            new[]
            {
                Req(PropCategory.Desk, 1, 1, true, PlacementHint.Center),
                Req(PropCategory.Chair, 2, 3, false, PlacementHint.Center),
                Req(PropCategory.Monitor, 1, 2, false, PlacementHint.Center),
                Req(PropCategory.Cabinet, 1, 1, true, PlacementHint.AlongWall),
            },
            new[]
            {
                Req(PropCategory.Sofa, 0, 1, true, PlacementHint.NearWindow),
                Req(PropCategory.TableSmall, 0, 1, true, PlacementHint.NearWindow),
                Req(PropCategory.PlantLarge, 0, 1, true, PlacementHint.NearWindow),
            },
            3);

        return CreateRoomDef(
            RoomType.ManagerOffice,
            RoomTags.FacadePreferred | RoomTags.FacadeRoom | RoomTags.RequiresWindow | RoomTags.LogicRoom,
            4f, 7f, 4f, 7f,
            0.85f, 0.25f, true, 0, 1,
            new[] { pattern });
    }

    private static RoomTypeDef CreateStartCheckpoint()
    {
        return CreateRoomDef(
            RoomType.StartCheckpoint,
            RoomTags.Checkpoint | RoomTags.SafeSpot | RoomTags.LogicRoom | RoomTags.Landmark,
            4f, 8f, 4f, 8f,
            0.7f, 0.2f, false, 1, 1,
            new PropPatternDef[0]);
    }

    private static RoomTypeDef CreateElevatorLobby()
    {
        return CreateRoomDef(
            RoomType.ElevatorLobby,
            RoomTags.CorePreferred | RoomTags.LogicRoom | RoomTags.Landmark,
            4f, 8f, 3f, 6f,
            0.1f, 0.9f, false, 1, 1,
            new PropPatternDef[0]);
    }

    private static RoomTypeDef CreateStairsLobby()
    {
        return CreateRoomDef(
            RoomType.StairsLobby,
            RoomTags.CorePreferred | RoomTags.Landmark,
            4f, 8f, 3f, 6f,
            0.1f, 0.9f, false, 1, 1,
            new PropPatternDef[0]);
    }

    private static RoomTypeDef CreateShaftAccess()
    {
        return CreateRoomDef(
            RoomType.ShaftAccess,
            RoomTags.CorePreferred | RoomTags.Tech | RoomTags.ChokepointsAllowed,
            4f, 8f, 3f, 6f,
            0.05f, 1f, false, 1, 1,
            new[] { CreateVentSegment().Patterns[0].Pattern });
    }

    private static RoomTypeDef CreateRoomDef(
        RoomType type,
        RoomTags tags,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        float facadePreference,
        float corePreference,
        bool requiresWindow,
        int minPerFloor,
        int maxPerFloor,
        IList<PropPatternDef> patterns)
    {
        var definition = ScriptableObject.CreateInstance<RoomTypeDef>();
        definition.name = type + "Def";
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.Configure(type, tags, new Vector2(minX, maxX), new Vector2(minZ, maxZ), facadePreference, corePreference, requiresWindow, minPerFloor, maxPerFloor, 1f, 1f, 1f, 1f, patterns);
        return definition;
    }

    private static PropPatternDef CreatePattern(string patternId, AnchorPlan anchorPlan, IList<PropRequirement> required, IList<PropRequirement> optional, int optionalBudget)
    {
        var pattern = ScriptableObject.CreateInstance<PropPatternDef>();
        pattern.name = patternId;
        pattern.hideFlags = HideFlags.HideAndDontSave;
        pattern.Configure(patternId, 1f, anchorPlan, new ClearanceConfig(), required, optional, optionalBudget, new AntiRepeatPolicy(), new PropPlacementRules());
        return pattern;
    }

    private static PropRequirement Req(PropCategory category, int minCount, int maxCount, bool blocksMovement, PlacementHint hint)
    {
        return new PropRequirement
        {
            Category = category,
            MinCount = minCount,
            MaxCount = Mathf.Max(minCount, maxCount),
            BlocksMovement = blocksMovement,
            PlacementHint = hint,
            PreferNearWindow = hint == PlacementHint.NearWindow,
        };
    }
}
}
