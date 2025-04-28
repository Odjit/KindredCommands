using HarmonyLib;
using KindredCommands.Data;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;

namespace KindredCommands.Patches;

[HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
internal class AbilityRunScriptsSystemPatch
{
	public static void Prefix(AbilityRunScriptsSystem __instance)
	{
		var entities = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			var acse = entity.Read<AbilityCastStartedEvent>();
			if (!Core.ConfigSettings.SoulshardsFlightRestricted && acse.Ability.Read<PrefabGUID>() == Prefabs.AB_Shapeshift_Bat_TakeFlight_Cast)
				Core.GearService.SetShardsRestricted(false);
		}
		entities.Dispose();
	}

	public static void Postfix(AbilityRunScriptsSystem __instance)
	{
		var entities = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			var acse = entity.Read<AbilityCastStartedEvent>();
			if (!Core.ConfigSettings.SoulshardsFlightRestricted && acse.Ability.Read<PrefabGUID>() == Prefabs.AB_Shapeshift_Bat_TakeFlight_Cast)
				Core.GearService.SetShardsRestricted(true);
		}
		entities.Dispose();
	}
}

[HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
internal class ReplaceAbilityOnSlotSystemPatch
{
	public static void Prefix(ReplaceAbilityOnSlotSystem __instance)
	{
		var entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			var raosb = entity.Read<ReplaceAbilityOnSlotBuff>();
			Core.Log.LogInfo($"ReplaceAbilityOnSlotBuff: {raosb.Target} Slot {raosb.Slot} ReplaceGroupId {raosb.ReplaceGroupId.LookupName()} NewGroupId {raosb.NewGroupId.LookupName()} Priority {raosb.Priority} CastBlockType {raosb.CastBlockType} CopyCooldown {raosb.CopyCooldown}");
		}
		entities.Dispose();
	}
}
