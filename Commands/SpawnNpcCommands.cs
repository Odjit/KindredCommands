using System.Linq;
using KindredCommands.Data;
using KindredCommands.Models;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;
using static ProjectM.Metrics;
using static RootMotion.FinalIK.Grounding;

namespace KindredCommands.Commands;

internal static class SpawnCommands
{
	public record struct CharacterUnit(string Name, PrefabGUID Prefab);

	public class CharacterUnitConverter : CommandArgumentConverter<CharacterUnit>
	{
		public override CharacterUnit Parse(ICommandContext ctx, string input)
		{
			if (Character.Named.TryGetValue(input, out var unit) || Character.Named.TryGetValue("CHAR_" + input, out unit))
			{
				return new(Character.NameFromPrefab[unit.GuidHash], unit);
			}
			// "CHAR_Bandit_Bomber": -1128238456,
			if (int.TryParse(input, out var id) && Character.NameFromPrefab.TryGetValue(id, out var name))
			{
				return new(name, new(id));
			}

			throw ctx.Error($"Can't find unit {input.Bold()}");
		}
	}

	[Command("spawnnpc", "spwn", description: "Spawns CHAR_ npcs", adminOnly: true)]
	public static void SpawnNpc(ChatCommandContext ctx, CharacterUnit character, int count = 1, int level = -1)
	{
		if (Database.IsSpawnBanned(character.Name, out var reason))
		{
			throw ctx.Error($"Cannot spawn {character.Name.Bold()} because it is banned. Reason: {reason}");
		}

		var pos = Core.EntityManager.GetComponentData<LocalToWorld>(ctx.Event.SenderCharacterEntity).Position;

		for (var i = 0; i < count; i++)
		{
			Core.UnitSpawner.SpawnWithCallback(ctx.Event.SenderUserEntity, character.Prefab, new float2(pos.x, pos.z), -1, (Entity e) =>
			{
				if (level > 0)
				{
					Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.BoostedBuff1, -1, true);
					if (BuffUtility.TryGetBuff(Core.EntityManager, e, Prefabs.BoostedBuff1, out var buffEntity))
					{
						buffEntity.Remove<SpawnStructure_WeakenState_DataShared>();
						buffEntity.Remove<ScriptSpawn>();
						buffEntity.Add<ModifyUnitLevelBuff>();
						buffEntity.Write(new ModifyUnitLevelBuff()
						{
							UnitLevel = level
						});
					}

					if (!e.Has<UnitLevel>())
						e.Add<UnitLevel>();
					var unitLevel = e.Read<UnitLevel>();
					unitLevel.Level._Value = level;
					e.Write(unitLevel);
				}

			});
		}
		ctx.Reply($"Spawning {count} {character.Name.Bold()} at your position");
	}

	[Command("customspawn", "cspwn", "customspawn <Prefab Name> [<BloodType> <BloodQuality> <Consumable(\"true/false\")> <Duration> <level>]", "Spawns a modified NPC at your current position.", adminOnly: true)]
	public static void CustomSpawnNpc(ChatCommandContext ctx, CharacterUnit unit, BloodType type = BloodType.Frailed, int quality = 0, bool consumable = true, int duration = -1, int level = 0)
	{
		var pos = Core.EntityManager.GetComponentData<LocalToWorld>(ctx.Event.SenderCharacterEntity).Position;
		CustomSpawnNpcAt(ctx, unit, pos.x, pos.y, pos.z, type, quality, consumable, duration, level);
	}

	[Command("customspawnat", "cspwnat", "customspawnat <Prefab Name> <X> <Z> <Y> [<BloodType> <BloodQuality> <Consumable(\"true/false\")> <Duration> <level>]", "Spawns a modified NPC at a specific location.", adminOnly: true)]
	public static void CustomSpawnNpcAt(ChatCommandContext ctx, CharacterUnit unit, float x, float y, float z, BloodType type = BloodType.Frailed, int quality = 0, bool consumable = true, int duration = -1, int level = 0)
	{
		if (Database.IsSpawnBanned(unit.Name, out var reason))
		{
			throw ctx.Error($"Cannot spawn {unit.Name.Bold()} because it is banned. Reason: {reason}");
		}

		if (quality > 100 || quality < 0)
		{
			throw ctx.Error($"Blood Quality must be between 0 and 100");
		}

		Core.UnitSpawner.SpawnWithCallback(ctx.Event.SenderUserEntity, unit.Prefab, new float2(x, z), duration, (Entity e) =>
		{
			if (e.Has<BloodConsumeSource>())
			{
				var blood = Core.EntityManager.GetComponentData<BloodConsumeSource>(e);
				blood.UnitBloodType._Value = new PrefabGUID((int)type);
				blood.BloodQuality = quality;
				blood.CanBeConsumed = consumable;
				Core.EntityManager.SetComponentData(e, blood);

				switch (type)
				{
					case BloodType.Brute:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Brute, -1, true);
						break;
					case BloodType.Creature:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Creature, -1, true);
						break;
					case BloodType.Draculin:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Draculin, -1, true);
						break;
					case BloodType.Mutant:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Mutant, -1, true);
						break;
					case BloodType.Rogue:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Rogue, -1, true);
						break;
					case BloodType.Scholar:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Scholar, -1, true);
						break;
					case BloodType.Warrior:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Warrior, -1, true);
						break;
					case BloodType.Worker:
						Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.AB_BloodQualityUnitBuff_Worker, -1, true);
						break;
				}
			}

			if (level > 0)
			{
				Buffs.TryAddBuff(ctx.Event.SenderUserEntity, e, Prefabs.BoostedBuff1, -1, true);
				if (BuffUtility.TryGetBuff(Core.EntityManager, e, Prefabs.BoostedBuff1, out var buffEntity))
				{
					buffEntity.Remove<SpawnStructure_WeakenState_DataShared>();
					buffEntity.Remove<ScriptSpawn>();
					buffEntity.Add<ModifyUnitLevelBuff>();
					buffEntity.Write(new ModifyUnitLevelBuff()
					{
						UnitLevel = level
					});
				}

				if (!e.Has<UnitLevel>())
					e.Add<UnitLevel>();
				var unitLevel = e.Read<UnitLevel>();
				unitLevel.Level._Value = level;
				e.Write(unitLevel);
			}
		}, y);
		ctx.Reply($"Spawning {unit.Name.Bold()} with {quality}% {type} blood at {x}, {z}. It is Lvl{level} and will live {(duration < 0 ? "until killed" : $"{duration} seconds")}.");
	}

	[Command("despawnnpc", "dspwn", description: "Despawns CHAR_ npcs", adminOnly: true)]
	public static void DespawnNpc(ChatCommandContext ctx, CharacterUnit character, float radius = 25f)
	{
		var charEntity = ctx.Event.SenderCharacterEntity;
		var pos = charEntity.Read<Translation>().Value.xz;
		var entities = Helper.GetAllEntitiesInRadius<PrefabGUID>(pos, radius).Where(e => e.Read<PrefabGUID>().Equals(character.Prefab));
		var count = 0;
		foreach (var e in entities)
		{
			StatChangeUtility.KillOrDestroyEntity(Core.EntityManager, e, charEntity, charEntity, Time.time, StatChangeReason.Default, true);
			count++;
		}
		ctx.Reply($"You've killed {count} {character.Name.Bold()} at your position. You murderer!");
	}

	[Command("spawnhorse", "sh", description: "Spawns a horse", adminOnly: true)] 
	public static void SpawnHorse(ChatCommandContext ctx, float speed, float acceleration, float rotation, int num=1)
	{
		var pos = Core.EntityManager.GetComponentData<LocalToWorld>(ctx.Event.SenderCharacterEntity).Position;
		var horsePrefab = Prefabs.CHAR_Mount_Horse;

		for (int i = 0; i < num; i++)
		{
			Core.UnitSpawner.SpawnWithCallback(ctx.Event.SenderUserEntity, horsePrefab, pos.xz, -1, (Entity horse) =>
			{
				var mount = horse.Read<Mountable>();
				mount.MaxSpeed = speed;
				mount.Acceleration = acceleration;
				mount.RotationSpeed = rotation * 10f;
				horse.Write(mount);
			});
		}

		ctx.Reply($"Spawned {num} horse{(num > 1 ? "s" : "")} (with speed:{speed}, accel:{acceleration}, and rotate:{rotation}) near you.");
	}


	[Command("spawnban", description: "Shows which GUIDs are banned and why.", adminOnly: true)]
	public static void SpawnBan(ChatCommandContext ctx, CharacterUnit character, string reason)
	{
		Database.SetNoSpawn(character.Name, reason);
		ctx.Reply($"Banned '{character.Name}' from spawning with reason '{reason}'");
	}

	[Command("teleporthorse", description: "teleports horses to you", adminOnly: true)]
	public static void TeleportHorse(ChatCommandContext ctx, float radius = 5f)
	{
		var charEntity = ctx.Event.SenderCharacterEntity;
		var pos = charEntity.Read<Translation>().Value.xz;
		var entities = Helper.GetAllEntitiesInRadius<Mountable>(pos, radius).Where(e => e.Read<PrefabGUID>().Equals(Prefabs.CHAR_Mount_Horse_Vampire));
		var count = 0;
		foreach (var e in entities)
		{
			Core.EntityManager.SetComponentData(e, new Translation { Value = Core.EntityManager.GetComponentData<LocalToWorld>(ctx.Event.SenderCharacterEntity).Position });
			count++;
		}

		ctx.Reply($"You've teleported {count} horses to your position.");
	}
}
