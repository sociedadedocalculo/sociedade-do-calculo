// Saves the crafting recipe info in a ScriptableObject that can be used ingame
// by referencing it from a MonoBehaviour. It only stores static data.
//
// We also add each one to a dictionary automatically, so that all of them can
// be found by name without having to put them all in a database. Note that we
// have to put them all into the Resources folder and use Resources.LoadAll to
// load them. This is important because some recipes may not be referenced by
// any entity ingame. But all recipes should still be loadable from the
// database, even if they are not referenced by anyone anymore. So we have to
// use Resources.Load. (before we added them to the dict in OnEnable, but that's
// only called for those that are referenced in the game. All others will be
// ignored be Unity.)
//
// A Recipe can be created by right clicking the Resources folder and selecting
// Create -> uMMORPG Recipe. Existing recipes can be found in the Resources
// folder.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName="New Recipe", menuName="uMMORPG Recipe", order=999)]
public class ScriptableRecipe : ScriptableObject
{
    // fixed ingredient size for all recipes
    public static int recipeSize = 6;

    // ingredients and result
    public List<ScriptableItemAndAmount> ingredients = new List<ScriptableItemAndAmount>(6);
    public ScriptableItem result;

    // crafting time in seconds
    public float craftingTime = 1;

    // probability of success
    [Range(0, 1)] public float probability = 1;

    // check if the list of items works for this recipe. the list shouldn't
    // contain 'null'.
    // (inheriting classes can modify the matching algorithm if needed)
    public virtual bool CanCraftWith(List<ItemSlot> items)
    {
        // items list should not be touched, since it's often used to check more
        // than one recipe. so let's just create a local copy.
        items = new List<ItemSlot>(items);

        // make sure that we have at least one ingredient
        if (ingredients.Any(slot => slot.amount > 0 && slot.item != null))
        {
            // each ingredient in the list, with amount?
            foreach (ScriptableItemAndAmount ingredient in ingredients)
            {
                if (ingredient.amount > 0 && ingredient.item != null)
                {
                    // is there a stack with at least that amount and that item?
                    int index = items.FindIndex(slot => slot.amount >= ingredient.amount && slot.item.data == ingredient.item);
                    if (index != -1)
                        items.RemoveAt(index);
                    else
                        return false;
                }
            }

            // and nothing else in the list?
            return items.Count == 0;
        }
        else return false;
    }

    // caching /////////////////////////////////////////////////////////////////
    // we can only use Resources.Load in the main thread. we can't use it when
    // declaring static variables. so we have to use it as soon as 'dict' is
    // accessed for the first time from the main thread.
    static Dictionary<string, ScriptableRecipe> cache;
    public static Dictionary<string, ScriptableRecipe> dict
    {
        get
        {
            // not loaded yet?
            if (cache == null)
            {
                // get all ScriptableRecipes in resources
                ScriptableRecipe[] recipes = Resources.LoadAll<ScriptableRecipe>("");

                // check for duplicates, then add to cache
                List<string> duplicates = recipes.ToList().FindDuplicates(recipe => recipe.name);
                if (duplicates.Count == 0)
                {
                    cache = recipes.ToDictionary(recipe => recipe.name, recipe => recipe);
                }
                else
                {
                    foreach (string duplicate in duplicates)
                        Debug.LogError("Resources folder contains multiple ScriptableRecipes with the name " + duplicate + ". If you are using subfolders like 'Warrior/Ring' and 'Archer/Ring', then rename them to 'Warrior/(Warrior)Ring' and 'Archer/(Archer)Ring' instead.");
                }
            }
            return cache;
        }
    }

    // validation //////////////////////////////////////////////////////////////
    void OnValidate()
    {
        // force list size
        // -> add if too few
        for (int i = ingredients.Count; i < recipeSize; ++i)
            ingredients.Add(new ScriptableItemAndAmount());

        // -> remove if too many
        for (int i = recipeSize; i < ingredients.Count; ++i)
            ingredients.RemoveAt(i);
    }
}
