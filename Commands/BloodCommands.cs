using KindredCommands.Models;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;

namespace KindredCommands.Commands;

internal static class BloodCommands
{

	[Command("bloodpotion", "bp", description: "Creates a Potion with specified Blood Type, Quality, and amount", adminOnly: true)]
	public static void GiveBloodPotionCommand(ChatCommandContext ctx, BloodType type = BloodType.Frailed, float quality = 100f, int quantity = 1)
	{
		quality = Mathf.Clamp(quality, 0, 100);
		var countAdded = Helper.AddBloodPotionToInventory(ctx.Event.SenderCharacterEntity, type, quality, quantity);
		ctx.Reply($"Received <color=#ff0>{countAdded}</color> Blood Merlot of <color=#ff0>{type}</color> type of <color=#ff0>{quality}</color>% quality");
		if (countAdded < quantity)
		{
			ctx.Reply($"Inventory is full, could not add the last <color=#ff0>{quantity - countAdded}</color> Blood Merlot");
		}
	}

	[Command("bloodpotionToAll", null, description: "Gives a potion to all players, with specified Blood Type, Quality, and amount", adminOnly: true)]
	public static void GiveBloodPotionToAllPlayersCommand(ChatCommandContext ctx, BloodType type = BloodType.Frailed, float quality = 100f, int quantity = 1, bool includeOffline = false)
	{
		quality = Mathf.Clamp(quality, 0, 100);
		foreach (var characterEntity in Core.Players.GetPlayerCharacters(onlineOnly: !includeOffline))
		{
			Helper.AddBloodPotionToInventory(characterEntity, type, quality, quantity);
		}
		var targetDesc = includeOffline ? "all players" : "all online players";
		ctx.Reply($"Gave to {targetDesc}: <color=#ff0>{quantity}</color> Blood Merlot of <color=#ff0>{type}</color> type of <color=#ff0>{quality}</color>% quality");
		FixedString512Bytes message = $"All players received {quantity} Blood Merlot of {type} type";
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref message);
	}

	[Command("bloodpotionmix", "bpm", description: "Creates a Potion with two specified Blood Types, Qualities, secondary trait option and amount", adminOnly: true)]
	public static void GiveBloodMerlotCommand(ChatCommandContext ctx, BloodType primaryType = BloodType.Frailed, float primaryQuality = 100f, BloodType secondaryType = BloodType.Frailed, float secondaryQuality = 100f, int secondaryTrait = 1, int quantity = 1)
	{
		primaryQuality = Mathf.Clamp(primaryQuality, 0, 100);
		secondaryQuality = Mathf.Clamp(secondaryQuality, 0, 100);
		secondaryTrait = Mathf.Clamp(secondaryTrait, 1, 3);
		var countAdded = Helper.AddBloodMerlotMixToInventory(ctx.Event.SenderCharacterEntity, primaryType, primaryQuality, secondaryType, secondaryQuality, secondaryTrait, quantity);
		ctx.Reply($"Received <color=#ff0>{countAdded}</color> Blood Merlot of <color=#ff0>{primaryType}</color> type of <color=#ff0>{primaryQuality}</color>% quality " +
				  $"with secondary <color=#ff0>{secondaryType}</color> type of <color=#ff0>{secondaryQuality}</color>% quality and trait option {secondaryTrait}");
		if (countAdded < quantity)
		{
			ctx.Reply($"Inventory is full, could not add the last <color=#ff0>{quantity - countAdded}</color> Blood Merlot");
		}
	}

	[Command("bloodpotionmixToAll", null, description: "Gives a potion to all players, with two specified Blood Types, Qualities, secondary trait option and amount", adminOnly: true)]
	public static void GiveBloodMerlotToAllPlayersCommand(ChatCommandContext ctx, BloodType primaryType = BloodType.Frailed, float primaryQuality = 100f, BloodType secondaryType = BloodType.Frailed, float secondaryQuality = 100f, int secondaryTrait = 1, int quantity = 1, bool includeOffline = false)
	{
		primaryQuality = Mathf.Clamp(primaryQuality, 0, 100);
		secondaryQuality = Mathf.Clamp(secondaryQuality, 0, 100);
		secondaryTrait = Mathf.Clamp(secondaryTrait, 1, 3);
		foreach (var characterEntity in Core.Players.GetPlayerCharacters(onlineOnly: !includeOffline))
		{
			Helper.AddBloodMerlotMixToInventory(characterEntity, primaryType, primaryQuality, secondaryType, secondaryQuality, secondaryTrait, quantity);
		}
		var targetDesc = includeOffline ? "all players" : "all online players";
		ctx.Reply($"Gave to {targetDesc}: <color=#ff0>{quantity}</color> Blood Merlot of <color=#ff0>{primaryType}</color> type of <color=#ff0>{primaryQuality}</color>% quality " +
				  $"with secondary <color=#ff0>{secondaryType}</color> type of <color=#ff0>{secondaryQuality}</color>% quality and trait option {secondaryTrait}");
		FixedString512Bytes message = $"All players received {quantity} Blood Merlot of {primaryType}/{secondaryType} type";
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref message);
	}
}
