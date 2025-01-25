using Stunlock.Core;
using Unity.Entities;

namespace KindredCommands.Commands.BloodBound;

public class ItemDescriptor
{
	/// <summary>
	/// Initializes a new valid instance of <see cref="ItemDescriptor"/>.
	/// </summary>
	/// <param name="input">User input.</param>
	/// <param name="prefab">Prefab id.</param>
	/// <param name="entity">Entity.</param>
	public ItemDescriptor(string input, PrefabGUID prefab, Entity entity)
	{
		Input = input;
		Prefab = prefab;
		Entity = entity;
		IsValid = true;
	}

	/// <summary>
	/// Initializes a new invalid instance of <see cref="ItemDescriptor"/>.
	/// </summary>
	/// <param name="input">User input.</param>
	public ItemDescriptor(string input)
	{
		Input = input;
		IsValid = false;
	}

	/// <summary>
	/// Entity.
	/// </summary>
	public Entity Entity { get; }

	/// <summary>
	/// User input.
	/// </summary>
	public string Input { get; }

	/// <summary>
	/// Indicates if descriptor is valid.
	/// </summary>
	public bool IsValid { get; }

	/// <summary>
	/// Prefab id.
	/// </summary>
	public PrefabGUID Prefab { get; }
}
