using KindredCommands.Commands.Converters;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using VampireCommandFramework;

namespace KindredCommands.Commands;

internal class GiveItemCommands
{
	[Command("give", "g", "<Prefab GUID or name> [quantity=1]", "Gives the specified item to the player", adminOnly: true)]
	public static void GiveItem(ChatCommandContext ctx, ItemParameter item, int quantity = 1)
	{
		Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, item.Value, quantity);
		var prefabSys = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		var name = prefabSys._PrefabLookupMap.GetName(item.Value);
		ctx.Reply($"Gave {quantity} {name}");
	}

	[Command("giveToAll", null, "<Prefab GUID or name> [quantity=1] [includeOffline=false]", "Gives the specified item to all players", adminOnly: true)]
	public static void GiveItemToAllPlayers(ChatCommandContext ctx, ItemParameter item, int quantity = 1, bool includeOffline = false)
	{
		foreach (var characterEntity in Core.Players.GetPlayerCharacters(onlineOnly: !includeOffline))
		{
			Helper.AddItemToInventory(characterEntity, item.Value, quantity);
		}
		var prefabSys = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		var name = prefabSys._PrefabLookupMap.GetName(item.Value);
		var targetDesc = includeOffline ? "all players" : "all online players";
		ctx.Reply($"Gave {quantity} {name} to {targetDesc}");
		FixedString512Bytes message = $"All players received {quantity} {name}";
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref message);
	}
}
