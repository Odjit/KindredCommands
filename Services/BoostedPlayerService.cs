using System.Collections;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using KindredCommands.Data;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Shared;
using Unity.Entities;
using UnityEngine;

namespace KindredCommands.Services
{
	internal class BoostedPlayerService
	{
		readonly HashSet<Entity> boostedPlayers = [];
		readonly Dictionary<Entity, float> playerAttackSpeed = [];
		readonly Dictionary<Entity, float> playerDamage = [];
		readonly Dictionary<Entity, int>   playerHps = [];
		readonly Dictionary<Entity, float> playerProjectileSpeeds = [];
		readonly Dictionary<Entity, float> playerProjectileRanges = [];
		readonly Dictionary<Entity, float> playerSpeeds = [];
		readonly Dictionary<Entity, float> playerYield = [];
		readonly HashSet<Entity> noAggroPlayers = [];
		readonly HashSet<Entity> noBlooddrainPlayers = [];
		readonly HashSet<Entity> noDurabilityPlayers = [];
		readonly HashSet<Entity> noCooldownPlayers = [];
		readonly HashSet<Entity> immaterialPlayers = [];
		readonly HashSet<Entity> invinciblePlayers = [];
		readonly HashSet<Entity> shroudedPlayers = [];

		GameObject boostedPlayerSvcGameObject;
		IgnorePhysicsDebugSystem boostedPlayerMonoBehaviour;

		public BoostedPlayerService()
		{
			boostedPlayerSvcGameObject = new GameObject("BoostedPlayerService");
			boostedPlayerMonoBehaviour = boostedPlayerSvcGameObject.AddComponent<IgnorePhysicsDebugSystem>();
		}

		public bool IsBoostedPlayer(Entity charEntity)
        {
            return boostedPlayers.Contains(charEntity);
        }

		public void UpdateBoostedPlayer(Entity charEntity)
		{
			boostedPlayers.Add(charEntity);
			var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;

			if(invinciblePlayers.Contains(charEntity))
			{
				// Heal back to full
				Health health = charEntity.Read<Health>();
				health.Value = health.MaxHealth;
				health.MaxRecoveryHealth = health.MaxHealth;
				charEntity.Write(health);

				// Remove PVP Buff
				Buffs.RemoveBuff(charEntity, Prefabs.Buff_InCombat_PvPVampire);
			}

			if(immaterialPlayers.Contains(charEntity))
			{
				Buffs.AddBuff(userEntity, charEntity, Prefabs.AB_Blood_BloodRite_Immaterial, -1, true);
				if (BuffUtility.TryGetBuff(Core.EntityManager, charEntity, Prefabs.AB_Blood_BloodRite_Immaterial, out Entity immaterialBuffEntity))
				{
					var modifyMovementSpeedBuff = immaterialBuffEntity.Read<ModifyMovementSpeedBuff>();
					modifyMovementSpeedBuff.MoveSpeed = 1; //bloodrite makes you accelerate forever, disable this
					immaterialBuffEntity.Write(modifyMovementSpeedBuff);
				}
			}

			if(shroudedPlayers.Contains(charEntity))
			{
				Buffs.AddBuff(userEntity, charEntity, Prefabs.EquipBuff_ShroudOfTheForest, -1, true);
			}

			boostedPlayerMonoBehaviour.StartCoroutine(RemoveAndAddCustomBuff(userEntity, charEntity).WrapToIl2Cpp());
		}

		IEnumerator RemoveAndAddCustomBuff(Entity userEntity, Entity charEntity)
		{
			Buffs.RemoveBuff(charEntity, Prefabs.CustomBuff);
			while(BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.CustomBuff))
				yield return null;

			Buffs.AddBuff(userEntity, charEntity, Prefabs.CustomBuff, -1, true);
		}

		public void RemoveBoostedPlayer(Entity charEntity)
		{
			boostedPlayers.Remove(charEntity);
			playerAttackSpeed.Remove(charEntity);
			playerDamage.Remove(charEntity);
			playerHps.Remove(charEntity);
			playerProjectileSpeeds.Remove(charEntity);
			playerProjectileRanges.Remove(charEntity);
			playerSpeeds.Remove(charEntity);
			playerYield.Remove(charEntity);
			noAggroPlayers.Remove(charEntity);
			noBlooddrainPlayers.Remove(charEntity);
			noCooldownPlayers.Remove(charEntity);
			noDurabilityPlayers.Remove(charEntity);
			immaterialPlayers.Remove(charEntity);
			invinciblePlayers.Remove(charEntity);
			shroudedPlayers.Remove(charEntity);

			ClearExtraBuffs(charEntity);
		}

		public void SetAttackSpeedMultiplier(Entity charEntity, float attackSpeed)
		{
			playerAttackSpeed[charEntity] = attackSpeed;
		}

		public void SetDamageBoost(Entity charEntity, float damage)
		{
			boostedPlayers.Add(charEntity);
			playerDamage[charEntity] = damage;
		}

		public void SetHealthBoost(Entity charEntity, int hp)
		{
			boostedPlayers.Add(charEntity);
			playerHps[charEntity] = hp;
		}

		public void SetProjectileSpeedMultiplier(Entity charEntity, float speed)
		{
			playerProjectileSpeeds[charEntity] = speed;
		}

		public bool GetProjectileSpeedMultiplier(Entity charEntity, out float speed)
		{
			return playerProjectileSpeeds.TryGetValue(charEntity, out speed);
		}

		public void SetProjectileRangeMultiplier(Entity charEntity, float range)
		{
			playerProjectileRanges[charEntity] = range;
		}

		public bool GetProjectileRangeMultiplier(Entity charEntity, out float range)
		{
			return playerProjectileRanges.TryGetValue(charEntity, out range);
		}

		public void SetSpeedBoost(Entity charEntity, float speed)
		{
			boostedPlayers.Add(charEntity);
			playerSpeeds[charEntity] = speed;
		}

		public void SetYieldMultiplier(Entity charEntity, float yield)
		{
			playerYield[charEntity] = yield;
		}

		public void AddNoAggro(Entity charEntity)
		{
			noAggroPlayers.Add(charEntity);
		}

		public void AddNoBlooddrain(Entity charEntity)
		{
			noBlooddrainPlayers.Add(charEntity);
		}

		public void AddNoCooldown(Entity charEntity)
		{
			noCooldownPlayers.Add(charEntity);
		}

		public void AddNoDurability(Entity charEntity)
		{
			noDurabilityPlayers.Add(charEntity);
		}

		public void AddPlayerImmaterial(Entity charEntity)
		{
			immaterialPlayers.Add(charEntity);
		}

		public void AddPlayerInvincible(Entity charEntity)
		{
			invinciblePlayers.Add(charEntity);
		}

		public bool IsPlayerInvincible(Entity charEntity)
		{
			return invinciblePlayers.Contains(charEntity);
		}

		public void AddPlayerShrouded(Entity charEntity)
		{
			shroudedPlayers.Add(charEntity);
		}

		public void UpdateCustomBuff(Entity buffEntity)
		{
			var charEntity = buffEntity.Read<EntityOwner>().Owner;
			var modifyStatBuffer = Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
			modifyStatBuffer.Clear();

			if (playerAttackSpeed.TryGetValue(charEntity, out var attackSpeed))
			{
				var modifiedBuff = AttackSpeed;
				modifiedBuff.Value = attackSpeed;
				modifyStatBuffer.Add(modifiedBuff);
			}

			if (playerDamage.TryGetValue(charEntity, out var damage))
			{
				foreach (var damageBuff in damageBuffs)
				{
					var modifiedBuff = damageBuff;
					modifiedBuff.Value = damage;
					modifyStatBuffer.Add(modifiedBuff);
				}
			}

			if (playerHps.TryGetValue(charEntity, out var hp))
			{
				var modifiedBuff = MaxHP;
				modifiedBuff.Value = hp;
				modifyStatBuffer.Add(modifiedBuff);
			}

			if (playerSpeeds.TryGetValue(charEntity, out var speed))
			{
				var modifiedBuff = Speed;
				modifiedBuff.Value = speed;
				modifyStatBuffer.Add(modifiedBuff);
			}

			if (playerYield.TryGetValue(charEntity, out var yield))
			{
				var modifiedBuff = MaxYield;
				modifiedBuff.Value = yield;
				modifyStatBuffer.Add(modifiedBuff);
			}

			if (noAggroPlayers.Contains(charEntity))
			{
				buffEntity.Add<DisableAggroBuff>();
				buffEntity.Write(new DisableAggroBuff
				{
					Mode = DisableAggroBuffMode.OthersDontAttackTarget
				});
			}

			if (noBlooddrainPlayers.Contains(charEntity))
			{
				buffEntity.Add<ModifyBloodDrainBuff>();
				var modifyBloodDrainBuff = new ModifyBloodDrainBuff()
				{
					AffectBloodValue = true,
					AffectIdleBloodValue = true,
					BloodValue = 0,
					BloodIdleValue = 0,

					ModificationId = new ModificationId(),
					ModificationIdleId = new ModificationId(),
					IgnoreIdleDrainModId = new ModificationId(),

					ModificationPriority = 1000,
					ModificationIdlePriority = 1000,

					ModificationType = ModificationType.Set,
					ModificationIdleType = ModificationType.Set,

					IgnoreIdleDrainWhileActive = true,
				};
				buffEntity.Write(modifyBloodDrainBuff);
			}

			if (noCooldownPlayers.Contains(charEntity))
			{
				modifyStatBuffer.Add(Cooldown);
			}

			if (noDurabilityPlayers.Contains(charEntity))
			{
				modifyStatBuffer.Add(DurabilityLoss);
			}

			long buffModificationFlags = 0;
			if (immaterialPlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.DisableDynamicCollision | BuffModificationTypes.DisableMapCollision | BuffModificationTypes.IsVbloodGhost);

			}

			if (invinciblePlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.Invulnerable | BuffModificationTypes.ImmuneToHazards | BuffModificationTypes.ImmuneToSun | BuffModificationTypes.CannotBeDisconnectDragged | BuffModificationTypes.DisableUnitVisibility);
				foreach (var buff in invincibleBuffs)
				{
					modifyStatBuffer.Add(buff);
				}
			}

			if (buffModificationFlags != 0)
			{
				buffEntity.Add<BuffModificationFlagData>();
				var buffModificationFlagData = new BuffModificationFlagData()
				{
					ModificationTypes = buffModificationFlags,
					ModificationId = ModificationId.NewId(0),
				};
				buffEntity.Write(buffModificationFlagData);
			}
		}

		void ClearExtraBuffs(Entity charEntity)
		{
			var buffs = Core.EntityManager.GetBuffer<BuffBuffer>(charEntity);
			var stringsToIgnore = new List<string>
			{
				"BloodBuff",
				"SetBonus",
				"EquipBuff",
				"Combat",
				"VBlood_Ability_Replace",
				"Shapeshift",
				"Interact",
				"AB_Consumable",
			};

			foreach (var buff in buffs)
			{
				bool shouldRemove = true;
				foreach (string word in stringsToIgnore)
				{
					if (buff.PrefabGuid.LookupName().Contains(word))
					{
						shouldRemove = false;
						break;
					}
				}
				if (shouldRemove)
				{
					DestroyUtility.Destroy(Core.EntityManager, buff.Entity, DestroyDebugReason.TryRemoveBuff);
				}
			}
			var equipment = charEntity.Read<Equipment>();
			if (!equipment.IsEquipped(Prefabs.Item_Cloak_Main_ShroudOfTheForest, out var _) && BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.EquipBuff_ShroudOfTheForest))
			{
				Buffs.RemoveBuff(charEntity, Prefabs.EquipBuff_ShroudOfTheForest);
			}
		}

		#region GodMode & Other Buff
		static ModifyUnitStatBuff_DOTS Cooldown = new()
		{
			StatType = UnitStatType.CooldownModifier,
			Value = 0,
			ModificationType = ModificationType.Set,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SunCharge = new()
		{
			StatType = UnitStatType.SunChargeTime,
			Value = 50000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS Hazard = new()
		{
			StatType = UnitStatType.ImmuneToHazards,
			Value = 1,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SunResist = new()
		{
			StatType = UnitStatType.SunResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS Speed = new()
		{
			StatType = UnitStatType.MovementSpeed,
			Value = 15,
			ModificationType = ModificationType.Set,
			Id = ModificationId.NewId(4)
		};

		static ModifyUnitStatBuff_DOTS PResist = new()
		{
			StatType = UnitStatType.PhysicalResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS FResist = new()
		{
			StatType = UnitStatType.FireResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS HResist = new()
		{
			StatType = UnitStatType.HolyResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SResist = new()
		{
			StatType = UnitStatType.SilverResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS GResist = new()
		{
			StatType = UnitStatType.GarlicResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SPResist = new()
		{
			StatType = UnitStatType.SpellResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS PPower = new()
		{
			StatType = UnitStatType.PhysicalPower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SiegePower = new()
		{
			StatType = UnitStatType.SiegePower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS RPower = new()
		{
			StatType = UnitStatType.ResourcePower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SPPower = new()
		{
			StatType = UnitStatType.SpellPower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS PHRegen = new()
		{
			StatType = UnitStatType.PassiveHealthRegen,
			Value = 100000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS HRecovery = new()
		{
			StatType = UnitStatType.HealthRecovery,
			Value = 100000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS MaxHP = new()
		{
			StatType = UnitStatType.MaxHealth,
			Value = 100000,
			ModificationType = ModificationType.Set,
			Id = ModificationId.NewId(5)
		};

		static ModifyUnitStatBuff_DOTS MaxYield = new()
		{
			StatType = UnitStatType.ResourceYield,
			Value = 10,
			ModificationType = ModificationType.Multiply,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS DurabilityLoss = new()
		{
			StatType = UnitStatType.ReducedResourceDurabilityLoss,
			Value = -10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS AttackSpeed = new()
		{
			StatType = UnitStatType.AttackSpeed,
			Value = 5,
			ModificationType = ModificationType.Multiply,
			Id = ModificationId.NewId(0)
		};

		public readonly static List<ModifyUnitStatBuff_DOTS> invincibleBuffs =
		[
			PResist,
			FResist,
			HResist,
			SResist,
			SunResist,
			GResist,
			SPResist,
			Hazard,
			SunCharge,
			HRecovery,
			PHRegen
		];

		public readonly static List<ModifyUnitStatBuff_DOTS> damageBuffs =
		[
			PPower,
			RPower,
			SPPower,
			SiegePower
		];
		#endregion
	}
}