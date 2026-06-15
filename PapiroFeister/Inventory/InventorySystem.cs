using System;

namespace PapiroFeister.Inventory;

public sealed class InventorySystem
{
    public const int HotbarSize = 8;
    
    private readonly Item[] _hotbarSlots = new Item[HotbarSize];
    private Item[] _backpackSlots;
    private int _upgradeLevel = 0; // 0: +0, 1: +8, 2: +16, 3: +24

    public int SelectedHotbarIndex { get; set; } = 0;
    public int UpgradeLevel => _upgradeLevel;

    public InventorySystem()
    {
        // Base backpack size is 8 slots
        _backpackSlots = new Item[GetBackpackCapacityForLevel(0)];
    }

    public int GetBackpackCapacityForLevel(int level)
    {
        return 8 + level * 8; // 8, 16, 24, 32 slots
    }

    public int GetBackpackAdditionalSlots(int level)
    {
        return level * 8; // +0, +8, +16, +24
    }

    public int BackpackCapacity => _backpackSlots.Length;

    public Item GetHotbarItem(int index)
    {
        if (index < 0 || index >= HotbarSize) return null;
        return _hotbarSlots[index];
    }

    public Item GetBackpackItem(int index)
    {
        if (index < 0 || index >= _backpackSlots.Length) return null;
        return _backpackSlots[index];
    }

    public void SetHotbarItem(int index, Item item)
    {
        if (index >= 0 && index < HotbarSize)
        {
            _hotbarSlots[index] = item;
        }
    }

    public void SetBackpackItem(int index, Item item)
    {
        if (index >= 0 && index < _backpackSlots.Length)
        {
            _backpackSlots[index] = item;
        }
    }

    public void SetUpgradeLevel(int newLevel)
    {
        if (newLevel < 0 || newLevel > 3)
            return;

        int oldCapacity = _backpackSlots.Length;
        int newCapacity = GetBackpackCapacityForLevel(newLevel);
        
        Item[] newBackpack = new Item[newCapacity];
        // Copy elements over
        int copyCount = Math.Min(oldCapacity, newCapacity);
        Array.Copy(_backpackSlots, newBackpack, copyCount);
        
        // If we shrunk the backpack (e.g. cycling upgrade), items in the truncated slots might be lost.
        // But since we only cycle up in gameplay, or loop back for demonstration, let's try to fit lost items
        // back into the remaining slots or hotbar!
        if (newCapacity < oldCapacity)
        {
            for (int i = newCapacity; i < oldCapacity; i++)
            {
                Item lostItem = _backpackSlots[i];
                if (lostItem != null)
                {
                    // Try to re-add it to the new backpack size
                    _backpackSlots = newBackpack; // temporarily assign to check capacity
                    bool added = AddItemInternal(lostItem, newCapacity);
                    if (!added)
                    {
                        // If it doesn't fit, it's dropped/lost
                    }
                    newBackpack = _backpackSlots; // update newBackpack ref
                }
            }
        }

        _backpackSlots = newBackpack;
        _upgradeLevel = newLevel;
    }

    public void CycleUpgrade()
    {
        int nextLevel = (_upgradeLevel + 1) % 4;
        SetUpgradeLevel(nextLevel);
    }

    public bool AddItem(Item item)
    {
        return AddItemInternal(item, _backpackSlots.Length);
    }

    private bool AddItemInternal(Item item, int backpackCapacity)
    {
        if (item == null || item.Quantity <= 0)
            return true;

        // 1. Try to merge into existing stacks (Hotbar first, then Backpack)
        if (item.Type.MaxStack > 1)
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                Item slot = _hotbarSlots[i];
                if (slot != null && slot.Type.Id == item.Type.Id && slot.Quantity < slot.Type.MaxStack)
                {
                    int space = slot.Type.MaxStack - slot.Quantity;
                    int toAdd = Math.Min(space, item.Quantity);
                    slot.Quantity += toAdd;
                    item.Quantity -= toAdd;
                    if (item.Quantity <= 0)
                        return true;
                }
            }

            for (int i = 0; i < backpackCapacity; i++)
            {
                Item slot = _backpackSlots[i];
                if (slot != null && slot.Type.Id == item.Type.Id && slot.Quantity < slot.Type.MaxStack)
                {
                    int space = slot.Type.MaxStack - slot.Quantity;
                    int toAdd = Math.Min(space, item.Quantity);
                    slot.Quantity += toAdd;
                    item.Quantity -= toAdd;
                    if (item.Quantity <= 0)
                        return true;
                }
            }
        }

        // 2. Put in first empty slot in Hotbar
        for (int i = 0; i < HotbarSize; i++)
        {
            if (_hotbarSlots[i] == null)
            {
                _hotbarSlots[i] = item;
                return true;
            }
        }

        // 3. Put in first empty slot in Backpack
        for (int i = 0; i < backpackCapacity; i++)
        {
            if (_backpackSlots[i] == null)
            {
                _backpackSlots[i] = item;
                return true;
            }
        }

        return false; // Inventory full
    }

    public void ClearInventory()
    {
        for (int i = 0; i < HotbarSize; i++)
            _hotbarSlots[i] = null;
        for (int i = 0; i < _backpackSlots.Length; i++)
            _backpackSlots[i] = null;
    }

    public Item RemoveSelectedItem(int quantity = 1)
    {
        Item selected = _hotbarSlots[SelectedHotbarIndex];
        if (selected == null)
            return null;

        int toRemove = Math.Min(quantity, selected.Quantity);
        selected.Quantity -= toRemove;
        
        Item removed = selected.Clone(toRemove);
        
        if (selected.Quantity <= 0)
            _hotbarSlots[SelectedHotbarIndex] = null;

        return removed;
    }

    public int GetItemCount(ItemType type)
    {
        if (type == null) return 0;
        int count = 0;
        for (int i = 0; i < HotbarSize; i++)
        {
            if (_hotbarSlots[i] != null && _hotbarSlots[i].Type.Id == type.Id)
            {
                count += _hotbarSlots[i].Quantity;
            }
        }
        for (int i = 0; i < _backpackSlots.Length; i++)
        {
            if (_backpackSlots[i] != null && _backpackSlots[i].Type.Id == type.Id)
            {
                count += _backpackSlots[i].Quantity;
            }
        }
        return count;
    }

    public bool RemoveItems(ItemType type, int quantity)
    {
        if (type == null || quantity <= 0) return true;
        if (GetItemCount(type) < quantity) return false;

        int remainingToRemove = quantity;

        // Deduct from backpack first
        for (int i = 0; i < _backpackSlots.Length; i++)
        {
            if (_backpackSlots[i] != null && _backpackSlots[i].Type.Id == type.Id)
            {
                if (_backpackSlots[i].Quantity > remainingToRemove)
                {
                    _backpackSlots[i].Quantity -= remainingToRemove;
                    remainingToRemove = 0;
                    break;
                }
                else
                {
                    remainingToRemove -= _backpackSlots[i].Quantity;
                    _backpackSlots[i] = null;
                }
            }
        }

        if (remainingToRemove > 0)
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                if (_hotbarSlots[i] != null && _hotbarSlots[i].Type.Id == type.Id)
                {
                    if (_hotbarSlots[i].Quantity > remainingToRemove)
                    {
                        _hotbarSlots[i].Quantity -= remainingToRemove;
                        remainingToRemove = 0;
                        break;
                    }
                    else
                    {
                        remainingToRemove -= _hotbarSlots[i].Quantity;
                        _hotbarSlots[i] = null;
                    }
                }
            }
        }

        return remainingToRemove == 0;
    }
}
