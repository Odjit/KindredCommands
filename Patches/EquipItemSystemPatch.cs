using System.Collections;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace KindredCommands.Patches;

// This patch is here to fix the unique weapons while boosted
[HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
public static class EquipItemSystemPatch
{
	public static void Prefix(EquipItemSystem __instance)
	{
		var entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);

		foreach (var entity in entities)
		{
			var fc = entity.Read<FromCharacter>();

			if (fc.Character.Has<VampireAttributeCaps>()) continue;

			var eie = entity.Read<EquipItemEvent>();

			var ab = Core.EntityManager.GetBuffer<AttachedBuffer>(fc.Character);
			var ib = Core.EntityManager.GetBuffer<InventoryBuffer>(ab[0].Entity);

			var item = ib[eie.SlotIndex];
			if (!item.ItemEntity.Equals(NetworkedEntity.Empty) && item.ItemEntity.GetEntityOnServer().Has<LegendaryItemGeneratorTemplate>())
			{
				Core.Log.LogInfo("Unique Item");
				var charEntity = fc.Character;

				// Readd the caps
				if (!charEntity.Has<VampireAttributeCaps>()) charEntity.Add<VampireAttributeCaps>();

				var prefabGuid = charEntity.Read<PrefabGUID>();
				if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefab))
				{
					var caps = prefab.Read<VampireAttributeCaps>();
					charEntity.Write(caps);

					Core.StartCoroutine(RemoveCapsAgain(charEntity));
				}
			}

			Core.Log.LogInfo($"EquipItemEvent {eie.SlotIndex}");
		}
	}

	static IEnumerator RemoveCapsAgain(Entity charEntity)
	{
		yield return null;
		if (charEntity.Has<VampireAttributeCaps>()) charEntity.Remove<VampireAttributeCaps>();
	}
}

