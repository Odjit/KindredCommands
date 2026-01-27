using System.Collections;
using System.Collections.Generic;
using KindredCommands.Data;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;

namespace KindredCommands.Services
{
	internal class BoostedPlayerService
	{
		readonly Dictionary<Entity, float> playerAttackSpeed = [];
		readonly Dictionary<Entity, float> playerDamage = [];
		readonly Dictionary<Entity, float> playerHps = [];
		readonly Dictionary<Entity, float> playerSpeeds = [];
		readonly Dictionary<Entity, float> playerYield = [];
		readonly HashSet<Entity> batVisionPlayers = [];
		readonly HashSet<Entity> flyingPlayers = [];
		readonly HashSet<Entity> noAggroPlayers = [];
		readonly HashSet<Entity> noBlooddrainPlayers = [];
		readonly HashSet<Entity> noDurabilityPlayers = [];
		readonly HashSet<Entity> noCooldownPlayers = [];
		readonly HashSet<Entity> immaterialPlayers = [];
		readonly HashSet<Entity> invinciblePlayers = [];
		readonly HashSet<Entity> shroudedPlayers = [];
		readonly HashSet<Entity> sunInvulnPlayers = [];
		readonly HashSet<Entity> daywalkerPlayers = [];

		public BoostedPlayerService()
		{
			LoadCurrentPlayerBoosts();
		}

		public bool IsBoostedPlayer(Entity charEntity, bool includeDaywalkers = false, bool includeShrouded = true)
		{
			return playerAttackSpeed.ContainsKey(charEntity) || playerDamage.ContainsKey(charEntity) || playerHps.ContainsKey(charEntity) ||
				playerSpeeds.ContainsKey(charEntity) || playerYield.ContainsKey(charEntity) ||
				batVisionPlayers.Contains(charEntity) || flyingPlayers.Contains(charEntity) || 
				noAggroPlayers.Contains(charEntity) || noBlooddrainPlayers.Contains(charEntity) || noDurabilityPlayers.Contains(charEntity) ||
				noCooldownPlayers.Contains(charEntity) || immaterialPlayers.Contains(charEntity) || invinciblePlayers.Contains(charEntity) ||
				includeShrouded && shroudedPlayers.Contains(charEntity) || sunInvulnPlayers.Contains(charEntity) ||
				includeDaywalkers && daywalkerPlayers.Contains(charEntity);
		}

		public IEnumerable<Entity> GetBoostedPlayers()
		{
			foreach (var charEntity in Helper.GetEntitiesByComponentType<PlayerCharacter>(includeDisabled: true))
			{
				if (IsBoostedPlayer(charEntity, includeShrouded: false))
					yield return charEntity;
			}
		}

		bool ShouldRemoveCaps(Entity charEntity)
		{
			return playerAttackSpeed.ContainsKey(charEntity) ||  playerSpeeds.ContainsKey(charEntity) ||
				playerYield.ContainsKey(charEntity) || noBlooddrainPlayers.Contains(charEntity) ||
				noCooldownPlayers.Contains(charEntity);
		}

		public void UpdateBoostedPlayer(Entity charEntity)
		{
			if(!IsBoostedPlayer(charEntity, true))
			{
				ClearExtraBuffs(charEntity);
				return;
			}

			if (charEntity.Has<VampireAttributeCaps>() && ShouldRemoveCaps(charEntity)) charEntity.Remove<VampireAttributeCaps>();

			var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;

			if (invinciblePlayers.Contains(charEntity))
			{
				// Heal back to full
				Health health = charEntity.Read<Health>();
				health.Value = health.MaxHealth;
				health.MaxRecoveryHealth = health.MaxHealth;
				charEntity.Write(health);

				// Remove PVP Buff
				Buffs.RemoveBuff(charEntity, Prefabs.Buff_InCombat_PvPVampire);
			}

			if (immaterialPlayers.Contains(charEntity))
			{
				Buffs.TryAddBuff(userEntity, charEntity, Prefabs.AB_Blood_BloodRite_Immaterial, -1, true);
				if (BuffUtility.TryGetBuff(Core.EntityManager, charEntity, Prefabs.AB_Blood_BloodRite_Immaterial, out Entity immaterialBuffEntity))
				{
					var modifyMovementSpeedBuff = immaterialBuffEntity.Read<ModifyMovementSpeedBuff>();
					modifyMovementSpeedBuff.MoveSpeed = 1; //bloodrite makes you accelerate forever, disable this
					immaterialBuffEntity.Write(modifyMovementSpeedBuff);
				}
			}
			else
			{
				Buffs.RemoveBuff(charEntity, Prefabs.AB_Blood_BloodRite_Immaterial);
			}

			if (shroudedPlayers.Contains(charEntity))
			{
				Buffs.TryAddBuff(userEntity, charEntity, Prefabs.EquipBuff_ShroudOfTheForest, -1, true);
			}
			else
			{
				var equipment = charEntity.Read<Equipment>();
				if (!equipment.IsEquipped(Prefabs.Item_Cloak_Main_ShroudOfTheForest, out var _))
					Buffs.RemoveBuff(charEntity, Prefabs.EquipBuff_ShroudOfTheForest);
			}

			Core.StartCoroutine(RemoveAndAddCustomBuff(userEntity, charEntity));
		}

		IEnumerator RemoveAndAddCustomBuff(Entity userEntity, Entity charEntity)
		{
			Buffs.RemoveBuff(charEntity, Prefabs.BoostedBuff1);
			Buffs.RemoveBuff(charEntity, Prefabs.BoostedBuff2);
			while (BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.BoostedBuff1))
				yield return null;
			while (BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.BoostedBuff2))
				yield return null;

			Buffs.TryAddBuff(userEntity, charEntity, Prefabs.BoostedBuff1, -1, true);
			Buffs.TryAddBuff(userEntity, charEntity, Prefabs.BoostedBuff2, -1, true);

			if (BuffUtility.TryGetBuff(Core.EntityManager, charEntity, Prefabs.BoostedBuff1, out var buffEntity))
			{
				buffEntity.Remove<SpawnStructure_WeakenState_DataShared>();
				buffEntity.Remove<ScriptSpawn>();
			}
			if (BuffUtility.TryGetBuff(Core.EntityManager, charEntity, Prefabs.BoostedBuff2, out buffEntity))
			{
				buffEntity.Remove<SpawnStructure_WeakenState_DataShared>();
				buffEntity.Remove<ScriptSpawn>();
			}
		}

		public void RemoveBoostedPlayer(Entity charEntity)
		{
			playerAttackSpeed.Remove(charEntity);
			playerDamage.Remove(charEntity);
			playerHps.Remove(charEntity);
			playerSpeeds.Remove(charEntity);
			playerYield.Remove(charEntity);
			batVisionPlayers.Remove(charEntity);
			flyingPlayers.Remove(charEntity);
			noAggroPlayers.Remove(charEntity);
			noBlooddrainPlayers.Remove(charEntity);
			noCooldownPlayers.Remove(charEntity);
			if(noDurabilityPlayers.Remove(charEntity))
				Core.TrackPlayerEquipment.StopTrackingPlayerForNoDurability(charEntity);
			immaterialPlayers.Remove(charEntity);
			invinciblePlayers.Remove(charEntity);
			shroudedPlayers.Remove(charEntity);
			sunInvulnPlayers.Remove(charEntity);

			ClearExtraBuffs(charEntity);
		}

		public void SetAttackSpeedMultiplier(Entity charEntity, float attackSpeed)
		{
			playerAttackSpeed[charEntity] = attackSpeed;
		}

		public bool RemoveAttackSpeedMultiplier(Entity charEntity)
		{
			return playerAttackSpeed.Remove(charEntity);
		}

		public bool GetAttackSpeedMultiplier(Entity charEntity, out float attackSpeed)
		{
			return playerAttackSpeed.TryGetValue(charEntity, out attackSpeed);
		}

		public void SetDamageBoost(Entity charEntity, float damage)
		{
			playerDamage[charEntity] = damage;
		}

		public bool RemoveDamageBoost(Entity charEntity)
		{
			return playerDamage.Remove(charEntity);
		}

		public bool GetDamageBoost(Entity charEntity, out float damage)
		{
			return playerDamage.TryGetValue(charEntity, out damage);
		}

		public void SetHealthBoost(Entity charEntity, int hp)
		{
			playerHps[charEntity] = hp;
		}

		public bool RemoveHealthBoost(Entity charEntity)
		{
			return playerHps.Remove(charEntity);
		}

		public bool GetHealthBoost(Entity charEntity, out float hp)
		{
			return playerHps.TryGetValue(charEntity, out hp);
		}

		public void SetSpeedBoost(Entity charEntity, float speed)
		{
			playerSpeeds[charEntity] = speed;
		}

		public bool RemoveSpeedBoost(Entity charEntity)
		{
			return playerSpeeds.Remove(charEntity);
		}

		public bool GetSpeedBoost(Entity charEntity, out float speed)
		{
			return playerSpeeds.TryGetValue(charEntity, out speed);
		}

		public void SetYieldMultiplier(Entity charEntity, float yield)
		{
			playerYield[charEntity] = yield;
		}

		public bool RemoveYieldMultiplier(Entity charEntity)
		{
			return playerYield.Remove(charEntity);
		}

		public bool GetYieldMultiplier(Entity charEntity, out float yield)
		{
			return playerYield.TryGetValue(charEntity, out yield);
		}

		public bool ToggleBatVision(Entity charEntity)
		{
			if (batVisionPlayers.Contains(charEntity))
			{
				batVisionPlayers.Remove(charEntity);
				return false;
			}
			batVisionPlayers.Add(charEntity);
			return true;
		}

		public bool HasBatVision(Entity charEntity)
		{
			return batVisionPlayers.Contains(charEntity);
		}

		public bool ToggleFlying(Entity charEntity)
		{
			if (flyingPlayers.Contains(charEntity))
			{
				flyingPlayers.Remove(charEntity);
				return false;
			}
			flyingPlayers.Add(charEntity);
			return true;
		}

		public bool IsFlying(Entity charEntity)
		{
			return flyingPlayers.Contains(charEntity);
		}

		public bool ToggleNoAggro(Entity charEntity)
		{
			if (noAggroPlayers.Contains(charEntity))
			{
				noAggroPlayers.Remove(charEntity);
				return false;
			}
			noAggroPlayers.Add(charEntity);
			return true;
		}

		public bool HasNoAggro(Entity charEntity)
		{
			return noAggroPlayers.Contains(charEntity);
		}

		public bool ToggleNoBlooddrain(Entity charEntity)
		{
			if (noBlooddrainPlayers.Contains(charEntity))
			{
				noBlooddrainPlayers.Remove(charEntity);
				return false;
			}
			noBlooddrainPlayers.Add(charEntity);
			return true;
		}

		public bool HasNoBlooddrain(Entity charEntity)
		{
			return noBlooddrainPlayers.Contains(charEntity);
		}

		public bool ToggleNoCooldown(Entity charEntity)
		{
			if (noCooldownPlayers.Contains(charEntity))
			{
				noCooldownPlayers.Remove(charEntity);
				return false;
			}
			noCooldownPlayers.Add(charEntity);
			return true;
		}

		public bool HasNoCooldown(Entity charEntity)
		{
			return noCooldownPlayers.Contains(charEntity);
		}

		public bool ToggleNoDurability(Entity charEntity)
		{
			if (!noDurabilityPlayers.Contains(charEntity))
			{
				noDurabilityPlayers.Add(charEntity);
				Core.TrackPlayerEquipment.StartTrackingPlayerForNoDurability(charEntity);
				return true;
			}

			noDurabilityPlayers.Remove(charEntity);
			Core.TrackPlayerEquipment.StopTrackingPlayerForNoDurability(charEntity);
			return false;
		}

		public bool HasNoDurability(Entity charEntity)
		{
			return noDurabilityPlayers.Contains(charEntity);
		}

		public bool TogglePlayerImmaterial(Entity charEntity)
		{
			if (immaterialPlayers.Contains(charEntity))
			{
				immaterialPlayers.Remove(charEntity);
				return false;
			}
			immaterialPlayers.Add(charEntity);
			return true;
		}

		public bool IsPlayerImmaterial(Entity charEntity)
		{
			return immaterialPlayers.Contains(charEntity);
		}

		public bool TogglePlayerInvincible(Entity charEntity)
		{
			if (invinciblePlayers.Contains(charEntity))
			{
				invinciblePlayers.Remove(charEntity);
				return false;
			}
			invinciblePlayers.Add(charEntity);
			return true;
		}

		public bool IsPlayerInvincible(Entity charEntity)
		{
			return invinciblePlayers.Contains(charEntity);
		}

		public bool ToggleSunInvulnerable(Entity charEntity)
		{
			if (sunInvulnPlayers.Contains(charEntity))
			{
				sunInvulnPlayers.Remove(charEntity);
				return false;
			}
			sunInvulnPlayers.Add(charEntity);
			return true;
		}

		public bool IsSunInvulnerable(Entity charEntity)
		{
			return sunInvulnPlayers.Contains(charEntity);
		}

		public bool ToggleDaywalker(Entity charEntity)
		{
			if (daywalkerPlayers.Contains(charEntity))
			{
				daywalkerPlayers.Remove(charEntity);
				return false;
			}
			daywalkerPlayers.Add(charEntity);
			return true;
		}

		public bool IsDaywalker(Entity charEntity)
		{
			return daywalkerPlayers.Contains(charEntity);
		}

		public bool TogglePlayerShrouded(Entity charEntity)
		{
			if (shroudedPlayers.Contains(charEntity))
			{
				shroudedPlayers.Remove(charEntity);
				return false;
			}
			shroudedPlayers.Add(charEntity);
			return true;
		}

		public void HandleShroudRemoval(Entity charEntity)
		{
			if (shroudedPlayers.Contains(charEntity))
			{
				Core.StartCoroutine(AddBuffOnceRemoved(charEntity, Prefabs.EquipBuff_ShroudOfTheForest));
			}
		}

		IEnumerator AddBuffOnceRemoved(Entity charEntity, PrefabGUID buffPrefab)
		{
			while(BuffUtility.HasBuff(Core.EntityManager, charEntity, buffPrefab))
				yield return null;
			if(IsPlayerShrouded(charEntity))
				Buffs.TryAddBuff(charEntity.Read<PlayerCharacter>().UserEntity, charEntity, buffPrefab, -1, true);
		}

		public bool IsPlayerShrouded(Entity charEntity)
		{
			return shroudedPlayers.Contains(charEntity);
		}

		public void UpdateBoostedBuff1(Entity buffEntity)
		{
			
			var charEntity = buffEntity.Read<EntityOwner>().Owner;
			var modifyStatBuffer = Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
			modifyStatBuffer.Clear();

			if (playerAttackSpeed.TryGetValue(charEntity, out var attackSpeed))
			{
				var modifiedBuff = AbilityAttackSpeed;
				modifiedBuff.Value = attackSpeed;
				modifyStatBuffer.Add(modifiedBuff);

				modifiedBuff = PrimaryAttackSpeed;
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
				foreach (var speedBuff in speedBuffs)
				{
					var modifiedBuff = speedBuff;
					if (speedBuff.StatType == UnitStatType.MovementSpeed)
						modifiedBuff.Value = speed;
					else
						modifiedBuff.Value = (speed - 4)/4f;
					modifyStatBuffer.Add(modifiedBuff);
				}
			}

			if (playerYield.TryGetValue(charEntity, out var yield))
			{
				var modifiedBuff = MaxYield;
				modifiedBuff.Value = yield;
				modifyStatBuffer.Add(modifiedBuff);
			}

			if (noCooldownPlayers.Contains(charEntity))
			{
				modifyStatBuffer.Add(Cooldown);
			}

			long buffModificationFlags = 0;
			if (daywalkerPlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.ImmuneToSun);
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

		public void UpdateBoostedBuff2(Entity buffEntity)
		{
			var charEntity = buffEntity.Read<EntityOwner>().Owner;
			var modifyStatBuffer = Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
			modifyStatBuffer.Clear();

			if (noDurabilityPlayers.Contains(charEntity))
			{
				modifyStatBuffer.Add(DurabilityLoss);
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

			long buffModificationFlags = 0;
			if (flyingPlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.DisableDynamicCollision | BuffModificationTypes.FlyOnlyMapCollision | BuffModificationTypes.IsFlying);
			}

			if (immaterialPlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.DisableDynamicCollision | BuffModificationTypes.DisableMapCollision | BuffModificationTypes.IsVbloodGhost);

			}

			if (invinciblePlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.Invulnerable | BuffModificationTypes.ImmuneToSun | BuffModificationTypes.CannotBeDisconnectDragged);
				foreach (var buff in invincibleBuffs)
				{
					modifyStatBuffer.Add(buff);
				}
			}

			if (sunInvulnPlayers.Contains(charEntity))
			{
				buffModificationFlags |= (long)(BuffModificationTypes.ImmuneToSun);
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
			Buffs.RemoveBuff(charEntity, Prefabs.BoostedBuff1);
			Buffs.RemoveBuff(charEntity, Prefabs.BoostedBuff2);
			Buffs.RemoveBuff(charEntity, Prefabs.AB_Blood_BloodRite_Immaterial);

			var equipment = charEntity.Read<Equipment>();
			if (!equipment.IsEquipped(Prefabs.Item_Cloak_Main_ShroudOfTheForest, out var _) && BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.EquipBuff_ShroudOfTheForest))
			{
				Buffs.RemoveBuff(charEntity, Prefabs.EquipBuff_ShroudOfTheForest);
			}

			// Readd the caps
			if (!charEntity.Has<VampireAttributeCaps>()) charEntity.Add<VampireAttributeCaps>();
			
			var prefabGuid = charEntity.Read<PrefabGUID>();
			if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefab))
			{
				var caps = prefab.Read<VampireAttributeCaps>();
				charEntity.Write(caps);
			}
		}

		void LoadCurrentPlayerBoosts()
		{
			foreach(var charEntity in Helper.GetEntitiesByComponentType<PlayerCharacter>(includeDisabled: true))
			{
				LoadPlayerBoosts(charEntity);
			}
		}

		void LoadPlayerBoosts(Entity charEntity)
		{
			if (BuffUtility.TryGetBuff(Core.Server.EntityManager, charEntity, Prefabs.BoostedBuff1, out var buffEntity) &&
				buffEntity.Has<ModifyUnitStatBuff_DOTS>())
			{
				foreach (var buff in buffEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>())
				{
					switch (buff.StatType)
					{
						case UnitStatType.AbilityAttackSpeed:
							playerAttackSpeed[charEntity] = buff.Value;
							break;
						case UnitStatType.PhysicalPower:
							playerDamage[charEntity] = buff.Value;
							break;
						case UnitStatType.MaxHealth:
							playerHps[charEntity] = buff.Value;
							break;
						case UnitStatType.MovementSpeed:
							playerSpeeds[charEntity] = buff.Value;
							break;
						case UnitStatType.ResourceYield:
							playerYield[charEntity] = buff.Value;
							break;
						case UnitStatType.CooldownRecoveryRate:
							noCooldownPlayers.Add(charEntity);
							break;
						case UnitStatType.ReducedResourceDurabilityLoss:
							noDurabilityPlayers.Add(charEntity);
							break;
					}
				}

				if (buffEntity.Has<BuffModificationFlagData>())
				{
					var buffModificationFlagData = buffEntity.Read<BuffModificationFlagData>();

					if ((buffModificationFlagData.ModificationTypes & (long)BuffModificationTypes.ImmuneToSun) != 0)
					{
						daywalkerPlayers.Add(charEntity);
					}
				}
			}

			if (BuffUtility.TryGetBuff(Core.Server.EntityManager, charEntity, Prefabs.BoostedBuff2, out buffEntity))
			{
				if (Core.EntityManager.HasBuffer<ModifyUnitStatBuff_DOTS>(buffEntity))
				{
					foreach (var buff in buffEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>())
					{
						switch (buff.StatType)
						{
							case UnitStatType.ReducedResourceDurabilityLoss:
								noDurabilityPlayers.Add(charEntity);
								break;
						}
					}
				}

				if (buffEntity.Has<DisableAggroBuff>())
				{
					noAggroPlayers.Add(charEntity);
				}

				if (buffEntity.Has<ModifyBloodDrainBuff>())
				{
					noBlooddrainPlayers.Add(charEntity);
				}

				if (buffEntity.Has<BuffModificationFlagData>())
				{
					var buffModificationFlagData = buffEntity.Read<BuffModificationFlagData>();
					if ((buffModificationFlagData.ModificationTypes & (long)BuffModificationTypes.IsFlying) != 0)
					{
						flyingPlayers.Add(charEntity);
					}
					if ((buffModificationFlagData.ModificationTypes & (long)BuffModificationTypes.DisableMapCollision) != 0)
					{
						immaterialPlayers.Add(charEntity);
					}
					if ((buffModificationFlagData.ModificationTypes & (long)BuffModificationTypes.Invulnerable) != 0)
					{
						invinciblePlayers.Add(charEntity);
					}
					else if ((buffModificationFlagData.ModificationTypes & (long)BuffModificationTypes.ImmuneToSun) != 0)
					{
						sunInvulnPlayers.Add(charEntity);
					}
				}
			}

			if (BuffUtility.TryGetBuff(Core.Server.EntityManager, charEntity, Prefabs.EquipBuff_ShroudOfTheForest, out buffEntity))
			{
				var equipment = charEntity.Read<Equipment>();
				if (!equipment.IsEquipped(Prefabs.Item_Cloak_Main_ShroudOfTheForest, out var _))
				{
					TogglePlayerShrouded(charEntity);
				}
			}
		}

		#region GodMode & Other Buff
		static ModifyUnitStatBuff_DOTS Cooldown = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.CooldownRecoveryRate,
			Value = 100,
			ModificationType = ModificationType.Set,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SunCharge = new()
		{
			StatType = UnitStatType.SunChargeTime,
			Value = 50000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS Hazard = new()
		{
			StatType = UnitStatType.ImmuneToHazards,
			Value = 1,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SunResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.SunResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS Speed = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.MovementSpeed,
			Value = 15,
			ModificationType = ModificationType.Set,
			Modifier = 1,
			Id = ModificationId.NewId(4)
		};

		static ModifyUnitStatBuff_DOTS ShapeshiftSpeed = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.BonusShapeshiftMovementSpeed,
			Value = 15,
			ModificationType = ModificationType.Set,
			Modifier = 1,
			Id = ModificationId.NewId(4)
		};

		static ModifyUnitStatBuff_DOTS MountSpeed = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.BonusMountMovementSpeed,
			Value = 15,
			ModificationType = ModificationType.Set,
			Modifier = 1,
			Id = ModificationId.NewId(4)
		};

		static ModifyUnitStatBuff_DOTS PResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.PhysicalResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS FResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.FireResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS HResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.HolyResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.SilverResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS GResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.GarlicResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SPResist = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.SpellResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS PPower = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.PhysicalPower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SiegePower = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.SiegePower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS RPower = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.ResourcePower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS SPPower = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.SpellPower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS PHRegen = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.PassiveHealthRegen,
			Value = 100000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS HRecovery = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.HealthRecovery,
			Value = 100000,
			ModificationType = ModificationType.Add,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS MaxHP = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.MaxHealth,
			Value = 100000,
			ModificationType = ModificationType.Set,
			Modifier = 1,
			Id = ModificationId.NewId(5)
		};

		static ModifyUnitStatBuff_DOTS MaxYield = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.ResourceYield,
			Value = 10,
			ModificationType = ModificationType.Multiply,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS DurabilityLoss = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.ReducedResourceDurabilityLoss,
			Value = -1000000000,
			ModificationType = ModificationType.Set,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS PrimaryAttackSpeed = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.PrimaryAttackSpeed,
			Value = 5,
			ModificationType = ModificationType.Multiply,
			Modifier = 1,
			Id = ModificationId.NewId(0)
		};

		static ModifyUnitStatBuff_DOTS AbilityAttackSpeed = new()
		{
			AttributeCapType = AttributeCapType.Uncapped,
			StatType = UnitStatType.AbilityAttackSpeed,
			Value = 5,
			ModificationType = ModificationType.Multiply,
			Modifier = 1,
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

		public readonly static List<ModifyUnitStatBuff_DOTS> speedBuffs =
		[
			Speed,
			ShapeshiftSpeed,
			MountSpeed
		];
		#endregion
	}
}
