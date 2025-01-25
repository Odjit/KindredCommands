using Stunlock.Core;
using VampireCommandFramework;

namespace KindredCommands.Commands.BloodBound;

/// <summary>
/// Converts user inputs to command parameter.
/// </summary>
public class ItemDescriptorConverter : CommandArgumentConverter<ItemDescriptor>
{
	/// <summary>
	/// Parses user input.
	/// </summary>
	/// <param name="ctx">Command context.</param>
	/// <param name="input">User input.</param>
	/// <returns>Command parameter.</returns>
	public override ItemDescriptor Parse(ICommandContext ctx, string input)
	{
		PrefabGUID prefabId;
		// user used prefab id as identifier (i.e -1958888844)
		if (int.TryParse(input, out var guid))
		{
			prefabId = new PrefabGUID(guid);
			
		}
		// user used name as identifier (i.e. Item_Weapon_Axe_T01_Bone)
		else
		{
			if (!Core.Prefabs.TryGetItem(input, out prefabId))
			{
				return new ItemDescriptor(input);
			}
		}

		// verify there is an entity behind specified prefab id.
		if (!Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabId, out var entity))
		{
			return new ItemDescriptor(input);
		}

		return new ItemDescriptor(input, prefabId, entity);
	}
}
