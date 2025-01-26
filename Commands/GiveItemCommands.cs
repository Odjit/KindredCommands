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
		prefabSys.PrefabGuidToNameDictionary.TryGetValue(item.Value, out var name);
		ctx.Reply($"Gave {quantity} {name}");
	}
}
