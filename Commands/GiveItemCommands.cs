using KindredCommands.Commands.Converters;
using ProjectM;
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

	[Command("giveToAll", null, "<Prefab GUID or name> [quantity=1] [onlineOnly=false]", "Gives the specified item to all players", adminOnly: true)]
	public static void GiveItemToAllPlayers(ChatCommandContext ctx, ItemParameter item, int quantity = 1, bool onlineOnly = false)
	{
		foreach (var characterEntity in Core.Players.GetPlayerCharacters(onlineOnly))
		{
			Helper.AddItemToInventory(characterEntity, item.Value, quantity);
		}		
		var prefabSys = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		var name = prefabSys._PrefabLookupMap.GetName(item.Value);
		var targetDesc = onlineOnly ? "all online players" : "all players";
		ctx.Reply($"Gave {quantity} {name} to {targetDesc}");
	}
}
