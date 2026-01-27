using ProjectM.Network;
using ProjectM;
using Unity.Entities;
using ProjectM.Shared;
using Stunlock.Core;

namespace KindredCommands;
internal class Buffs
{
	public static bool TryAddBuff(Entity user, Entity character, PrefabGUID buffPrefab, int duration = 0, bool immortal = true)
	{
		if (BuffUtility.HasBuff(Core.Server.EntityManager, character, buffPrefab.ToIdentifier()))
		{
			return false;
		}
		return TryAddOrUpdateBuff(user, character, buffPrefab, duration, immortal);
	}
	
	public static bool TryAddOrUpdateBuff(Entity user, Entity character, PrefabGUID buffPrefab, int duration = 0, bool immortal = true)
	{
		if (!TryGetOrCreateBuff(user, character, buffPrefab, out var buffEntity))
		{
			return false;
		}
		UpdateBuff(buffEntity, duration, immortal);
		return true;
	}

	public static bool TryGetOrCreateBuff(Entity User, Entity Character, PrefabGUID buffPrefab, out Entity buffEntity)
	{
		if (BuffUtility.TryGetBuff(Core.Server.EntityManager, Character, buffPrefab, out buffEntity))
		{
			return true;
		}

		var des = Core.Server.GetExistingSystemManaged<DebugEventsSystem>();
		var buffEvent = new ApplyBuffDebugEvent()
		{
			BuffPrefabGUID = buffPrefab
		};
		var fromCharacter = new FromCharacter()
		{
			User = User,
			Character = Character
		};
		des.ApplyBuff(fromCharacter, buffEvent);

		if (!BuffUtility.TryGetBuff(Core.Server.EntityManager, Character, buffPrefab, out buffEntity))
		{
			return false;
		}

		if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
		{
			buffEntity.Remove<CreateGameplayEventsOnSpawn>();
		}
		if (buffEntity.Has<GameplayEventListeners>())
		{
			buffEntity.Remove<GameplayEventListeners>();
		}
		return true;
	}	

	public static void UpdateBuff(Entity buffEntity, int duration = 0, bool immortal = true)
	{
		ResetBuffDuration(buffEntity, duration);
		UpdateBuffPersistThroughDeath(buffEntity, immortal);
	}

	public static void UpdateBuffPersistThroughDeath(Entity buffEntity, bool shouldPersistThroughDeath)
	{
		if (shouldPersistThroughDeath)
		{
			buffEntity.Add<Buff_Persists_Through_Death>();
			if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEvent>();
			}

			if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
			}
		}
		else
		{
			buffEntity.Remove<Buff_Persists_Through_Death>();
		}
	}

	public static void ResetBuffDuration(Entity buffEntity, int duration = 0)
	{
		float age = 0;
		if (Core.EntityManager.TryGetComponentData<Age>(buffEntity, out var ageData))
		{
			age = ageData.Value;
		}

		if (duration > -1 && duration != 0)
		{
			if (!buffEntity.Has<LifeTime>())
			{
				buffEntity.Add<LifeTime>();
			}
			var lifetime = buffEntity.Read<LifeTime>();
			lifetime.Duration = duration + age;
			if (lifetime.EndAction == LifeTimeEndAction.None)
			{
				lifetime.EndAction = LifeTimeEndAction.Destroy;
			}
			buffEntity.Write(lifetime);
		}
		else if (duration == -1)
		{
			if (buffEntity.Has<LifeTime>())
			{
				var lifetime = buffEntity.Read<LifeTime>();
				lifetime.EndAction = LifeTimeEndAction.None;
				buffEntity.Write(lifetime);
			}
			if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEvent>();
			}
			if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
			{
				buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
			}
		}
	}

	public static void RemoveBuff(Entity Character, PrefabGUID buffPrefab)
	{
		if (BuffUtility.TryGetBuff(Core.EntityManager, Character, buffPrefab, out var buffEntity))
		{
			DestroyUtility.Destroy(Core.EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
		}
	}
}
