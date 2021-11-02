﻿using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using DG.Tweening;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Reflection;
using TMPro;

namespace The_Legend_of_Bum_bo_Windfall
{
    class CollectibleChanges
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CollectibleChanges));
            Console.WriteLine("[The Legend of Bum-bo: Windfall] Applying collectible changes");
        }

		//Patch: Changes Thermos description 
		[HarmonyPostfix, HarmonyPatch(typeof(ThermosTrinket), MethodType.Constructor)]
		static void ThermosTrinket_Constructor(ThermosTrinket __instance)
		{
			__instance.Name = "Charge All Items + Heal";
		}

		public static UseTrinket currentTrinket;
		static int currentTrinketIndex;
		public static bool[] enabledSpells = new bool[6];
		//Patch: Allows the player to choose which spell to use Rainbow Tick on 
		[HarmonyPrefix, HarmonyPatch(typeof(RainbowTickTrinket), "Use")]
		static bool RainbowTickTrinket_Use(RainbowTickTrinket __instance, int _index)
		{
			//Loop through spells
			bool anyActiveSpells = false;
			int spellCounter = 0;
			while (spellCounter < __instance.app.model.characterSheet.spells.Count)
			{
				//Record which spells are disabled
				if (!__instance.app.view.spells[spellCounter].disableObject.activeSelf)
                {
					enabledSpells[spellCounter] = true;
                }
                else
                {
					enabledSpells[spellCounter] = false;
				}

				//Enable/disable spells
				if (CalculateCostReduction(spellCounter, 0.15f, __instance.app, false) > 0)
                {
					__instance.app.view.spells[spellCounter].EnableSpell();
					anyActiveSpells = true;
				}
                else
                {
					__instance.app.view.spells[spellCounter].DisableSpell();
				}
				spellCounter += 1;
			}

			//Abort if there are no viable spells
			if (!anyActiveSpells)
            {
				for (int spellCounter2 = 0; spellCounter2 < __instance.app.model.characterSheet.spells.Count; spellCounter2++)
				{
					if (enabledSpells[spellCounter2])
					{
						__instance.app.view.spells[spellCounter2].EnableSpell();
					}
					else
					{
						__instance.app.view.spells[spellCounter2].DisableSpell();
					}
				}
				__instance.app.controller.GUINotification("No Viable Spells", GUINotificationView.NotifyType.General, null, true);
				return false;
			}

			currentTrinket = __instance;
			currentTrinketIndex = _index;

			__instance.app.model.spellModel.currentSpell = null;
			__instance.app.model.spellModel.spellQueued = false;

			__instance.app.model.spellViewUsed = null;

			__instance.app.controller.eventsController.SetEvent(new SpellModifySpellEvent());
			__instance.app.controller.GUINotification("Pick A Spell To Modify", GUINotificationView.NotifyType.Spell, null, false);

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing Rainbow Tick effect");
			return false;
		}
		//Patch: Changes Rainbow Tick description 
		[HarmonyPostfix, HarmonyPatch(typeof(RainbowTickTrinket), MethodType.Constructor)]
		static void RainbowTickTrinket_Constructor(RainbowTickTrinket __instance)
		{
			__instance.Name = "Reduces Spell Cost";
		}

		//Patch: Allows the player to choose which spell to use Brown Tick on 
		[HarmonyPrefix, HarmonyPatch(typeof(BrownTickTrinket), "Use")]
		static bool BrownTickTrinket_Use(BrownTickTrinket __instance, int _index)
		{
			//Loop through spells
			bool anyActiveSpells = false;
			int spellCounter = 0;
			while (spellCounter < __instance.app.model.characterSheet.spells.Count)
			{
				//Record which spells are disabled
				if (!__instance.app.view.spells[spellCounter].disableObject.activeSelf)
				{
					enabledSpells[spellCounter] = true;
				}
				else
				{
					enabledSpells[spellCounter] = false;
				}

				//Enable/disable spells
				if (__instance.app.model.characterSheet.spells[spellCounter].IsChargeable && __instance.app.model.characterSheet.spells[spellCounter].requiredCharge > 0)
				{
					__instance.app.view.spells[spellCounter].EnableSpell();
					anyActiveSpells = true;
				}
				else
				{
					__instance.app.view.spells[spellCounter].DisableSpell();
				}
				spellCounter += 1;
			}

			//Abort if there are no viable spells
			if (!anyActiveSpells)
			{
				for (int spellCounter2 = 0; spellCounter2 < __instance.app.model.characterSheet.spells.Count; spellCounter2++)
				{
					if (enabledSpells[spellCounter2])
					{
						__instance.app.view.spells[spellCounter2].EnableSpell();
					}
					else
					{
						__instance.app.view.spells[spellCounter2].DisableSpell();
					}
				}
				__instance.app.controller.GUINotification("No Viable Spells", GUINotificationView.NotifyType.General, null, true);
				return false;
			}

			currentTrinket = __instance;
			currentTrinketIndex = _index;

			__instance.app.model.spellModel.currentSpell = null;
			__instance.app.model.spellModel.spellQueued = false;

			__instance.app.model.spellViewUsed = null;

			__instance.app.controller.eventsController.SetEvent(new SpellModifySpellEvent());
			__instance.app.controller.GUINotification("Pick A Spell To Modify", GUINotificationView.NotifyType.Spell, null, false);

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing Brown Tick effect");
			return false;
		}

		//Patch: Implements trinket modify spell effects
		//Rainbow Tick
		//Brown Tick
		[HarmonyPrefix, HarmonyPatch(typeof(SpellView), "OnMouseDown")]
		static bool SpellView_OnMouseDown(SpellView __instance, bool ___exit, SpellElement ___spell)
		{
			if (!__instance.app.model.paused && !___exit && ___spell != null && !__instance.disableObject.activeSelf && __instance.app.model.bumboEvent.GetType().ToString() == "SpellModifySpellEvent" && currentTrinket != null)
			{
				__instance.app.view.soundsView.PlaySound(SoundsView.eSound.Button, SoundsView.eAudioSlot.Default, false);

				CollectibleFixes.UseTrinket_Use_Prefix(currentTrinket, currentTrinketIndex);
				CollectibleFixes.UseTrinket_Use_Base_Method(currentTrinket, currentTrinketIndex);
				CollectibleFixes.UseTrinket_Use_Postfix(currentTrinket);

				switch (currentTrinket.trinketName)
                {
					case TrinketName.RainbowTick:
						SpellElement spellElement = ___spell;

						int costReduction = CalculateCostReduction(__instance.spellIndex, 0.15f, __instance.app, false);

						for (int j = costReduction; j > 0; j--)
						{
							//Find colors with cost above 0
							List<int> availableColors = new List<int>();
							for (int k = 0; k < 6; k++)
							{
								if (spellElement.Cost[k] != 0)
								{
									availableColors.Add(k);
								}
							}
							//Choose random color to reduce
							int randomColor = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
							short[] cost = __instance.app.model.characterSheet.spells[__instance.spellIndex].Cost;
							cost[randomColor] -= 1;

							int totalCombinedCost = 0;
							//Increase the reduced color's cost modifier if the spell's total cost (including modifier) would be reduced below minimum OR if the reduced color's cost (including modifier) would be reduced below zero
							for (int costCounter = 0; costCounter < 6; costCounter++)
                            {
								totalCombinedCost += (short)(__instance.app.model.characterSheet.spells[__instance.spellIndex].Cost[costCounter] + __instance.app.model.characterSheet.spells[__instance.spellIndex].CostModifier[costCounter]);
							}
							if (totalCombinedCost < SpellManaCosts.MinimumManaCost(__instance.app.model.characterSheet.spells[__instance.spellIndex].spellName) || __instance.app.model.characterSheet.spells[__instance.spellIndex].Cost[randomColor] + __instance.app.model.characterSheet.spells[__instance.spellIndex].CostModifier[randomColor] < 0)
                            {
								__instance.app.model.characterSheet.spells[__instance.spellIndex].CostModifier[randomColor] += 1;
							}
						}
						break;
					case TrinketName.BrownTick:
						//Reduce recharge time
						if (__instance.app.model.characterSheet.spells[__instance.spellIndex].requiredCharge > 0)
                        {
							__instance.app.model.characterSheet.spells[__instance.spellIndex].requiredCharge--;
						}
						if (__instance.app.model.characterSheet.spells[__instance.spellIndex].requiredCharge < __instance.app.model.characterSheet.spells[__instance.spellIndex].charge)
						{
							__instance.app.model.characterSheet.spells[__instance.spellIndex].charge = __instance.app.model.characterSheet.spells[__instance.spellIndex].requiredCharge;
						}
						if (__instance.app.model.characterSheet.spells[__instance.spellIndex].requiredCharge == 0)
						{
							__instance.app.model.characterSheet.spells[__instance.spellIndex].chargeEveryRound = true;
							__instance.app.model.characterSheet.spells[__instance.spellIndex].usedInRound = false;
						}
						break;
				}

				for (int spellCounter2 = 0; spellCounter2 < __instance.app.model.characterSheet.spells.Count; spellCounter2++)
				{
					if (enabledSpells[spellCounter2])
					{
						__instance.app.view.spells[spellCounter2].EnableSpell();
					}
					else
					{
						__instance.app.view.spells[spellCounter2].DisableSpell();
					}
				}

				__instance.app.controller.SetActiveSpells(true, true);
				__instance.app.controller.UpdateSpellManaText();
				currentTrinket = null;

				__instance.app.view.soundsView.PlaySound(SoundsView.eSound.ItemUpgraded, SoundsView.eAudioSlot.Default, false);
				__instance.app.view.spells[__instance.spellIndex].spellParticles.Play();
				__instance.Shake(1f);
				__instance.app.controller.HideNotifications(false);

				__instance.app.model.spellModel.currentSpell = null;
				__instance.app.model.spellModel.spellQueued = false;

				if (__instance.app.model.bumboEvent.GetType().ToString() == "SpellModifySpellEvent")
                {
					__instance.app.controller.eventsController.EndEvent();
				}

				Console.WriteLine("[The Legend of Bum-bo: Windfall] Implementing trinket modify spell effect");
				return false;
			}
			return true;
		}

		//Patch: Changes starting stats and collectibles of characters
		//Increases the cost of Bum-bo the Dead's attack fly
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterSheet), "Awake")]
		static void CharacterSheet_Awake(CharacterSheet __instance)
		{
			StartingSpell[] deadStartingSpells = __instance.bumboList[(int)CharacterSheet.BumboType.TheDead].startingSpells;
			for (int i = 0; i < deadStartingSpells.Length; i++)
            {
				StartingSpell deadStartingSpell = deadStartingSpells[i];
				if (deadStartingSpell.spell == SpellName.AttackFly)
                {
					deadStartingSpell.toothCost = 6;
				}
			}

			StartingSpell[] weirdStartingSpells = __instance.bumboList[(int)CharacterSheet.BumboType.TheWeird].startingSpells;
			for (int i = 0; i < weirdStartingSpells.Length; i++)
			{
				StartingSpell weirdStartingSpell = weirdStartingSpells[i];
				if (weirdStartingSpell.spell == SpellName.MagicMarker)
				{
					weirdStartingSpell.peeCost = 6;
				}
			}
		}

		//Patch: Spell mana cost text now indicates whether the cost is temporarily modified
		[HarmonyPrefix, HarmonyPatch(typeof(BumboController), "UpdateSpellManaText", new Type[] { typeof(int), typeof(SpellElement) })]
		static bool BumboController_UpdateSpellManaText(BumboController __instance, int _spell_index, SpellElement _spell)
		{
			if (!_spell.IsChargeable)
			{
				float num = 0f;
				__instance.app.view.spells[_spell_index].spellMana1.SetActive(false);
				for (short num2 = 0; num2 < 5; num2 += 1)
				{
					int num3 = (int)num2;
					if (num2 > 0)
					{
						num3++;
					}
					if (_spell.Cost[num3] > 0 || _spell.CostModifier[num3] > 0)
					{
						__instance.app.view.spells[_spell_index].manaIconViews[num3].gameObject.transform.localPosition = new Vector3(-0.13f + 0.085f * num, 0.02f, 0f);
						__instance.app.view.spells[_spell_index].manaIconViews[num3].gameObject.SetActive(true);
						__instance.app.view.spells[_spell_index].manaIconViews[num3].SetMana((int)(_spell.Cost[num3] + _spell.CostModifier[num3]));
						num += 1f;

						//Change text color
						if (_spell.CostModifier[num3] < 0)
						{
							__instance.app.view.spells[_spell_index].manaIconViews[num3].amount.color = new Color(0.005f, 0.05f, 0.2f);
						}
						else if (_spell.CostModifier[num3] > 0)
						{
							__instance.app.view.spells[_spell_index].manaIconViews[num3].amount.color = new Color(0.2f, 0.005f, 0.005f);
						}
						else
						{
							__instance.app.view.spells[_spell_index].manaIconViews[num3].amount.color = Color.black;
						}
					}
					else
					{
						__instance.app.view.spells[_spell_index].manaIconViews[num3].gameObject.SetActive(false);
					}
				}
			}
			else
			{
				__instance.app.view.spells[_spell_index].spellMana1.SetActive(true);
				for (short num4 = 0; num4 < 5; num4 += 1)
				{
					int num5 = (int)num4;
					if (num4 > 0)
					{
						num5++;
					}
					__instance.app.view.spells[_spell_index].manaIconViews[num5].gameObject.SetActive(false);
				}
				short num6 = 0;
				while ((int)num6 < __instance.app.view.spells[_spell_index].spellMana2.Length)
				{
					__instance.app.view.spells[_spell_index].spellMana2[(int)num6].SetActive(false);
					num6 += 1;
				}
				short num7 = 0;
				while ((int)num7 < __instance.app.view.spells[_spell_index].spellMana3.Length)
				{
					__instance.app.view.spells[_spell_index].spellMana3[(int)num7].SetActive(false);
					num7 += 1;
				}
				__instance.app.view.spells[_spell_index].spellMana1.GetComponent<MeshRenderer>().material.SetTextureOffset("_MainTex", new Vector2(0.5f, 0.255f));
				__instance.app.view.spells[_spell_index].spellMana1.transform.GetChild(0).GetComponent<TextMeshPro>().text = _spell.charge + " / " + _spell.requiredCharge;
			}
			return false;
		}

		//Access base method
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(SpellElement), nameof(SpellElement.CastSpell))]
		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool CastSpellDummy_GoldenTickSpell(GoldenTickSpell instance) { return false; }
		//Patch: Golden Tick rework
		[HarmonyPrefix, HarmonyPatch(typeof(GoldenTickSpell), "CastSpell")]
		static bool SleightOfHandSpell_CastSpell(GoldenTickSpell __instance, ref bool __result)
		{
			if (!CastSpellDummy_GoldenTickSpell(__instance))
			{
				__result = false;
				return false;
			}
			__instance.app.model.spellModel.currentSpell = null;
			__instance.app.model.spellModel.spellQueued = false;
			for (int i = 0; i < __instance.app.model.characterSheet.spells.Count; i++)
			{
				SpellElement spellElement = __instance.app.model.characterSheet.spells[i];
				if (!spellElement.IsChargeable)
				{
					int costReduction = CalculateCostReduction(i, 0.4f, __instance.app, true);

					for (int k = costReduction; k > 0; k--)
					{
						List<int> availableColors = new List<int>();
						for (int l = 0; l < 6; l++)
						{
							if (spellElement.Cost[l] + spellElement.CostModifier[l] != 0)
							{
								availableColors.Add(l);
							}
						}
						if (availableColors.Count > 0)
						{
							int randomColor = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
							spellElement.CostModifier[randomColor] -= 1;
						}
					}
				}
				else if (spellElement != __instance)

				{
					spellElement.ChargeSpell();
				}
			}
			__instance.charge = 0;
			__instance.app.controller.UpdateSpellManaText();
			__instance.app.controller.SetActiveSpells(true, true);
			__instance.app.controller.GUINotification("Make\nSpells Easier\nTo Cast!", GUINotificationView.NotifyType.Spell, __instance, true);
			__instance.app.controller.eventsController.SetEvent(new IdleEvent());
			__result = true;

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing Golden Tick effect");
			return false;
		}

		//Access base method
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(SpellElement), nameof(SpellElement.CastSpell))]
		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool CastSpellDummy_SleightOfHandSpell(SleightOfHandSpell instance) { return false; }
		//Patch: Sleight of Hand rework
		[HarmonyPrefix, HarmonyPatch(typeof(SleightOfHandSpell), "CastSpell")]
		static bool SleightOfHandSpell_CastSpell(SleightOfHandSpell __instance, ref bool __result)
		{
			if (!CastSpellDummy_SleightOfHandSpell(__instance))
			{
				__result = false;
				return false;
			}
			__instance.app.model.spellModel.currentSpell = null;
			__instance.app.model.spellModel.spellQueued = false;
			for (int i = 0; i < __instance.app.model.characterSheet.spells.Count; i++)
			{
				SpellElement spellElement = __instance.app.model.characterSheet.spells[i];
				if (!spellElement.IsChargeable && spellElement != __instance)
				{
					int costReduction = CalculateCostReduction(i, 0.25f, __instance.app, true);

					for (int k = costReduction; k > 0; k--)
					{
						List<int> availableColors = new List<int>();
						for (int l = 0; l < 6; l++)
						{
							if (spellElement.Cost[l] + spellElement.CostModifier[l] != 0)
							{
								availableColors.Add(l);
							}
						}
						if (availableColors.Count > 0)
						{
							int randomColor = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
							spellElement.CostModifier[randomColor] -= 1;
						}
					}
				}
			}
			__instance.app.controller.UpdateSpellManaText();
			__instance.app.controller.SetActiveSpells(true, true);
			__instance.app.controller.GUINotification("Spells\nCost Less\nIn Room!", GUINotificationView.NotifyType.Spell, __instance, true);
			__instance.app.controller.eventsController.SetEvent(new IdleEvent());
			SoundsView.Instance.PlaySound(SoundsView.eSound.Spell_LowerCost, SoundsView.eAudioSlot.Default, false);
			__result = true;

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing Sleight of Hand effect");
			return false;
		}

		//Patch: Damage needles can no longer be used on spells that will not be upgraded by the needle
		[HarmonyPostfix, HarmonyPatch(typeof(DamagePrickTrinket), "QualifySpell")]
		static void DamagePrickTrinket_QualifySpell(DamagePrickTrinket __instance, int _spell_index)
		{
			SpellElement spellElement = __instance.app.model.characterSheet.spells[_spell_index];
			if (spellElement.spellName == SpellName.Ecoli || spellElement.spellName == SpellName.ExorcismKit || spellElement.spellName == SpellName.MegaBean || spellElement.spellName == SpellName.PuzzleFlick)
			{
				__instance.app.view.spells[_spell_index].DisableSpell();
				Console.WriteLine("[The Legend of Bum-bo: Windfall] Disabling attack spell that won't be affected by damage needle");
			}
		}
		[HarmonyPrefix, HarmonyPatch(typeof(Shop), "AddDamagePrick")]
		static bool Shop_AddDamagePrick(Shop __instance, ref List<TrinketName> ___needles)
		{
			short num = 0;
			while ((int)num < __instance.app.model.characterSheet.spells.Count)
			{
				SpellElement spellElement = __instance.app.model.characterSheet.spells[(int)num];
				if (spellElement.Category == SpellElement.SpellCategory.Attack && !(spellElement.spellName == SpellName.Ecoli || spellElement.spellName == SpellName.ExorcismKit || spellElement.spellName == SpellName.MegaBean || spellElement.spellName == SpellName.PuzzleFlick))
				{
					___needles.Add(TrinketName.DamagePrick);
					return false;
				}
				num += 1;
			}
			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing damage needle appearance condition");
			return false;
		}

		//Patch: Charge needles can no longer be used on spells that will not be upgraded by the needle
		[HarmonyPostfix, HarmonyPatch(typeof(ChargePrickTrinket), "QualifySpell")]
		static void ChargePrickTrinket_QualifySpell(ChargePrickTrinket __instance, int _spell_index)
		{
			if (__instance.app.model.characterSheet.spells[_spell_index].requiredCharge == 0)
			{
				__instance.app.view.spells[_spell_index].DisableSpell();
				Console.WriteLine("[The Legend of Bum-bo: Windfall] Disabling item that won't be affected by charge needle");
			}
		}

		public static int CalculateCostReduction(int _spell_index, float reductionPercentage, BumboApplication bumboApplication, bool temporaryCost)
        {
			//Calculate cost reduction
			int totalManaCost = 0;
			for (int i = 0; i < 6; i++)
			{
				totalManaCost += (int)bumboApplication.model.characterSheet.spells[_spell_index].Cost[i];
				if (temporaryCost)
                {
					totalManaCost += (int)bumboApplication.model.characterSheet.spells[_spell_index].CostModifier[i];
				}
			}

			int costReduction = Mathf.RoundToInt((float)totalManaCost * reductionPercentage);

			//Do not reduce total cost below minimum
			while (totalManaCost - costReduction < SpellManaCosts.MinimumManaCost(bumboApplication.model.characterSheet.spells[_spell_index].spellName))
			{
				costReduction--;
				if (costReduction <= 0)
				{
					break;
				}
			}
			return costReduction;
		}

		//Patch: Mana needle rework
		[HarmonyPostfix, HarmonyPatch(typeof(ManaPrickTrinket), "QualifySpell")]
		static void ManaPrickTrinket_QualifySpell(ManaPrickTrinket __instance, int _spell_index)
		{
			int costReduction = CalculateCostReduction(_spell_index, 0.25f, __instance.app, false);

			//Enable spell if cost reduction is above zero
			if (costReduction > 0)
            {
				__instance.app.view.spells[_spell_index].EnableSpell();
			}
            else
            {
				__instance.app.view.spells[_spell_index].DisableSpell();
			}

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing mana needle spell qualification");
		}
		[HarmonyPrefix, HarmonyPatch(typeof(ManaPrickTrinket), "UpdateSpell")]
		static bool ManaPrickTrinket_UpdateSpell(ManaPrickTrinket __instance, int _spell_index)
		{
			SpellElement spellElement = __instance.app.model.characterSheet.spells[_spell_index];

			int costReduction = CalculateCostReduction(_spell_index, 0.25f, __instance.app, false);

			for (int j = costReduction; j > 0; j--)
			{
				//Find colors with cost above 0
				List<int> availableColors = new List<int>();
				for (int k = 0; k < 6; k++)
				{
					if (spellElement.Cost[k] != 0)
					{
						availableColors.Add(k);
					}
				}
				//Choose random color to reduce
				int randomColor = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
				short[] cost = __instance.app.model.characterSheet.spells[_spell_index].Cost;
				cost[randomColor] -= 1;
			}
			__instance.app.controller.UpdateSpellManaText();
			__instance.app.view.soundsView.PlaySound(SoundsView.eSound.ItemUpgraded, SoundsView.eAudioSlot.Default, false);
			__instance.app.view.spells[_spell_index].spellParticles.Play();

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing mana needle effect");
			return false;
		}
		[HarmonyPrefix, HarmonyPatch(typeof(Shop), "AddManaPrick")]
		static bool Shop_AddManaPrick(Shop __instance, ref List<TrinketName> ___needles)
		{
			short num = 0;
			while ((int)num < __instance.app.model.characterSheet.spells.Count)
			{
				int costReduction = CalculateCostReduction(num, 0.25f, __instance.app, false);
				if (costReduction > 0)
				{
					___needles.Add(TrinketName.ManaPrick);
					Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing mana needle appearance condition");
					return false;
				}
				num += 1;
			}
			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing mana needle appearance condition");
			return false;
		}

		//Patch: Bum-bo the Dead will now not encounter Shuffle Needles instead of Mana Needles
		//Since spell mana cost reduction is now preserved when the cost is rerolled, mana needles are useful to Bum-bo the Dead
		//Shuffle needles on the other hand are pretty pointless
		//Also saves and loads shop
		[HarmonyPrefix, HarmonyPatch(typeof(Shop), "Init")]
		static bool Shop_Init(Shop __instance, ref List<TrinketName> ___needles, ref GameObject ___item1Pickup, ref GameObject ___item2Pickup, ref GameObject ___item3Pickup, ref GameObject ___item4Pickup, ref TrinketModel ___trinketModel)
		{
			if (PlayerPrefs.GetInt("loadGambling", 0) == 1)
            {
				//Load shop
				XmlDocument lDoc = (XmlDocument)typeof(SavedStateController).GetField("lDoc", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance.app.controller.savedStateController);

				if (lDoc != null)
				{
					XmlNode xmlNode = lDoc.SelectSingleNode("/save/gambling");

					if (xmlNode != null)
					{
						XmlNodeList xmlNodeList = xmlNode.SelectNodes("pickup");

						bool[] savedPickups = new bool[4];

						for (int i = 0; i < xmlNodeList.Count; i++)
						{
							XmlNode xmlNode2 = xmlNodeList.Item(i);

							int index = Convert.ToInt32(xmlNode2.Attributes["index"].Value);
							savedPickups[i] = true;

							string type = xmlNode2.Attributes["type"].Value;

							GameObject itemPickup = null;
							GameObject item = null;
							ItemPriceView itemPrice = null;
							switch (index)
							{
								case 0:
									item = __instance.item1;
									itemPrice = __instance.item1Price;
									break;
								case 1:
									item = __instance.item2;
									itemPrice = __instance.item2Price;
									break;
								case 2:
									item = __instance.item3;
									itemPrice = __instance.item3Price;
									break;
								case 3:
									item = __instance.item4;
									itemPrice = __instance.item4Price;
									break;
							}

							if (type == "heart")
                            {
								itemPickup = (GameObject)AccessTools.Method(typeof(Shop), "AddHeart").Invoke(__instance, new object[] { item, itemPrice });
							}
							else if (type == "trinket")
                            {
								TrinketName trinketName = (TrinketName)Enum.Parse(typeof(TrinketName), xmlNode2.Attributes["trinketName"].Value);

								if (__instance.app.model.trinketModel.trinkets[trinketName].Category != TrinketElement.TrinketCategory.Prick)
                                {
									if (___trinketModel == null)
                                    {
										___trinketModel = __instance.gameObject.AddComponent<TrinketModel>();
									}
								}

								itemPickup = (GameObject)AccessTools.Method(typeof(Shop), "AddTrinket").Invoke(__instance, new object[] { item, itemPrice });
								int price = 7;
								switch (trinketName)
                                {
									case TrinketName.ManaPrick:
										price = 8;
										break;
									case TrinketName.DamagePrick:
										price = 5;
										break;
									case TrinketName.ChargePrick:
										price = 8;
										break;
									case TrinketName.ShufflePrick:
										price = 2;
										break;
									case TrinketName.RandomPrick:
										price = 5;
										break;
								}

								TrinketPickupView trinketPickupView = itemPickup.GetComponent<TrinketPickupView>();
								trinketPickupView.SetTrinket(trinketName, price);
								itemPrice.SetPrice(price);
								trinketPickupView.removePickup = true;

								switch (index)
								{
									case 0:
										 ___item1Pickup = itemPickup;
										break;
									case 1:
										___item2Pickup = itemPickup;
										break;
									case 2:
										___item3Pickup = itemPickup;
										break;
									case 3:
										___item4Pickup = itemPickup;
										break;
								}
							}
						}

						if (___item1Pickup != null)
						{
							___item1Pickup.GetComponent<TrinketPickupView>().shopIndex = 0;
						}
						if (___item2Pickup != null)
						{
							___item2Pickup.GetComponent<TrinketPickupView>().shopIndex = 1;
						}
						if (___item3Pickup != null)
						{
							___item3Pickup.GetComponent<TrinketPickupView>().shopIndex = 2;
						}

						return false;
					}
				}
            }

			___needles = new List<TrinketName>();
			if (__instance.app.model.characterSheet.bumboType != CharacterSheet.BumboType.Eden)
			{
				if (__instance.app.model.characterSheet.bumboType != CharacterSheet.BumboType.TheDead)
				{
					//Shuffle Needle
					AccessTools.Method(typeof(Shop), "AddShufflePrick").Invoke(__instance, null);
				}
				AccessTools.Method(typeof(Shop), "AddDamagePrick").Invoke(__instance, null);
				AccessTools.Method(typeof(Shop), "AddChargePrick").Invoke(__instance, null);
				AccessTools.Method(typeof(Shop), "AddRandomPrick").Invoke(__instance, null);
				//Mana Needle
				AccessTools.Method(typeof(Shop), "AddManaPrick").Invoke(__instance, null);

				if (___needles.Count > 0)
				{
					___item1Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddNeedle").Invoke(__instance, new object[] { __instance.item1, __instance.item1Price });
				}
				if (___needles.Count > 0)
				{
					___item2Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddNeedle").Invoke(__instance, new object[] { __instance.item2, __instance.item2Price });
				}
				if (___needles.Count > 0)
				{
					___item3Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddNeedle").Invoke(__instance, new object[] { __instance.item3, __instance.item3Price });
				}
			}
			else
			{
				___trinketModel = __instance.gameObject.AddComponent<TrinketModel>();
				___item1Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddTrinket").Invoke(__instance, new object[] { __instance.item1, __instance.item1Price });
				___item2Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddTrinket").Invoke(__instance, new object[] { __instance.item2, __instance.item2Price });
				___item3Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddTrinket").Invoke(__instance, new object[] { __instance.item3, __instance.item3Price });
			}
			if (__instance.app.model.characterSheet.bumboType != CharacterSheet.BumboType.TheLost)
			{
				___item4Pickup = (GameObject)AccessTools.Method(typeof(Shop), "AddHeart").Invoke(__instance, new object[] { __instance.item4, __instance.item4Price });
			}
			if (___item1Pickup != null)
			{
				___item1Pickup.GetComponent<TrinketPickupView>().shopIndex = 0;
			}
			if (___item2Pickup != null)
			{
				___item2Pickup.GetComponent<TrinketPickupView>().shopIndex = 1;
			}
			if (___item3Pickup != null)
			{
				___item3Pickup.GetComponent<TrinketPickupView>().shopIndex = 2;
			}
			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing shop generation");

			if (PlayerPrefs.GetInt("loadGambling", 0) == 0)
			{
				//Save shop
				SaveChanges.SaveShop(__instance);
			}
			return false;
		}

		//Patch: Reworks mana cost generation
		//Certain spells have new base mana costs
		//The number of mana colors is now determined in a flexible way
		//Permanent and temporary mana cost reduction is now preserved when rerolling spell costs
		//Converter special mana cost generation is preserved when its mana cost is rerolled
		[HarmonyPrefix, HarmonyPatch(typeof(BumboController), "SetSpellCost", new Type[] { typeof(SpellElement), typeof(bool[]) })]
        static bool BumboController_SetSpellCost(BumboController __instance, SpellElement _spell, bool[] _ignore_mana, ref SpellElement __result)
        {
			if (_spell.spellName.ToString().Contains("Converter"))
            {
				Block.BlockType blockType = Block.BlockType.Bone;
				switch (_spell.spellName)
                {
					case SpellName.ConverterWhite:
						blockType = Block.BlockType.Bone;
						break;
					case SpellName.ConverterBrown:
						blockType = Block.BlockType.Poop;
						break;
					case SpellName.ConverterGreen:
						blockType = Block.BlockType.Booger;
						break;
					case SpellName.ConverterGrey:
						blockType = Block.BlockType.Tooth;
						break;
					case SpellName.ConverterYellow:
						blockType = Block.BlockType.Pee;
						break;
				}

				List<Block.BlockType> list = new List<Block.BlockType>();
				for (int i = 0; i < 6; i++)
				{
					if (i != 1 && i != (int)blockType)
					{
						list.Add((Block.BlockType)i);
					}
				}
				short[] array = new short[6];
				for (int j = 0; j < 2; j++)
				{
					int index = UnityEngine.Random.Range(0, list.Count);
					Block.BlockType chosenBlockType = list[index];
					array[(int)chosenBlockType] += 1;
					list.RemoveAt(index);
				}
				_spell.Cost = array;
				__result = _spell;

				Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing Converter mana cost generation");
				return false;
			}

			if (_ignore_mana == null)
			{
				_ignore_mana = new bool[6];
			}
			if (!_spell.IsChargeable && _spell.setCost)
			{
				//New mana costs
				int totalSpellCost = -1;

				int value;
				if (SpellManaCosts.manaCosts.TryGetValue(_spell.spellName, out value))
				{
					totalSpellCost = value;
				}

				List<short> spellCost = new List<short>();

				//Old mana costs
				if (totalSpellCost == -1)
				{
					switch (_spell.manaSize)
					{
						case SpellElement.ManaSize.S:
							totalSpellCost = 2;
							break;
						case SpellElement.ManaSize.M:
							totalSpellCost = 4;
							break;
						case SpellElement.ManaSize.L:
							totalSpellCost = 6;
							break;
						case SpellElement.ManaSize.XL:
							totalSpellCost = 10;
							break;
						case SpellElement.ManaSize.XXL:
							totalSpellCost = 16;
							break;
						case SpellElement.ManaSize.XXXL:
							totalSpellCost = 20;
							break;
						default:
							totalSpellCost = 1;
							break;
					}
				}

				//Preserve mana cost reduction
				int currentSpellCost = 0;
				for (int i = 0; i < _spell.Cost.Length; i++)
				{
					currentSpellCost += _spell.Cost[i];
				}
				if (totalSpellCost > currentSpellCost && currentSpellCost > 0)
				{
					totalSpellCost = currentSpellCost;
				}

				//Choose number of colors of mana
				int maximumColorCount;
				if (totalSpellCost < 4)
				{
					maximumColorCount = 1;
				}
				else if (totalSpellCost < 6)
				{
					maximumColorCount = 2;
				}
				else
				{
					maximumColorCount = 3;
				}

				int minimumColorCount;
				if (totalSpellCost > 16)
				{
					minimumColorCount = 3;
				}
				else if (totalSpellCost > 7)
				{
					minimumColorCount = 2;
				}
				else
				{
					minimumColorCount = 1;
				}

				int colorCount;
				if (__instance.app.model.characterSheet.bumboType == CharacterSheet.BumboType.TheStout)
				{
					colorCount = minimumColorCount;
				}
				else
				{
					//Random float
					float rand = UnityEngine.Random.Range((float)minimumColorCount - 0.5f, (float)maximumColorCount + 0.5f);

					//Another random float
					float rand2 = UnityEngine.Random.Range((float)minimumColorCount - 0.5f, (float)maximumColorCount + 0.5f);

					//Lower number is chosen, then rounded to the nearest integer; lower color counts are more likely
					colorCount = Mathf.RoundToInt(rand < rand2 ? rand : rand2);

					//Reduce impact of weighted randomness
					if (UnityEngine.Random.Range(0, 1f) < 0.5f)
                    {
						colorCount = Mathf.RoundToInt(rand);
					}

					//Failsafe
					if (colorCount < minimumColorCount)
                    {
						colorCount = minimumColorCount;
                    }
					else if (colorCount > maximumColorCount)
                    {
						colorCount = maximumColorCount;
                    }
				}

				for (int j = colorCount; j > 0; j--)
				{
					spellCost.Add(0);
				}

				//Generate spell cost
				int cheapestColorIndex = 0;
				for (int k = totalSpellCost; k > 0; k--)
				{
					int cheapestColor = 99;
					for (int l = 0; l < spellCost.Count; l++)
					{
						if ((int)spellCost[l] < cheapestColor)
						{
							cheapestColor = (int)spellCost[l];
							cheapestColorIndex = l;
						}
					}
					spellCost[cheapestColorIndex] += 1;
				}
				List<ManaType> bannedColors = new List<ManaType>
				{
					ManaType.Bone,
					ManaType.Booger,
					ManaType.Pee,
					ManaType.Poop,
					ManaType.Tooth
				};
				for (int m = 0; m < __instance.app.model.characterSheet.spells.Count; m++)
				{
					if (bannedColors.Count > 0)
					{
						for (int n = bannedColors.Count - 1; n >= 0; n--)
						{
							if (_ignore_mana[(int)bannedColors[n]])
							{
								//Ban ignored colors
								bannedColors.RemoveAt(n);
							}
							else if (__instance.app.model.characterSheet.spells[m].Cost[(int)bannedColors[n]] != 0)
							{
								//Ban colors of existing spells
								bannedColors.RemoveAt(n);
							}
						}
					}
				}
				List<ManaType> list6 = new List<ManaType>();
				_spell.Cost = new short[6];
				for (int num10 = 0; num10 < spellCost.Count; num10++)
				{
					if (bannedColors.Count == 0)
					{
						for (int num11 = 0; num11 < 6; num11++)
						{
							if (num11 != 1 && list6.IndexOf((ManaType)num11) < 0)
							{
								bannedColors.Add((ManaType)num11);
							}
						}
					}
					int index2 = UnityEngine.Random.Range(0, bannedColors.Count);
					_spell.Cost[(int)bannedColors[index2]] = spellCost[num10];
					list6.Add(bannedColors[index2]);
					bannedColors.RemoveAt(index2);
				}

				//Preserve temporary mana cost reduction
				short num12 = 0;
				for (int num13 = 0; num13 < _spell.CostModifier.Length; num13++)
				{
					num12 -= _spell.CostModifier[num13];
					_spell.CostModifier[num13] = 0;
				}
				for (int num14 = (int)num12; num14 > 0; num14--)
				{
					List<int> list7 = new List<int>();
					for (int num15 = 0; num15 < 6; num15++)
					{
						if (_spell.Cost[num15] + _spell.CostModifier[num15] > 0)
						{
							list7.Add(num15);
						}
					}
					if (list6.Count > 0)
					{
						int num16 = list7[UnityEngine.Random.Range(0, list7.Count)];
						short[] costModifier = _spell.CostModifier;
						int num17 = num16;
						costModifier[num17] -= 1;
					}
				}
			}
			__result = _spell;

			Console.WriteLine("[The Legend of Bum-bo: Windfall] Changing spell mana cost generation");
            return false;
        }

        //Patch: Pentagram no longer provides puzzle damage
        [HarmonyPostfix, HarmonyPatch(typeof(PentagramSpell), "CastSpell")]
        static void PentagramSpell_CastSpell(PentagramSpell __instance, bool __result)
        {
			if (__result)
            {
				__instance.app.model.characterSheet.bumboRoomModifiers.damage--;
				//Item damage room modifier is not implemented in the base game and must be added in
				__instance.app.model.characterSheet.bumboRoomModifiers.itemDamage++;
				__instance.app.controller.UpdateStats();
				Console.WriteLine("[The Legend of Bum-bo: Windfall] Preventing Pentagram from granting puzzle damage");
			}
		}

		//Patch: Implements room spell damage
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterSheet), "getItemDamage")]
		static void CharacterSheet_getItemDamage(CharacterSheet __instance, ref int __result)
		{
			int damage = Mathf.RoundToInt((float)Mathf.Clamp(__result + __instance.app.model.characterSheet.bumboRoomModifiers.itemDamage, 1, 5));
			__result = damage;
		}
		//Patch: Implements room spell damage
		[HarmonyPrefix, HarmonyPatch(typeof(CharacterSheet), "getTemporaryItemDamage")]
		static bool CharacterSheet_getTemporaryItemDamage(CharacterSheet __instance, ref int __result)
		{
			int num = __instance.bumboBaseInfo.itemDamage + __instance.app.model.characterSheet.hiddenTrinket.AddToSpellDamage();
			short num2 = 0;
			while ((int)num2 < __instance.app.model.characterSheet.trinkets.Count)
			{
				num += __instance.app.controller.GetTrinket((int)num2).AddToSpellDamage();
				num2 += 1;
			}
			int num3 = Mathf.Max(0, 5 - num);
			int num4 = 0;
			num4 += __instance.app.model.characterSheet.bumboRoundModifiers.itemDamage;
			num4 += __instance.app.model.characterSheet.bumboRoundModifiers.damage;
			//Adding room item damage
			num4 += __instance.app.model.characterSheet.bumboRoomModifiers.itemDamage;
			num4 += __instance.app.model.characterSheet.bumboRoomModifiers.damage;
			__result = Mathf.Clamp(num4, 0, num3);
			return false;
		}

		//Patch: Increases mana gain from Converter
		[HarmonyPrefix, HarmonyPatch(typeof(ConverterSpell), "ConvertMana")]
		static bool ConverterSpell_ConvertMana(ConverterSpell __instance, Block.BlockType _type)
		{
			short[] array = new short[6];
			//Increase mana gain to 2
			array[(int)_type] += 2;
			__instance.app.controller.UpdateMana(array, true);
			__instance.app.controller.ShowManaGain();
			return false;
		}

		static int rockCounter = 0;
		//Patch: Rock Friends now drops a number of rocks equal to the player's spell damage stat
		[HarmonyPrefix, HarmonyPatch(typeof(RockFriendsSpell), "DropRock")]
		static bool RockFriendsSpell_DropRock(RockFriendsSpell __instance, ref int _rock_number)
		{
			rockCounter++;
			_rock_number = 1;
			if (rockCounter > __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier())
            {
				_rock_number = 4;
				rockCounter = 0;
			}
			return true;
		}
		//Patch: Changes Rock Friends description
		[HarmonyPostfix, HarmonyPatch(typeof(RockFriendsSpell), MethodType.Constructor)]
		static void RockFriendsSpell_Constructor(RockFriendsSpell __instance)
		{
			__instance.Name = "Hits Random Enemies = to Spell Damage";
		}

		//Patch: Changes Attack Fly spell damage to incorporate the player's spell damage stat
		[HarmonyPostfix, HarmonyPatch(typeof(AttackFlySpell), "Damage")]
		static void AttackFlySpell_Damage(AttackFlySpell __instance, ref int __result)
		{
			__result = __instance.baseDamage + __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier();
		}
		//Patch: Removes Bum-bo the Dead's special Attack Fly cost reroll
		[HarmonyPrefix, HarmonyPatch(typeof(BumboController), "SetSpellCostForTheDeadsAttackFly")]
		static bool BumboController_SetSpellCostForTheDeadsAttackFly(BumboController __instance, SpellElement _spell, bool[] _ignore_mana, ref SpellElement __result)
		{
			__result = __instance.SetSpellCost(_spell, _ignore_mana);
			return false;
		}

		//Patch: Prevents Mama Foot from killing the player (broken)
		[HarmonyPrefix, HarmonyPatch(typeof(MamaFootSpell), "Reward")]
		static bool MamaFootSpell_Reward(MamaFootSpell __instance)
		{
			float damage = 0.5f * __instance.app.model.characterSheet.bumboRoomModifiers.damageMultiplier;
			while (damage >= __instance.app.model.characterSheet.hitPoints + __instance.app.model.characterSheet.soulHearts)
            {
				damage -= 0.5f;
            }

			__instance.app.controller.TakeDamage(-damage / __instance.app.model.characterSheet.bumboRoomModifiers.damageMultiplier, null);

			__instance.app.Notify("reward.spell", null, new object[0]);
			return false;
		}

		//Patch: Changes Brimstone spell damage to incorporate the player's spell damage stat
		[HarmonyPostfix, HarmonyPatch(typeof(BrimstoneSpell), "Damage")]
		static void BrimstoneSpell_Damage(BrimstoneSpell __instance, ref int __result)
		{
			__result = __instance.baseDamage + __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier();
		}

		//Patch: Changes Lemon spell damage to incorporate the player's spell damage stat
		[HarmonyPostfix, HarmonyPatch(typeof(LemonSpell), "Damage")]
		static void LemonSpell_Damage(LemonSpell __instance, ref int __result)
		{
			__result = __instance.baseDamage + __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier();
		}

		//Patch: Changes Pliers spell damage to incorporate the player's spell damage stat
		[HarmonyPostfix, HarmonyPatch(typeof(PliersSpell), "Damage")]
		static void PliersSpell_Damage(PliersSpell __instance, ref int __result)
		{
			__result = __instance.baseDamage + __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier();
		}

		//Patch: Changes Mama Shoe spell damage to incorporate the player's spell damage stat
		[HarmonyPostfix, HarmonyPatch(typeof(MamaShoeSpell), "Damage")]
		static void MamaShoeSpell_Damage(MamaShoeSpell __instance, ref int __result)
		{
			__result = __instance.baseDamage + __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier();
		}

		//Patch: Changes Dog Tooth spell damage to incorporate the player's spell damage stat
		[HarmonyPostfix, HarmonyPatch(typeof(DogToothSpell), "Damage")]
		static void DogToothSpell_Damage(DogToothSpell __instance, ref int __result)
		{
			__result = __instance.baseDamage + __instance.app.model.characterSheet.getItemDamage() + __instance.SpellDamageModifier();
		}
		//Patch: Changes Dog Tooth description
		[HarmonyPostfix, HarmonyPatch(typeof(DogToothSpell), MethodType.Constructor)]
		static void DogToothSpell_Constructor(DogToothSpell __instance)
		{
			__instance.Name = "Attack that Heals You";
		}
	}

	static class SpellManaCosts
	{
		public static Dictionary<SpellName, int> manaCosts = new Dictionary<SpellName, int>()
		{
			{ SpellName.TwentyTwenty, 7 },
			{ SpellName.Pentagram, 8 },
			{ SpellName.AttackFly, 8 },
			{ SpellName.MamaFoot, 13 },
			{ SpellName.Lemon, 5 },
			{ SpellName.Pliers, 5 },
			{ SpellName.Juiced, 6 },
			{ SpellName.MagicMarker, 6 }
		};

		public static int MinimumManaCost(SpellName spell)
        {
			return minimumManaCosts.TryGetValue(spell, out int value) ? value : 2;
        }

		public static Dictionary<SpellName, int> minimumManaCosts = new Dictionary<SpellName, int>()
		{
			{ SpellName.MagicMarker, 5 }
		};
	}
}
