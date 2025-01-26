using System;
using System.Collections.Generic;
using System.Text;
using Stunlock.Core;
using VampireCommandFramework;

namespace KindredCommands.Commands.Converters;

/// <summary>
/// Converts string parameter to <see cref="ItemParameter"/>.
/// </summary>
internal class ItemParameterConverter : CommandArgumentConverter<ItemParameter>
{
	public override ItemParameter Parse(ICommandContext ctx, string input)
	{

		if (int.TryParse(input, out var integral))
		{
			return new ItemParameter(new(integral));
		}

		if (TryGet(input, out var result)) return result;

		var inputIngredientAdded = "Item_Ingredient_" + input;
		if (TryGet(inputIngredientAdded, out result)) return result;

		// Standard postfix
		var standardPostfix = inputIngredientAdded + "_Standard";
		if (TryGet(standardPostfix, out result)) return result;

		List<(string Name, PrefabGUID Prefab)> searchResults = [];
		foreach (var kvp in Core.Prefabs.SpawnableNameToGuid)
		{
			if (kvp.Value.Name.StartsWith("Item_") && kvp.Key.Contains(input, StringComparison.OrdinalIgnoreCase))
			{
				searchResults.Add((kvp.Value.Name["Item_".Length..], kvp.Value.Prefab));
			}
		}

		if (searchResults.Count == 1)
		{
			return new ItemParameter(searchResults[0].Prefab);
		}

		var lengthOfFail = 60 + "\n...".Length;

		if (searchResults.Count > 1)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Multiple results be more specific");
			foreach (var kvp in searchResults)
			{
				if (sb.Length + kvp.Name.Length + lengthOfFail >= Core.MAX_REPLY_LENGTH)
				{
					sb.AppendLine("...");
					throw ctx.Error(sb.ToString());
				}
				else
				{
					sb.AppendLine(kvp.Name);
				}
			}
			throw ctx.Error(sb.ToString());
		}

		// Try a double search splitting the input
		for (var i = 3; i < input.Length; ++i)
		{
			var inputOne = input[..i];
			var inputTwo = input[i..];
			foreach (var kvp in Core.Prefabs.SpawnableNameToGuid)
			{
				if (kvp.Value.Name.StartsWith("Item_") &&
					kvp.Key.Contains(inputOne, StringComparison.OrdinalIgnoreCase) &&
					kvp.Key.Contains(inputTwo, StringComparison.OrdinalIgnoreCase))
				{
					searchResults.Add((kvp.Value.Name["Item_".Length..], kvp.Value.Prefab));
				}
			}

			if (searchResults.Count == 1)
			{
				return new ItemParameter(searchResults[0].Prefab);
			}
		}

		var resultsFromFirstSplit = searchResults;
		searchResults = [];

		// Try a double search splitting the input with _ prepended
		for (var i = 3; i < input.Length; ++i)
		{
			var inputOne = "_" + input[..i];
			var inputTwo = input[i..];
			foreach (var kvp in Core.Prefabs.SpawnableNameToGuid)
			{
				if (kvp.Value.Name.StartsWith("Item_") &&
					kvp.Key.Contains(inputOne, StringComparison.OrdinalIgnoreCase) &&
					kvp.Key.Contains(inputTwo, StringComparison.OrdinalIgnoreCase))
				{
					searchResults.Add((kvp.Value.Name["Item_".Length..], kvp.Value.Prefab));
				}
			}

			if (searchResults.Count == 1)
			{
				return new ItemParameter(searchResults[0].Prefab);
			}

			if (searchResults.Count > 1)
			{
				var sb = new StringBuilder();
				sb.AppendLine("Multiple results be more specific");
				foreach (var kvp in searchResults)
				{
					if (sb.Length + kvp.Name.Length + lengthOfFail >= Core.MAX_REPLY_LENGTH)
					{
						sb.AppendLine("...");
						throw ctx.Error(sb.ToString());
					}
					else
					{
						sb.AppendLine(kvp.Name);
					}
				}
				throw ctx.Error(sb.ToString());
			}
		}

		if (resultsFromFirstSplit.Count > 1)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Multiple results be more specific");
			foreach (var kvp in resultsFromFirstSplit)
			{
				if (sb.Length + kvp.Name.Length + lengthOfFail > Core.MAX_REPLY_LENGTH)
				{
					sb.AppendLine("...");
					throw ctx.Error(sb.ToString());
				}
				else
				{
					sb.AppendLine(kvp.Name);
				}
			}
			throw ctx.Error(sb.ToString());
		}

		throw ctx.Error($"Invalid item id: {input}");
	}

	private static bool TryGet(string input, out ItemParameter item)
	{
		if (Core.Prefabs.TryGetItem(input, out var prefab))
		{
			item = new ItemParameter(prefab);
			return true;
		}

		item = new ItemParameter(new(0));
		return false;
	}
}

