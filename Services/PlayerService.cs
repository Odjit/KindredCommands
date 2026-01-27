using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using KindredCommands.Models;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace KindredCommands.Services;

internal class PlayerService
{
	readonly Dictionary<FixedString64Bytes, PlayerData> namePlayerCache = [];
	readonly Dictionary<ulong, PlayerData> steamPlayerCache = [];
	readonly Dictionary<NetworkId, PlayerData> idPlayerCache = [];

	internal bool TryFindSteam(ulong steamId, out PlayerData playerData)
	{
		return steamPlayerCache.TryGetValue(steamId, out playerData);
	}

	internal bool TryFindName(FixedString64Bytes name, out PlayerData playerData)
	{
		return namePlayerCache.TryGetValue(name, out playerData);
	}

	internal PlayerService()
	{
		namePlayerCache.Clear();
		steamPlayerCache.Clear();

		var userEntities = Helper.GetEntitiesByComponentType<User>(includeDisabled: true);
		foreach (var entity in userEntities)
		{
			var userData = Core.EntityManager.GetComponentData<User>(entity);
			var playerData = new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, entity, userData.LocalCharacter._Entity);

			namePlayerCache.TryAdd(userData.CharacterName.ToString().ToLower(), playerData);
			steamPlayerCache.TryAdd(userData.PlatformId, playerData);

			var charEntity = userData.LocalCharacter.GetEntityOnServer();
			if (!charEntity.Equals(Entity.Null) &&
				Core.ConfigSettings.EveryoneDaywalker ^ Core.BoostedPlayerService.IsDaywalker(charEntity))
			{
				Core.BoostedPlayerService.ToggleDaywalker(charEntity);
				Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			}
		}


		var onlinePlayers = namePlayerCache.Values.Where(p => p.IsOnline).Select(p => $"\t{p.CharacterName}");
		Core.Log.LogWarning($"Player Cache Created with {namePlayerCache.Count} entries total, listing {onlinePlayers.Count()} online:");
		Core.Log.LogWarning(string.Join("\n", onlinePlayers));
	}

	internal void UpdatePlayerCache(Entity userEntity, string oldName, string newName, bool forceOffline = false)
	{
		var userData = Core.EntityManager.GetComponentData<User>(userEntity);
		namePlayerCache.Remove(oldName.ToLower());

		if (forceOffline) userData.IsConnected = false;
		var playerData = new PlayerData(newName, userData.PlatformId, userData.IsConnected, userEntity, userData.LocalCharacter._Entity);

		namePlayerCache[newName.ToLower()] = playerData;
		steamPlayerCache[userData.PlatformId] = playerData;
		idPlayerCache[userEntity.Read<NetworkId>()] = playerData;
	}

	public bool TryFindUserFromNetworkId(NetworkId networkId, out Entity userEntity)
	{
		if(idPlayerCache.TryGetValue(networkId, out var playerData))
		{
			userEntity = playerData.UserEntity;
			return true;
		}
		userEntity = Entity.Null;
		return false;
	}

	internal bool RenamePlayer(Entity userEntity, Entity charEntity, FixedString64Bytes newName)
	{
		var des = Core.Server.GetExistingSystemManaged<DebugEventsSystem>();
		var networkId = Core.EntityManager.GetComponentData<NetworkId>(userEntity);
		var userData = Core.EntityManager.GetComponentData<User>(userEntity);
		var renameEvent = new RenameUserDebugEvent
		{
			NewName = newName,
			Target = networkId
		};
		var fromCharacter = new FromCharacter
		{
			User = userEntity,
			Character = charEntity
		};

		des.RenameUser(fromCharacter, renameEvent);
		UpdatePlayerCache(userEntity, userData.CharacterName.ToString(), newName.ToString());

		var attachedBuffer = Core.EntityManager.GetBuffer<AttachedBuffer>(charEntity);
		foreach(var entry in attachedBuffer)
		{
			if (entry.PrefabGuid.GuidHash != -892362184) continue;
			var pmi = entry.Entity.Read<PlayerMapIcon>();
			pmi.UserName = newName;
			entry.Entity.Write(pmi);
		}

		Core.Log.LogInfo($"Player {userData.CharacterName} renamed to {newName}");

		return true;
	}

	public IEnumerable<Entity> GetPlayerCharacters(bool onlineOnly)
	{
		foreach (var playerData in FindPlayers(onlineOnly: onlineOnly, requiresCharacter: true))
		{
			yield return playerData.CharEntity;
		}
	}

	public IEnumerable<PlayerData> FindPlayers(bool onlineOnly = false, bool requiresCharacter = false)
	{
		var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<User>(), ComponentType.AccessMode.ReadOnly));
		var query = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);
		var entities = query.ToEntityArray(Allocator.Temp);
		var userDatas = query.ToComponentDataArray<User>(Allocator.Temp);
		for (var i = 0; i < entities.Length; i++)
		{
			var userData = userDatas[i];
			bool matchesOnlineStatus = !onlineOnly || userData.IsConnected;
			bool matchesCharacterStatus = !requiresCharacter || !userData.LocalCharacter._Entity.Equals(Entity.Null);			
			if (matchesOnlineStatus && matchesCharacterStatus)
			{
				yield return new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, entities[i], userData.LocalCharacter._Entity);
			}
		}
		entities.Dispose();
		userDatas.Dispose();
	}

	public static IEnumerable<Entity> GetUsersOnline()
	{
		var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<User>(), ComponentType.AccessMode.ReadOnly));
		NativeArray<Entity> _userEntities = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder).ToEntityArray(Allocator.Temp);
		entityQueryBuilder.Dispose();
		foreach (var entity in _userEntities)
		{
			if (Core.EntityManager.Exists(entity) && entity.Read<User>().IsConnected)
				yield return entity;
		}
		_userEntities.Dispose();
	}


	public IEnumerable<Entity> GetCachedUsersOnline()
	{
		foreach (var pd in namePlayerCache.Values.ToArray())
		{
			var entity = pd.UserEntity;
			if (Core.EntityManager.Exists(entity) && entity.Read<User>().IsConnected)
				yield return entity;
		}
	}
}
