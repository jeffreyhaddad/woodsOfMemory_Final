using UnityEngine;

[System.Serializable]
public class Ingredient
{
    public ItemData item;
    public int quantity;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public Ingredient[] ingredients;
    public ItemData result;
    public int resultQuantity = 1;
    public ItemCategory category;
}
