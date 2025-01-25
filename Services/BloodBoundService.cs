using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace KindredCommands.Services;

/// <summary>
/// Manages blood-bound category.
/// </summary>
public class BloodBoundService
{
	/// <summary>
	/// Initializes a new instance of <see cref="BloodBoundService"/>.
	/// </summary>
	public BloodBoundService()
	{
		SetBloodBound(Core.ConfigSettings.BloodBound);
	}

	/// <summary>
	/// Updates category for every entity in <paramref name="bloodBound"/>.
	/// </summary>
	/// <param name="bloodBound">Key: prefab guid, Value: indicates if entity belongs to blood-bound category.</param>
	public void SetBloodBound(IReadOnlyDictionary<string, bool> bloodBound)
	{
		var defaultValues = new List<string>();
		foreach ((string key, bool value) in bloodBound)
		{
			if (Core.Prefabs.TryGetItem(key, out var prefab))
			{
				if (!SetBloodBound(prefab, value))
				{
					defaultValues.Add(key);
				}
			}
		}

		Core.ConfigSettings.ClearBloodBound(defaultValues);
	}

	/// <summary>
	/// Adds/Remove entity from blood-bound category.
	/// </summary>
	/// <param name="id">Prefab id.</param>
	/// <param name="entity">Entity.</param>
	/// <param name="value">Adds to blood--bound category if true. Otherwise removes.</param>
	/// <returns>True if value changed. False if remains the same.</returns>
	public bool SetBloodBound(PrefabGUID id, Entity entity, bool value)
	{
		// "Item_Weapon_Axe_T01_Bone": -1958888844,
		// Crystals: "Item_Ingredient_Crystal": -257494203
		// Bones:  "Item_Ingredient_Bone": 1821405450,
		// Hat: "Item_Headgear_ArcMageCrown": -2128818978
		var itemMap = Core.GameDataSystem.ItemHashLookupMap;
		var itemData = entity.Read<ItemData>();

		var hasAlready = itemData.ItemCategory.HasFlag(ItemCategory.BloodBound);
		if (hasAlready == value)
		{
			return false;
		}

		if (value)
		{
			itemData.ItemCategory |= ItemCategory.BloodBound;
		}
		else
		{
			itemData.ItemCategory &= ~ItemCategory.BloodBound;
		}

		entity.Write(itemData);
		itemMap[id] = itemData;

		return true;
	}

	/// <summary>
	/// Adds/Remove entity from blood-bound category.
	/// </summary>
	/// <param name="id">Item id.</param>
	/// <param name="value">Adds to blood--bound category if true. Otherwise removes.</param>
	/// <returns>True if value changed. False if remains the same.</returns>
	public bool SetBloodBound(PrefabGUID id, bool value)
	{
		var entity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[id];
		return SetBloodBound(id, entity, value);
	}
}
