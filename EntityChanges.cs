using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using DG.Tweening;

namespace The_Legend_of_Bum_bo_Windfall
{
    class EntityChanges
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(EntityChanges));
            Console.WriteLine("[The Legend of Bum-bo: Windfall] Applying entity changes");
        }

		//Changes champion generation such that each enemy has a chance to spawn as a champion instead of only one champion at most per room
		//Champions are now more common once the player has unlocked 'Everything Is Terrible!'
		[HarmonyPrefix, HarmonyPatch(typeof(BumboController), nameof(BumboController.MakeAChampion))]
		static bool BumboController_MakeAChampion(BumboController __instance)
		{
			if (WindfallSavedState.IsLoading())
			{
				return true;
			}

			float[] array = new float[]
			{
				0f,
				0.1f,
				0.125f,
				0.15f,
				0.175f
			};
			float NegaCrownMultiplier = 1f;
			float difficultyMultiplier = (!__instance.app.model.progression.unlocks[7]) ? 0.5f : 0.7f;
			for (int i = 0; i < __instance.app.model.characterSheet.trinkets.Count; i++)
			{
				NegaCrownMultiplier -= __instance.GetTrinket(i).ChampionChance();
			}
			if (__instance.app.model.mapModel.currentRoom.roomType != MapRoom.RoomType.Boss)
			{
				List<Enemy> list = new List<Enemy>();
				for (int j = 0; j < __instance.app.model.enemies.Count; j++)
				{
					if (__instance.app.model.enemies[j].alive && __instance.app.model.enemies[j].championType == Enemy.ChampionType.NotAChampion)
					{
						list.Add(__instance.app.model.enemies[j]);
					}
				}
				while (list.Count > 0)
				{
					int index = UnityEngine.Random.Range(0, list.Count);
					if (UnityEngine.Random.Range(0f, 1f) < array[__instance.app.model.characterSheet.currentFloor] * NegaCrownMultiplier * difficultyMultiplier)
					{
						Enemy.ChampionType champion = (Enemy.ChampionType)UnityEngine.Random.Range(1, 10);
						list[index].SetChampion(champion);
						list[index].Init();
					}
					list.RemoveAt(index);
				}
			}
			return false;
		}


		//Patch: Equalizes the chances between lanes when Loaf is determining where to spawn Corn Dips
		[HarmonyPrefix, HarmonyPatch(typeof(LoafBoss), "SpawnDips")]
		static bool LoafBoss_SpawnDips(LoafBoss __instance, int _spawn_y, ref bool __result)
        {
			DipEnemy[] array = UnityEngine.Object.FindObjectsOfType<DipEnemy>();
			int num = 0;
			float num2 = (__instance.getHealth() <= (float)(__instance.initialHealth / 2)) ? 0.5f : 0.25f;
			short num3 = 0;
			while ((int)num3 < array.Length)
			{
				if (array[(int)num3].enemyName == EnemyName.CornyDip)
				{
					num++;
				}
				num3 += 1;
			}

			//Determine how many Dips will be spawned
			bool[] dipSpawnLocations = new bool[3];
			for (short num4 = -1; num4 < 2; num4 += 1)
            {
				//Check whether the space is available
				if (__instance.position.x + (int)num4 >= 0 && __instance.position.x + (int)num4 < 3 && __instance.app.model.aiModel.battlefieldPositions[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x + (int)num4, _spawn_y]].owner_ground == null)
				{
					dipSpawnLocations[num4 + 1] = true;
				}
                else
                {
					dipSpawnLocations[num4 + 1] = false;
				}
			}

			//Count Dips
			int dipSpawnCount = 0;
			foreach (bool spawnLocation in dipSpawnLocations)
            {
				if (spawnLocation == true)
                {
					dipSpawnCount++;
                }
            }

			float spawnChance = 0f;

			//Spawn chance is zero if there is already a Corn Dip
			if (num == 0)
            {
				//Determine cumulative spawn chance
				for (int i = 1; i <= dipSpawnCount; i++)
				{
					spawnChance += BinomialDistribution.CalculateBinomialDistribution(dipSpawnCount, i, num2);
				}
			}

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Cumulative Corn Dip spawn chance: " + spawnChance.ToString());

			//Determine which lane to spawn Corn Dip in
			int cornDipSpawnLane = -2;
			if (UnityEngine.Random.Range(0f, 1f) < spawnChance)
            {
				cornDipSpawnLane = UnityEngine.Random.Range(-1, 2);
			}

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Corn Dip spawned: " + (cornDipSpawnLane != -2 ? "True" : "False"));

			//Spawn Dips
			for (short num4 = -1; num4 < 2; num4 += 1)
			{
				if (dipSpawnLocations[num4 + 1])
				{
					if (num4 == cornDipSpawnLane)
					{
						__instance.Spawn("CornyDip", __instance.position.x + (int)num4, _spawn_y, false);
						Console.WriteLine("[The Legend of Bum-bo: Windfall] Corn Dip spawned in lane " + (cornDipSpawnLane).ToString());
					}
					else
					{
						__instance.Spawn("Dip", __instance.position.x + (int)num4, _spawn_y, false);
					}
				}
			}

			SoundsView.Instance.PlaySound(SoundsView.eSound.Loaf_Dip_Spawn, __instance.transform.position, SoundsView.eAudioSlot.Default, false);
			__result = true;
			return false;
		}

		//Patch: Reduces Tainted Peeper's moves by one
		[HarmonyPostfix, HarmonyPatch(typeof(PeepsBoss), "Init")]
		static void PeepsBoss_Init(PeepsBoss __instance)
		{
			__instance.turns = 1;
			__instance.app.view.GUICamera.GetComponent<GUISide>().bossHeartView.SetMoves(__instance.turns + 2);
		}

		//Patch: Override enemy hurt method to modify damage resistance
		//Also changes Mysterious Bag effect to stack past 100% and incorporate Luck stat
		[HarmonyPrefix, HarmonyPatch(typeof(Enemy), "Hurt")]
        static bool Enemy_Hurt(Enemy __instance, ref bool ___knockbackHappened, float damage, Enemy.AttackImmunity _immunity = Enemy.AttackImmunity.SuperAttack, StatusEffect _status_effects = null, int _column = -1)
        {
			//Chance: 1/4
			float activationChance = 0.25f;

			float MysteriousBagEffectValue = 0;
			short trinketCounter = 0;
			while ((int)trinketCounter < __instance.app.model.characterSheet.trinkets.Count)
			{
				MysteriousBagEffectValue += __instance.app.controller.GetTrinket((int)trinketCounter).trinketName == TrinketName.MysteriousBag ? activationChance : 0f;
				trinketCounter += 1;
			}

			MysteriousBagEffectValue *= (float)__instance.app.controller.trinketController.EffectMultiplier();
			MysteriousBagEffectValue *= CollectibleChanges.TrinketLuckModifier(__instance.app.model.characterSheet);

			int MysteriousBagActivationCounter = CollectibleChanges.EffectActivationCounter(MysteriousBagEffectValue);

			//Replace Mysterious Bag effect
			if (__instance.dealSplashDamage && MysteriousBagActivationCounter > 0)
			{
				if (__instance.position.x > 0)
				{
					Enemy enemy = __instance.app.controller.VulnerableFromAbove(__instance.position.x - 1, __instance.position.y);
					if (enemy != null)
					{
						enemy.dealSplashDamage = false;
						enemy.Hurt((float)MysteriousBagActivationCounter, Enemy.AttackImmunity.ReduceSpellDamage, null, -1);
					}
				}
				if (__instance.position.x < 2)
				{
					Enemy enemy2 = __instance.app.controller.VulnerableFromAbove(__instance.position.x + 1, __instance.position.y);
					if (enemy2 != null)
					{
						enemy2.dealSplashDamage = false;
						enemy2.Hurt((float)MysteriousBagActivationCounter, Enemy.AttackImmunity.ReduceSpellDamage, null, -1);
					}
				}
				if (__instance.position.y > 0)
				{
					Enemy enemy3 = __instance.app.controller.VulnerableFromAbove(__instance.position.x, __instance.position.y - 1);
					if (enemy3 != null)
					{
						enemy3.dealSplashDamage = false;
						enemy3.Hurt((float)MysteriousBagActivationCounter, Enemy.AttackImmunity.ReduceSpellDamage, null, -1);
					}
				}
				if (__instance.position.y < 2)
				{
					Enemy enemy4 = __instance.app.controller.VulnerableFromAbove(__instance.position.x, __instance.position.y + 1);
					if (enemy4 != null)
					{
						enemy4.dealSplashDamage = false;
						enemy4.Hurt((float)MysteriousBagActivationCounter, Enemy.AttackImmunity.ReduceSpellDamage, null, -1);
					}
				}
			}
			else
			{
				__instance.dealSplashDamage = true;
			}
			if (_column == -1)
			{
				__instance.attackedFrom = __instance.position.x;
			}
			else
			{
				__instance.attackedFrom = _column;
			}
			MonoBehaviour.print("attacked from " + __instance.attackedFrom);
			if (!__instance.alive && !__instance.isPoop)
			{
				__instance.Resisted();
				return false;
			}
			if (damage != 0f && _immunity == __instance.attackImmunity)
			{
				if (_immunity == Enemy.AttackImmunity.ReducePuzzleDamage)
				{
					__instance.PuzzleResisted();
					damage = Mathf.Floor(damage / 4);
					if (damage > 0)
                    {
						Console.WriteLine("[The Legend of Bum-bo: Windfall] Reducing strength of enemy puzzle damage resistance");
					}
				}
				else if (_immunity == Enemy.AttackImmunity.ReduceSpellDamage)
				{
					__instance.SpellResisted();
					damage = Mathf.Floor(damage / 4);
					if (damage > 0)
					{
						Console.WriteLine("[The Legend of Bum-bo: Windfall] Reducing strength of enemy spell damage resistance");
					}
				}
			}
			bool flag = __instance.app.model.characterSheet.Critical(__instance);
			if (flag)
			{
				damage *= 2f;
				EnemyRoomView currentRoom = __instance.app.view.boxes.enemyRoom3x3.GetComponent<EnemyRoomView>();
				Sequence sequence = DOTween.Sequence();
				TweenSettingsExtensions.AppendCallback(TweenSettingsExtensions.Append(TweenSettingsExtensions.Append(TweenSettingsExtensions.Append(TweenSettingsExtensions.Append(TweenSettingsExtensions.AppendCallback(TweenSettingsExtensions.InsertCallback(sequence, 0.05f, delegate ()
				{
					__instance.app.controller.ShakeCamera(0.5f, 0.25f);
				}), delegate ()
				{
					currentRoom.hitLight.gameObject.SetActive(true);
				}), ShortcutExtensions.DOIntensity(currentRoom.hitLight, 1f, 0.1f)), ShortcutExtensions.DOIntensity(currentRoom.hitLight, 0.7f, 0.1f)), ShortcutExtensions.DOIntensity(currentRoom.hitLight, 0.9f, 0.1f)), ShortcutExtensions.DOIntensity(currentRoom.hitLight, 0f, 0.3f)), delegate ()
				{
					currentRoom.hitLight.gameObject.SetActive(false);
				});
				__instance.app.view.soundsView.PlayBumboSound(SoundsView.eSound.Critical_Hit, CharacterSheet.BumboType.TheBrave, -1);
				__instance.app.view.soundsView.PlayBumboSound(SoundsView.eSound.BumboHappy, __instance.app.model.characterSheet.bumboType, 0);
			}
			__instance.app.Notify("enemy.hurt", __instance, new object[0]);
			if (_status_effects == null)
			{
				_status_effects = new StatusEffect();
			}
			if (_status_effects.blind)
			{
				__instance.Blind(1, true);
			}
			if (__instance.app.controller.trinketController.WillBlind())
			{
				__instance.Blind(1, true);
			}
			if (_status_effects.poison)
			{
				__instance.Poison(1);
			}
			if (__instance.app.controller.trinketController.WillPoison())
			{
				__instance.Poison(1);
			}
			if (__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, __instance.position.y]].effect == BattlefieldEffect.Effect.Shield)
			{
				damage = 0f;
				__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, __instance.position.y]].TimeToDie();
				__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, __instance.position.y]] = new EmptyBFEffect();
			}
			for (int i = __instance.position.y; i < 3; i++)
			{
				if (__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, i]].effect == BattlefieldEffect.Effect.Fog && __instance.app.model.counteringFog.IndexOf(__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, i]].view.GetComponent<FogEffectView>()) < 0)
				{
					__instance.app.model.isFogCountering = true;
					__instance.app.model.counteringEnemy = __instance;
					__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, i]].view.GetComponent<FogEffectView>().x = __instance.position.x;
					__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, i]].view.GetComponent<FogEffectView>().y = i;
					__instance.app.model.counteringFog.Add(__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, i]].view.GetComponent<FogEffectView>());
					break;
				}
			}
			if (!___knockbackHappened && (__instance.PermanentKnockback || __instance.app.controller.trinketController.WillKnockback() || _status_effects.knockback >= UnityEngine.Random.Range(0f, 1f)))
			{
				__instance.Knockback();
			}
			if (_immunity == __instance.attackImmunity && damage > (float)__instance.reducedDamage)
			{
				damage = (float)__instance.reducedDamage;
			}
			damage = (float)__instance.app.model.aiModel.battlefieldEffects[__instance.app.model.aiModel.battlefieldPositionIndex[__instance.position.x, __instance.position.y]].AffectDamage((int)damage);
			__instance.health -= damage;
			__instance.app.view.soundsView.PlaySound(SoundsView.eSound.EnemiesHurt, __instance.transform.position, SoundsView.eAudioSlot.Default, true);
			if (__instance.alive && __instance.max_health != Enemy.HealthType.INVINCIBLE)
			{
				if (!__instance.boss && __instance.healthIndicator != null)
				{
					__instance.healthIndicator.GetComponent<HealthIndicatorView>().SetHealth((int)__instance.getHealth());
				}
				else if (__instance.boss)
				{
					__instance.app.view.GUICamera.GetComponent<GUISide>().bossHeartView.SetHealth((float)((int)__instance.getHealth()));
				}
			}
			if (damage > 0f)
			{
				__instance.app.controller.trinketController.GainRandomManaFromAttack();
				__instance.app.controller.trinketController.GainHealthFromAttack();
				__instance.app.controller.trinketController.GainAPFromAttack();
				__instance.app.controller.trinketController.OnEnemyHit(_immunity);
				Vector3 vector = (__instance.enemyType != Enemy.EnemyType.Ground) ? new Vector3(0f, 1f, -0.2f) : new Vector3(0f, 0.5f, -0.2f);
				__instance.app.view.hitParticles.Play(__instance.transform.position + vector);
				if (__instance.boogerCounter > 0 && !__instance.isPoop)
				{
					__instance.AnimateBoogered();
				}
				else
				{
					__instance.AnimateHurt();
				}
			}
			if (__instance.max_health != Enemy.HealthType.INVINCIBLE)
			{
				Vector3 vector2 = __instance.transform.position + new Vector3(0f, 0.5f, -0.15f);
				if (__instance.transform.Find("Attack Target") != null)
				{
					vector2 = __instance.transform.Find("Attack Target").transform.position + new Vector3(0f, 0.25f, -0.15f);
				}
				GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("Damage Indicator"), vector2, Quaternion.identity) as GameObject;
				gameObject.GetComponent<DamageView>().SetDamage(flag, (int)damage);
			}
			if (__instance.getHealth() <= 0f)
			{
				__instance.DeathGibs(flag);
				if (!__instance.boss)
				{
					__instance.timeToDie();
				}
				else
				{
					__instance.BossTimeToDie();
				}
			}
			else if (__instance.max_health != Enemy.HealthType.INVINCIBLE)
			{
				__instance.HurtGibs(flag);
			}
			if (__instance.alive && __instance.boogerCounter == 0 && __instance.getHealth() > 0f)
			{
				__instance.Counter();
			}
			___knockbackHappened = false;

			return false;
        }

    }

	public class BinomialDistribution
    {
		public static float CalculateBinomialDistribution(int numberOfEvents, int requiredSuccesses, float successProbability)
        {
			return CombinationFormula(numberOfEvents, requiredSuccesses) * (float)Math.Pow(successProbability, requiredSuccesses) * (float)Math.Pow(1 - successProbability, numberOfEvents - requiredSuccesses);
		}

		private static float CombinationFormula(int totalObjects, int objectsTaken)
        {
			if (totalObjects == objectsTaken) return 1;
			else return Factorial(totalObjects) / (Factorial(objectsTaken) * Factorial(totalObjects - objectsTaken));
		}

		private static int Factorial(int input)
        {
			int output = input;
			for (int i = input - 1; i >= 1; i--)
            {
				output *= i;
            }
			return output;
        }

	}
}
