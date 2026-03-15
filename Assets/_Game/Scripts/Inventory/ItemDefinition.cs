using System;
using UnityEngine;

namespace MurinoHDR.Inventory
{

public enum ItemTag
{
    None = 0,
    Keycard = 1,
    Fuse = 2,
    Tape = 3,
    Tool = 4,
    Rope = 5,
    Lockpick = 6,
    RepairedFuse = 7,
}

[Serializable]
public struct RecipeIngredient
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private int _count;

    public ItemDefinition Item => _item;
    public int Count => Mathf.Max(1, _count);

    public void Configure(ItemDefinition item, int count)
    {
        _item = item;
        _count = Mathf.Max(1, count);
    }
}

public sealed class ItemDefinition : ScriptableObject
{
    [SerializeField] private string _id = string.Empty;
    [SerializeField] private string _displayName = string.Empty;
    [SerializeField] private Sprite _icon;
    [SerializeField] private bool _stackable = true;
    [SerializeField] private int _maxStack = 1;
    [SerializeField] private ItemTag[] _tags = Array.Empty<ItemTag>();

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public bool Stackable => _stackable;
    public int MaxStack => Mathf.Max(1, _maxStack);
    public ItemTag[] Tags => _tags;

    public void Configure(string id, string displayName, bool stackable, int maxStack, params ItemTag[] tags)
    {
        _id = id;
        _displayName = displayName;
        _stackable = stackable;
        _maxStack = Mathf.Max(1, maxStack);
        _tags = tags ?? Array.Empty<ItemTag>();
    }

    public bool HasTag(ItemTag tag)
    {
        if (_tags == null)
        {
            return false;
        }

        for (var i = 0; i < _tags.Length; i++)
        {
            if (_tags[i] == tag)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class CraftRecipe : ScriptableObject
{
    [SerializeField] private string _id = string.Empty;
    [SerializeField] private string _displayName = string.Empty;
    [SerializeField] private RecipeIngredient[] _inputs = Array.Empty<RecipeIngredient>();
    [SerializeField] private ItemDefinition _output;
    [SerializeField] private int _outputCount = 1;

    public string Id => _id;
    public string DisplayName => _displayName;
    public RecipeIngredient[] Inputs => _inputs;
    public ItemDefinition Output => _output;
    public int OutputCount => Mathf.Max(1, _outputCount);

    public void Configure(string id, string displayName, RecipeIngredient[] inputs, ItemDefinition output, int outputCount)
    {
        _id = id;
        _displayName = displayName;
        _inputs = inputs ?? Array.Empty<RecipeIngredient>();
        _output = output;
        _outputCount = Mathf.Max(1, outputCount);
    }
}
}
