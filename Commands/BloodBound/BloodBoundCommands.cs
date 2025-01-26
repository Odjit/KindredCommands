using VampireCommandFramework;

namespace KindredCommands.Commands.BloodBound;

/// <summary>
/// Commands adding/removing entities from blood-bound category.
/// </summary>
[CommandGroup("bloodbound", "bb")]
public static class BloodBoundCommands
{
	/// <summary>
	/// Adds an entity to blood-bound category.
	/// </summary>
	/// <param name="ctx">Command context.</param>
	/// <param name="descriptor">Command parameter. See <see cref="BloodBoundItemParameterConverter"/>.</param>
	[Command("add", "a", "<Prefab GUID or name>", description: "Adds Blood-Bound attribute to items", adminOnly: true)]
	public static void AddBloodBound(ChatCommandContext ctx, BloodBoundItemParameter descriptor)
	{
		if (Core.BloodBoundService.SetBloodBound(descriptor.Prefab, descriptor.Entity, true))
		{
			Core.ConfigSettings.SetBloodBound(descriptor.Name, true);
			ctx.Reply($"Added Blood-Bound attribute to {descriptor.Name}");
		}
		else
		{
			ctx.Reply($"{descriptor.Name} is Blood-Bound already.");
		}
	}

	/// <summary>
	/// Removes an entity from blood-bound category.
	/// </summary>
	/// <param name="ctx">Command context.</param>
	/// <param name="descriptor">Command parameter. See <see cref="BloodBoundItemParameterConverter"/>.</param>
	[Command("remove", "r", "<Prefab GUID or name>", description: "Removes Blood-Bound attribute from items", adminOnly: true)]
	public static void RemoveBloodBound(ChatCommandContext ctx, BloodBoundItemParameter descriptor)
	{
		if (Core.BloodBoundService.SetBloodBound(descriptor.Prefab, false))
		{
			Core.ConfigSettings.SetBloodBound(descriptor.Name, false);
			ctx.Reply($"Removed Blood-Bound attribute from {descriptor.Name}");
		}
		else
		{
			ctx.Reply($"{descriptor.Name} isn't Blood-Bound already.");
		}
	}
}
