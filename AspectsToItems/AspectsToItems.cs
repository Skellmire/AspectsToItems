using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using EnigmaticThunder;
using BepInEx.Configuration;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace AspectsToItems
{
	[BepInDependency("com.EnigmaDev.EnigmaticThunder")]
	[BepInPlugin("com.Skell.AspectsToItems", "Aspects To Items", "1.0.0")]
	public class AspectsToItems : BaseUnityPlugin
	{
		public void Awake()
		{
			ConfigInit();

            Hook();
		}

        private void Hook()
        {
			On.RoR2.ContentManager.SetContentPacks += AddContent;
			On.RoR2.PickupDropletController.CreatePickupDroplet += YellowDrops;
			On.RoR2.CharacterBody.RecalculateStats += EliteBuffsOnYellow;
			IL.RoR2.CharacterModel.UpdateOverlays += SkinsOnYellow;
			On.RoR2.CharacterModel.EnableItemDisplay += CrownsOnYellow;
			On.RoR2.CharacterModel.DisableItemDisplay += CrownRemoval;

		}

		private static void AddContent(On.RoR2.ContentManager.orig_SetContentPacks orig, List<ContentPack> newContentPacks)
        {
			ContentPack contentPack = new ContentPack();
			contentPack.itemDefs = NewDefsList.ToArray();
			newContentPacks.Add(contentPack);
			orig(newContentPacks);
        }

		private static void CrownRemoval(On.RoR2.CharacterModel.orig_DisableItemDisplay orig, RoR2.CharacterModel self, ItemIndex itemIndex)
		{
			for (int i = 0; i < NewDefsList.Count; i++)
            {
				if (itemIndex == NewDefsList[i].itemIndex && self.inventoryEquipmentIndex != OldDefsList[i].equipmentIndex)
				{
					for (int j = self.parentedPrefabDisplays.Count - 1; j >= 0; j--)
					{
						if (self.parentedPrefabDisplays[j].equipmentIndex == OldDefsList[i].equipmentIndex)
						{
							self.parentedPrefabDisplays[j].Undo();
							self.parentedPrefabDisplays.RemoveAt(j);
						}
					}
					for (int k = self.limbMaskDisplays.Count - 1; k >= 0; k--)
					{
						if (self.limbMaskDisplays[k].equipmentIndex == OldDefsList[i].equipmentIndex)
						{
							self.limbMaskDisplays[k].Undo(self);
							self.limbMaskDisplays.RemoveAt(k);
						}
					}
				}
			}
			orig(self, itemIndex);
		}

		private static void CrownsOnYellow(On.RoR2.CharacterModel.orig_EnableItemDisplay orig, RoR2.CharacterModel self, ItemIndex itemIndex)
		{
			for (int i = 0; i < NewDefsList.Count; i++)
            {
				if (itemIndex == NewDefsList[i].itemIndex)
				{
					RoR2.DisplayRuleGroup equipmentDisplayRuleGroup = self.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(OldDefsList[i].equipmentIndex);
					self.InstantiateDisplayRuleGroup(equipmentDisplayRuleGroup, NewDefsList[i].itemIndex, EquipmentIndex.None);
				}
			}
			orig(self, itemIndex);
		}

		private static void SkinsOnYellow(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			c.GotoNext(
				//x => x.MatchStloc(4),
				//x => x.MatchLdloca(4),
				//x => x.MatchCallOrCallvirt<EliteIndex>("get_HasValue"),
				x => x.MatchBrtrue(out ILLabel IL_0089),
				x => x.MatchLdcI4(-1),
				x => x.MatchBr(out ILLabel IL_0090),
				x => x.MatchLdloca(4)
				//x => x.MatchCallOrCallvirt<EliteIndex>("GetValueOrDefault"),
				//x => x.MatchStfld<CharacterModel>("myEliteIndex")
				);
			c.Index += 6;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<RoR2.CharacterModel>>((self) =>
			{
				for (int i = 0; i < NewDefsList.Count; i++)
                {
					if (self.body && self.body.isElite && self.myEliteIndex == EliteIndex.None)
					{
						int itemCount = self.body.inventory.GetItemCount(NewDefsList[i].itemIndex);
						if (itemCount > 0)
						{
							self.myEliteIndex = OldDefsList[i].passiveBuffDef.eliteDef.eliteIndex;
						}
					}
                }
			});
			c.GotoNext(
				//x => x.MatchStloc(6),
				//x => x.MatchLdloca(6),
				//x => x.MatchCallOrCallvirt<int32>("get_HasValue"),
				x => x.MatchBrtrue(out ILLabel IL_00E8),
				x => x.MatchLdcI4(-1),
				x => x.MatchBr(out ILLabel IL_00EF),
				x => x.MatchLdloca(6)
				//x => x.MatchCallOrCallvirt<int32>("GetValueOrDefault"),
				//x => x.MatchStfld<CharacterModel>("shaderEliteRampIndex")
				);
			c.Index += 6;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<RoR2.CharacterModel>>((self) =>
			{
				for (int i = 0; i < NewDefsList.Count; i++)
				{
					if (self.body && self.body.isElite && self.shaderEliteRampIndex == -1)
					{
						int itemCount = self.body.inventory.GetItemCount(NewDefsList[i].itemIndex);
						if (itemCount > 0)
						{
							self.shaderEliteRampIndex = OldDefsList[i].passiveBuffDef.eliteDef.shaderEliteRampIndex;
						}
					}
				}
			});
		}

		private static void EliteBuffsOnYellow(On.RoR2.CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
		{
			for (int i = 0; i < NewDefsList.Count; i++)
			{
				if (self.inventory && self.inventory.GetItemCount(NewDefsList[i].itemIndex) > 0)
				{
					self.AddBuff(OldDefsList[i].passiveBuffDef);
				}
			}
			orig(self);
		}

		private static void YellowDrops(On.RoR2.PickupDropletController.orig_CreatePickupDroplet orig, RoR2.PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
		{
			for (int i = 0; i < NewDefsList.Count; i++)
            {
				if (pickupIndex == PickupCatalog.FindPickupIndex(OldDefsList[i].equipmentIndex))
				{
					if (NewDefsList[i] != null)
					{
						pickupIndex = PickupCatalog.FindPickupIndex(NewDefsList[i].itemIndex);
					}
				}
			}
			orig(pickupIndex, position, velocity);
		}

		public static void YellowAspectDef(EquipmentDef OriginalAspect, string NameToken, string PickupToken, string DescriptionToken, string LoreToken)
		{
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_NAME", NameToken);
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_PICKUP", PickupToken);
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_DESC", DescriptionToken);
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_LORE", LoreToken);

			if (OriginalAspect != null)
			{
				ItemDef itemDef = ScriptableObject.CreateInstance<ItemDef>();
				itemDef.name = OriginalAspect.name;
				itemDef.tier = AspectsTier.Value;
				itemDef.pickupModelPrefab = OriginalAspect.pickupModelPrefab;
				itemDef.pickupIconSprite = OriginalAspect.pickupIconSprite;
				itemDef.nameToken = OriginalAspect.ToString().ToUpper() + "_NAME";
				itemDef.pickupToken = OriginalAspect.ToString().ToUpper() + "_PICKUP";
				itemDef.descriptionToken = OriginalAspect.ToString().ToUpper() + "_DESC";
				itemDef.loreToken = OriginalAspect.ToString().ToUpper() + "_LORE";
				itemDef.tags = new ItemTag[]
				{
				ItemTag.Utility,
				WorldUniqueConfig.Value ? ItemTag.Any : ItemTag.WorldUnique
				};
				NewDefsList.Add(itemDef);
				OldDefsList.Add(OriginalAspect);
			}
		}

		public static void YellowAspectDef(EquipmentDef OriginalAspect, string[] NewLanguageTokens)
		{
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_NAME", NewLanguageTokens[0]);
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_PICKUP", NewLanguageTokens[1]);
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_DESC", NewLanguageTokens[2]);
			EnigmaticThunder.Modules.Languages.Add(OriginalAspect.ToString().ToUpper() + "_LORE", NewLanguageTokens[3]);

			if (OriginalAspect != null)
			{
				ItemDef itemDef = ScriptableObject.CreateInstance<ItemDef>();
				itemDef.name = OriginalAspect.name;
				itemDef.tier = AspectsTier.Value;
				itemDef.pickupModelPrefab = OriginalAspect.pickupModelPrefab;
				itemDef.pickupIconSprite = OriginalAspect.pickupIconSprite;
				itemDef.nameToken = OriginalAspect.ToString().ToUpper() + "_NAME";
				itemDef.pickupToken = OriginalAspect.ToString().ToUpper() + "_PICKUP";
				itemDef.descriptionToken = OriginalAspect.ToString().ToUpper() + "_DESC";
				itemDef.loreToken = OriginalAspect.ToString().ToUpper() + "_LORE";
				itemDef.tags = new ItemTag[]
				{
				ItemTag.Utility,
				WorldUniqueConfig.Value ? ItemTag.Any : ItemTag.WorldUnique
				};
				NewDefsList.Add(itemDef);
				OldDefsList.Add(OriginalAspect);
			}
		}

		private void ConfigInit()
		{
			AspectsTier = Config.Bind<ItemTier>(
			"AspectsToItems",
			"Elite Aspects Item Tier",
			ItemTier.Boss,
			"The item tier of the elite aspects."
			);
			WorldUniqueConfig = Config.Bind<bool>(
			"AspectsToItems",
			"Should these items drop from chests?",
			false,
			"If left false, the aspects will only drop from elites."
			);
		}


		//Initialization
		public static List<EquipmentDef> OldDefsList = new List<EquipmentDef>();
		public static List<ItemDef> NewDefsList = new List<ItemDef>();
		public static ConfigEntry<ItemTier> AspectsTier { get; set; }
		public static ConfigEntry<bool> WorldUniqueConfig { get; set; }
	}
}
