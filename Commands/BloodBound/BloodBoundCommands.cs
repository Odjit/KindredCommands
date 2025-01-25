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
	/// <param name="descriptor">Command parameter. See <see cref="ItemDescriptorConverter"/>.</param>
	[Command("add", "a", "<Prefab GUID or name>", description: "Adds Blood-Bound attribute to items", adminOnly: true)]
	public static void AddBloodBound(ChatCommandContext ctx, ItemDescriptor descriptor)
	{
		if (!descriptor.IsValid)
		{
			ctx.Reply($"{descriptor.Input} not found.");
		}
		else
		{
			if (Core.BloodBoundService.SetBloodBound(descriptor.Prefab, descriptor.Entity, true))
			{
				Core.ConfigSettings.SetBloodBound(descriptor.Prefab, true);
				ctx.Reply($"Added Blood-Bound attribute to {descriptor.Input}");
			}
			else
			{
				ctx.Reply($"{descriptor.Input} is Blood-Bound already.");
			}
		}
	}

	/// <summary>
	/// Removes an entity from blood-bound category.
	/// </summary>
	/// <param name="ctx">Command context.</param>
	/// <param name="descriptor">Command parameter. See <see cref="ItemDescriptorConverter"/>.</param>
	[Command("remove", "r", "<Prefab GUID or name>", description: "Removes Blood-Bound attribute from items", adminOnly: true)]
	public static void RemoveBloodBound(ChatCommandContext ctx, ItemDescriptor descriptor)
	{
		if (!descriptor.IsValid)
		{
			ctx.Reply($"{descriptor.Input} not found.");
		}
		else
		{
			if (Core.BloodBoundService.SetBloodBound(descriptor.Prefab, false))
			{
				Core.ConfigSettings.SetBloodBound(descriptor.Prefab, false);
				ctx.Reply($"Removed Blood-Bound attribute from {descriptor.Input}");
			}
			else
			{
				ctx.Reply($"{descriptor.Input} isn't Blood-Bound already.");
			}
		}
	}
}
