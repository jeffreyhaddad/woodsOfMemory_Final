using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    private ItemData equippedWeapon;
    private ItemData equippedTool;
    private ItemData equippedArmor;

    public ItemData EquippedWeapon => equippedWeapon;
    public ItemData EquippedTool => equippedTool;
    public ItemData EquippedArmor => equippedArmor;

    /// <summary>Total bonus damage from equipped weapon.</summary>
    public float WeaponDamageBonus => equippedWeapon != null ? equippedWeapon.damageBonus : 0f;

    /// <summary>Flat damage reduction from equipped armor.</summary>
    public float ArmorDefenseBonus => equippedArmor != null ? equippedArmor.defenseBonus : 0f;

    /// <summary>Fired when any equipment slot changes.</summary>
    public event Action OnEquipmentChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Equip an item into its slot. Returns the previously equipped item (or null).
    /// If the same item is already equipped, unequips it.
    /// </summary>
    public ItemData Equip(ItemData item)
    {
        if (item == null || item.equipSlot == EquipSlot.None)
            return null;

        ItemData previous = null;

        switch (item.equipSlot)
        {
            case EquipSlot.Weapon:
                if (equippedWeapon != null && equippedWeapon.itemName == item.itemName)
                {
                    equippedWeapon = null;
                    OnEquipmentChanged?.Invoke();
                    return null;
                }
                previous = equippedWeapon;
                equippedWeapon = item;
                break;

            case EquipSlot.Tool:
                if (equippedTool != null && equippedTool.itemName == item.itemName)
                {
                    equippedTool = null;
                    OnEquipmentChanged?.Invoke();
                    return null;
                }
                previous = equippedTool;
                equippedTool = item;
                break;

            case EquipSlot.Armor:
                if (equippedArmor != null && equippedArmor.itemName == item.itemName)
                {
                    equippedArmor = null;
                    OnEquipmentChanged?.Invoke();
                    return null;
                }
                previous = equippedArmor;
                equippedArmor = item;
                break;
        }

        OnEquipmentChanged?.Invoke();
        return previous;
    }

    /// <summary>Unequip a specific item if it's currently equipped.</summary>
    public void Unequip(ItemData item)
    {
        if (item == null) return;

        if (equippedWeapon != null && equippedWeapon.itemName == item.itemName)
            equippedWeapon = null;
        else if (equippedTool != null && equippedTool.itemName == item.itemName)
            equippedTool = null;
        else if (equippedArmor != null && equippedArmor.itemName == item.itemName)
            equippedArmor = null;
        else
            return;

        OnEquipmentChanged?.Invoke();
    }

    /// <summary>Check if an item is currently equipped (by name match).</summary>
    public bool IsEquipped(ItemData item)
    {
        if (item == null) return false;
        string name = item.itemName;
        return (equippedWeapon != null && equippedWeapon.itemName == name) ||
               (equippedTool != null && equippedTool.itemName == name) ||
               (equippedArmor != null && equippedArmor.itemName == name);
    }

    /// <summary>Clear all equipment slots.</summary>
    public void UnequipAll()
    {
        equippedWeapon = null;
        equippedTool = null;
        equippedArmor = null;
        OnEquipmentChanged?.Invoke();
    }
}
