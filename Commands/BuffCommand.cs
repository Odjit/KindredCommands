using System;
using KindredCommands.Commands.Converters;
using KindredCommands.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;

namespace KindredCommands.Commands;
internal class BuffCommands
{	public record struct BuffInput(string Name, PrefabGUID Prefab);

	public class BuffConverter : CommandArgumentConverter<BuffInput>
	{
		public override BuffInput Parse(ICommandContext ctx, string input)
		{
			if (Core.Prefabs.TryGetBuff(input, out PrefabGUID buffPrefab))
			{
				return new(buffPrefab.LookupName(), buffPrefab);
			}
			
			if (int.TryParse(input, out var id))
			{
				var prefabGuid = new PrefabGUID(id);
				if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefabEntity)
					&& prefabEntity.Has<Buff>())
				{
					var name = Core.Prefabs.CollectionSystem._PrefabLookupMap.GetName(prefabGuid);
					return new(name, prefabGuid);
				}
			}

			throw ctx.Error($"Can't find buff {input.Bold()}");
		}
	}

	[Command("buff", adminOnly: true)]
	public static void BuffCommand(ChatCommandContext ctx, BuffInput buff,OnlinePlayer player = null, int duration = 0, bool immortal = false)
	{
		var userEntity = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

		Buffs.TryAddOrUpdateBuff(userEntity, charEntity, buff.Prefab, duration, immortal);
		ctx.Reply($"Applied the buff {buff.Name} to {userEntity.Read<User>().CharacterName}");
	}

	[Command("buffToAll", "buffAll", adminOnly: true)]
	public static void BuffToAllOnlinePlayersCommand(ChatCommandContext ctx, BuffInput buff, int duration = 0, bool immortal = false, bool includeOffline = false)
	{
		int totalCount = 0;
		int successCount = 0;
		foreach (var playerData in Core.Players.FindPlayers(onlineOnly: !includeOffline, requiresCharacter: true))
		{
			totalCount++;
			try
			{
				Buffs.TryAddOrUpdateBuff(playerData.UserEntity, playerData.CharEntity, buff.Prefab, duration, immortal);
				successCount++;
			}
			catch (Exception ex)
			{
				Core.Log.LogError(ex);
			}
		}
		var targetDesc = includeOffline ? "players" : "online players";
		ctx.Reply($"Applied the buff {buff.Name} to {successCount}/{totalCount} {targetDesc}.");
		FixedString512Bytes message = $"All players received the buff {buff.Name}";
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref message);
	}

	[Command("debuff", adminOnly: true)]
	public static void DebuffCommand(ChatCommandContext ctx, BuffInput buff, OnlinePlayer player = null)
	{
		var targetEntity = (Entity)(player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity);
		Buffs.RemoveBuff(targetEntity, buff.Prefab);
		ctx.Reply($"Removed the buff {buff.Name} from {targetEntity.Read<PlayerCharacter>().Name}");
	}

	[Command("debuffToAll", "debuffAll", adminOnly: true)]
	public static void DebuffToAllPlayersCommand(ChatCommandContext ctx, BuffInput buff, bool includeOffline = false)
	{
		int totalCount = 0;
		int successCount = 0;
		foreach (var characterEntity in Core.Players.GetPlayerCharacters(onlineOnly: !includeOffline))
		{
			totalCount++;
			try
			{
				Buffs.RemoveBuff(characterEntity, buff.Prefab);
				successCount++;
			}
			catch (Exception ex)
			{
				Core.Log.LogError(ex);
			}
		}
		var targetDesc = includeOffline ? "players" : "online players";
		ctx.Reply($"Removed the buff {buff.Name} from {successCount}/{totalCount} {targetDesc}.");
		FixedString512Bytes message = $"The buff {buff.Name} has been removed from all players";
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref message);
	}

	[Command("listbuffs", description: "Lists the buffs a player has", adminOnly: true)]
	public static void ListBuffsCommand(ChatCommandContext ctx, OnlinePlayer player = null)
	{
		var Character = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
		var buffEntities = Helper.GetEntitiesByComponentTypes<Buff, PrefabGUID>();
		foreach (var buffEntity in buffEntities)
		{
			if (buffEntity.Read<EntityOwner>().Owner == Character)
			{
				ctx.Reply(buffEntity.Read<PrefabGUID>().LookupName());
			}
		}
	}

	internal static void DebuffCommand(Entity character, PrefabGUID buff_InCombat_PvPVampire)
	{
		throw new NotImplementedException();
	}
}
