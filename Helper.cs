using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using KindredCommands.Data;
using KindredCommands.Models;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay.Clan;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;

namespace KindredCommands;

// This is an anti-pattern, move stuff away from Helper not into it
internal static partial class Helper
{
	public static AdminAuthSystem adminAuthSystem = Core.Server.GetExistingSystemManaged<AdminAuthSystem>();
	public static ClanSystem_Server clanSystem = Core.Server.GetExistingSystemManaged<ClanSystem_Server>();
	public static EntityCommandBufferSystem entityCommandBufferSystem = Core.Server.GetExistingSystemManaged<EntityCommandBufferSystem>();

	public static PrefabGUID GetPrefabGUID(Entity entity)
	{
		var entityManager = Core.EntityManager;
		PrefabGUID guid;
		try
		{
			guid = entityManager.GetComponentData<PrefabGUID>(entity);
		}
		catch
		{
			guid = new PrefabGUID(0);
		}
		return guid;
	}

	public static bool TryGetClanEntityFromPlayer(Entity User, out Entity ClanEntity)
	{
		if (User.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>().Length > 0)
		{
			ClanEntity = User.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>()[0].Value;
			return true;
		}
		ClanEntity = new Entity();
		return false;
	}

	public static Entity AddItemToInventory(Entity recipient, PrefabGUID guid, int amount)
	{
		try
		{
			ServerGameManager serverGameManager = Core.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
			var inventoryResponse = serverGameManager.TryAddInventoryItem(recipient, guid, amount);

			return inventoryResponse.NewEntity;
		}
		catch (System.Exception e)
		{
			Core.LogException(e);
		}
		return new Entity();
	}

	public static int AddBloodPotionToInventory(Entity recipient, BloodType type = BloodType.Frailed, float quality = 100f, int quantity = 1)
	{
		var countAdded = 0;
		while (countAdded < quantity)
		{
			Entity addedItemEntity = AddItemToInventory(recipient, new PrefabGUID(1223264867), 1);
			if (addedItemEntity == Entity.Null)
			{
				break;
			}

			var blood = new StoredBlood()
			{
				BloodQuality = quality,
				PrimaryBloodType = new PrefabGUID((int)type)
			};

			Core.EntityManager.SetComponentData(addedItemEntity, blood);
			countAdded++;
		}
		return countAdded;
	}

	public static int AddBloodMerlotMixToInventory(Entity recipient, BloodType primaryType = BloodType.Frailed, float primaryQuality = 100f, BloodType secondaryType = BloodType.Frailed, float secondaryQuality = 100f, int secondaryTrait = 1, int quantity = 1)
	{
		var countAdded = 0;
		while (countAdded < quantity)
		{
			Entity addedItemEntity = AddItemToInventory(recipient, new PrefabGUID(1223264867), 1);
			if (addedItemEntity == Entity.Null)
			{
				break;
			}

			var blood = new StoredBlood()
			{
				BloodQuality = primaryQuality,
				PrimaryBloodType = new PrefabGUID((int)primaryType),
				SecondaryBlood = new()
				{
					Quality = secondaryQuality,
					Type = new PrefabGUID((int)secondaryType),
					BuffIndex = (byte)(secondaryTrait - 1)
				}
			};

			Core.EntityManager.SetComponentData(addedItemEntity, blood);
			countAdded++;
		}
		return countAdded;
	}

	public static NativeArray<Entity> GetEntitiesByComponentType<T1>(bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false)
	{
		EntityQueryOptions options = EntityQueryOptions.Default;
		if (includeAll) options |= EntityQueryOptions.IncludeAll;
		if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
		if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
		if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
		if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

		var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite))
			.WithOptions(options);

		var query = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);

		var entities = query.ToEntityArray(Allocator.Temp);
		return entities;
	}

	public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2>(bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false)
	{
		EntityQueryOptions options = EntityQueryOptions.Default;
		if (includeAll) options |= EntityQueryOptions.IncludeAll;
		if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
		if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
		if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
		if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

		var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite))
			.AddAll(new(Il2CppType.Of<T2>(), ComponentType.AccessMode.ReadWrite))
			.WithOptions(options);

		var query = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);

		var entities = query.ToEntityArray(Allocator.Temp);
		return entities;
	}

	public static IEnumerable<Entity> GetAllEntitiesInRadius<T>(float2 center, float radius)
	{
		var spatialData = Core.GenerateCastle._TileModelLookupSystemData;
		var tileModelSpatialLookupRO = spatialData.GetSpatialLookupReadOnlyAndComplete(Core.GenerateCastle);

		var gridPos = ConvertPosToTileGrid(center);

		var gridPosMin = ConvertPosToTileGrid(center - radius);
		var gridPosMax = ConvertPosToTileGrid(center + radius);
		var bounds = new BoundsMinMax(Mathf.FloorToInt(gridPosMin.x), Mathf.FloorToInt(gridPosMin.y),
									  Mathf.CeilToInt(gridPosMax.x), Mathf.CeilToInt(gridPosMax.y));

		var entities = tileModelSpatialLookupRO.GetEntities(ref bounds, TileType.All);
		foreach (var entity in entities)
		{
			if (!entity.Has<T>()) continue;
			if (!entity.Has<Translation>()) continue;
			var pos = entity.Read<Translation>().Value;
			if (math.distance(center, pos.xz) <= radius)
			{
				yield return entity;
			}
		}
		entities.Dispose();
	}

	public static Entity FindClosestTilePosition(Vector3 pos, bool ignoreFloors = false)
	{
		var spatialData = Core.GenerateCastle._TileModelLookupSystemData;
		var tileModelSpatialLookupRO = spatialData.GetSpatialLookupReadOnlyAndComplete(Core.GenerateCastle);

		var gridPos = ConvertPosToTileGrid(pos);
		var bounds = new BoundsMinMax((int)(gridPos.x - 2.5), (int)(gridPos.z - 2.5),
									  (int)(gridPos.x + 2.5), (int)(gridPos.z + 2.5));

		var closestEntity = Entity.Null;
		var closestDistance = float.MaxValue;
		var entities = tileModelSpatialLookupRO.GetEntities(ref bounds, TileType.All);
		for (var i = 0; i < entities.Length; ++i)
		{
			var entity = entities[i];
			if (!entity.Has<TilePosition>()) continue;
			if (!entity.Has<Translation>()) continue;
			if (ignoreFloors && entity.Has<CastleFloor>()) continue;
			var entityPos = entity.Read<Translation>().Value;
			var distance = math.distancesq(pos, entityPos);
			if (distance < closestDistance)
			{
				var prefabName = GetPrefabGUID(entity).LookupName();
				if (!prefabName.StartsWith("TM_")) continue;

				closestDistance = distance;
				closestEntity = entity;
			}
		}
		entities.Dispose();

		return closestEntity;
	}

	public static float2 ConvertPosToTileGrid(float2 pos)
	{
		return new float2(Mathf.FloorToInt(pos.x * 2) + 6400, Mathf.FloorToInt(pos.y * 2) + 6400);
	}

	public static float3 ConvertPosToTileGrid(float3 pos)
	{
		return new float3(Mathf.FloorToInt(pos.x * 2) + 6400, pos.y, Mathf.FloorToInt(pos.z * 2) + 6400);
	}

	public static void RepairGear(Entity Character, bool repair = true)
	{
		Equipment equipment = Character.Read<Equipment>();
		NativeList<Entity> equippedItems = new(Allocator.Temp);
		equipment.GetAllEquipmentEntities(equippedItems);
		foreach (var equippedItem in equippedItems)
		{
			if (equippedItem.Has<Durability>())
			{
				var durability = equippedItem.Read<Durability>();
				if (repair)
				{
					durability.Value = durability.MaxDurability;
				}
				else
				{
					durability.Value = 0;
				}

				equippedItem.Write(durability);
			}
		}
		equippedItems.Dispose();

		for (int i = 0; i < 36; i++)
		{
			if (InventoryUtilities.TryGetItemAtSlot(Core.EntityManager, Character, i, out InventoryBuffer item))
			{
				var itemEntity = item.ItemEntity._Entity;
				if (itemEntity.Has<Durability>())
				{
					var durability = itemEntity.Read<Durability>();
					if (repair)
					{
						durability.Value = durability.MaxDurability;
					}
					else
					{
						durability.Value = 0;
					}

					itemEntity.Write(durability);
				}
			}
		}
	}

	public static void ReviveCharacter(Entity Character, Entity User, ChatCommandContext ctx = null)
	{
		var health = Character.Read<Health>();
		ctx?.Reply("TryGetbuff");
		if (BuffUtility.TryGetBuff(Core.EntityManager, Character, Prefabs.Buff_General_Vampire_Wounded_Buff, out var buffData))
		{
			ctx?.Reply("Destroy");
			DestroyUtility.Destroy(Core.EntityManager, buffData, DestroyDebugReason.TryRemoveBuff);

			ctx?.Reply("Health");
			health.Value = health.MaxHealth;
			health.MaxRecoveryHealth = health.MaxHealth;
			Character.Write(health);
		}
		if (health.IsDead)
		{
			ctx?.Reply("Respawn");
			var pos = Character.Read<LocalToWorld>().Position;

			Nullable_Unboxed<float3> spawnLoc = new() { value = pos };

			ctx?.Reply("Respawn2");
			var sbs = Core.Server.GetExistingSystemManaged<ServerBootstrapSystem>();
			var bufferSystem = Core.Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
			var buffer = bufferSystem.CreateCommandBuffer();
			ctx?.Reply("Respawn3");
			sbs.RespawnCharacter(buffer, User,
				customSpawnLocation: spawnLoc,
				previousCharacter: Character);
		}
    }

	public static void KickPlayer(Entity userEntity)
	{
		EntityManager entityManager = Core.Server.EntityManager;
		User user = userEntity.Read<User>();

		if (!user.IsConnected || user.PlatformId==0) return;

		Entity entity =  entityManager.CreateEntity(new ComponentType[3]
		{
			ComponentType.ReadOnly<NetworkEventType>(),
			ComponentType.ReadOnly<SendEventToUser>(),
			ComponentType.ReadOnly<KickEvent>()
		});

		entity.Write(new KickEvent()
		{
			PlatformId = user.PlatformId
		});
		entity.Write(new SendEventToUser()
		{
			UserIndex = user.Index
		});
		entity.Write(new NetworkEventType()
		{
			EventId = NetworkEvents.EventId_KickEvent,
			IsAdminEvent = false,
			IsDebugEvent = false
		});
	}

	public static void UnlockWaypoints(Entity userEntity)
	{
		DynamicBuffer<UnlockedWaypointElement> dynamicBuffer = Core.EntityManager.AddBuffer<UnlockedWaypointElement>(userEntity);
		dynamicBuffer.Clear();
		foreach (Entity waypoint in Helper.GetEntitiesByComponentType<ChunkWaypoint>())
			dynamicBuffer.Add(new UnlockedWaypointElement()
			{
				Waypoint = waypoint.Read<NetworkId>()
			});
	}

	public static void RevealMapForPlayer(Entity userEntity)
	{
		var mapZoneElements = Core.EntityManager.GetBuffer<UserMapZoneElement>(userEntity);
		foreach (var mapZone in mapZoneElements)
		{
			var userZoneEntity = mapZone.UserZoneEntity.GetEntityOnServer();
			var revealElements = Core.EntityManager.GetBuffer<UserMapZonePackedRevealElement>(userZoneEntity);
			revealElements.Clear();
			var revealElement = new UserMapZonePackedRevealElement
			{
				PackedPixel = 255
			};
			for (var i = 0; i < 8192; i++)
			{
				revealElements.Add(revealElement);
			}
		}
	}
	// add the component debugunlock
}
