// =======================================================================================
// ADVANCED HARVESTING - PLAYER
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

#if _AdvancedHarvesting

// =======================================================================================
// PLAYER
// =======================================================================================
public partial class Player {

    [HideInInspector] public SyncListAdvancedHarvestingProfession UMO3d_Professions;
	[HideInInspector] protected float UMO3d_HarvestTimer;
	
    protected const string UMO3d_MSG_ADVANCED_HARVEST_ADD 	= "You learned a new profession: ";	
	protected const string UMO3d_MSG_ADVANCED_HARVEST_GAIN 	= "You gained profession experience as: ";	
    protected const string UMO3d_MSG_ADVANCED_HARVEST_LVL 	= "You just gained a profession level as: ";	
    protected const string UMO3d_MSG_ADVANCED_HARVEST_GET 	= "You harvested: ";	
    protected const string UMO3d_MSG_ADVANCED_HARVEST_REQ 	= "Harvest Requirements not met!";	
    protected const string UMO3d_MSG_ADVANCED_HARVEST_FAIL	= "Failed!";
    protected const int UMO3d_ADVANCED_HARVEST_FACTOR		= 10;
    
	// -----------------------------------------------------------------------------------
	// OnSelect_UMO3d_AdvancedHarvesting (Client)
	// -----------------------------------------------------------------------------------
	private void OnSelect_UMO3d_AdvancedHarvesting(Entity entity) {
		if (UMO3d_AdvancedHarvesting_ValidateResourceNode()) {

           	// -- not ready? start harvesting
           	if (((SimpleResourceNode)entity).canHarvest()) {
				
				Cmd_UMO3d_StartHarvest(); // Server
           		StartHarvestingVisuals(); // Client

           	} else {
           	// --- ready? show loot window
           		if (((SimpleResourceNode)entity).HasResources()) {
           			FindObjectOfType<UIAdvancedHarvestingLoot>().Show(); // Client
           		}
           	}
           	
		} else {
			// -- in any other case, simply walk there
			CmdNavigateTo(entity.collider.ClosestPointOnBounds(transform.position), interactionRange);
			
#if _FreeInfoBox
			if (entity is SimpleResourceNode) {
				FindObjectOfType<InfoBox>().AddMessage(UMO3d_MSG_ADVANCED_HARVEST_REQ);
			}
#endif

		}
	}
	
	// -----------------------------------------------------------------------------------
	// LateUpdate_UMO3d_AdvancedHarvesting (Client)
	// -----------------------------------------------------------------------------------
	[ClientCallback]
    public void LateUpdate_UMO3d_AdvancedHarvesting() {
    	if (UMO3d_AdvancedHarvesting_ValidateResourceNode()) {
        	if (UMO3d_HarvestTimer != 0 && NetworkTime.time > UMO3d_HarvestTimer) {
           		StopHarvestingVisuals(); // Client
           		Cmd_UMO3d_FinishHarvest(); // Server
           		UMO3d_HarvestTimer = 0; // Client
           	}
        }
    }
    
	// -----------------------------------------------------------------------------------
	// Cmd_UMO3d_StartHarvest (Server)
	// -----------------------------------------------------------------------------------
	[Command(channel = Channels.DefaultUnreliable)] // unimportant => unreliable
    public void Cmd_UMO3d_StartHarvest() {
    	UMO3d_HarvestTimer = NetworkTime.time + ((SimpleResourceNode)target).harvestDuration;
    	UMO3d_AdvancedHarvesting_depleteInventory( ((SimpleResourceNode)target).requiredProfession.depletableInventoryItem, ((SimpleResourceNode)target).requiredProfession.depleteAmount);
	}
        
	// -----------------------------------------------------------------------------------
	// Cmd_UMO3d_FinishHarvest (Server)
	// -----------------------------------------------------------------------------------
	[Command(channel = Channels.DefaultUnreliable)] // unimportant => unreliable
    public void Cmd_UMO3d_FinishHarvest() {
    	if (UMO3d_AdvancedHarvesting_ValidateResourceNode()) {

			// --------------------------------------------------------------------------- Experience
			var prof = getProfession(((SimpleResourceNode)target).requiredProfession);
			int oldLevel = prof.level;
			prof.experience += ((SimpleResourceNode)target).ProfessionExperienceReward;
			SetProfession(prof);

#if _FreeInfoBox
			if (oldLevel < prof.level) {
				FindObjectOfType<InfoBox>().TargetAddMessage(this.connectionToClient, UMO3d_MSG_ADVANCED_HARVEST_LVL + ((SimpleResourceNode)target).requiredProfession.name + " [L" + prof.level + "]");
			}
#endif     
			
			// --------------------------------------------------------------------------- Success Chance
			var success = false;
			
			var baseChance = ((SimpleResourceNode)target).requiredProfession.baseHarvestChance;
			
			// -- check for success
			if (baseChance < 1) {
				
				var nodeLevel 	= ((SimpleResourceNode)target).minProfessionLevel;
				var tmpProf 	= getProfession(((SimpleResourceNode)target).requiredProfession);

				if (tmpProf.level > nodeLevel) {
					baseChance += ((tmpProf.level - nodeLevel) * (baseChance / UMO3d_ADVANCED_HARVEST_FACTOR));
				}

				if (UnityEngine.Random.value <= baseChance) {
					success = true;
				}

			} else {
				success = true;
			}
			
			// --------------------------------------------------------------------------- Resources
			if (success) {
				((SimpleResourceNode)target).RefillResources();
			} else {
				Rpc_UMO3d_AdvancedHarvesting_ShowPopup(target.gameObject, UMO3d_MSG_ADVANCED_HARVEST_FAIL);
			}
			
			UMO3d_HarvestTimer = 0;

        }
    }
  
  	// -----------------------------------------------------------------------------------
	// UMO3d_AdvancedHarvesting_ValidateResourceNode (Client/Server)
	// -----------------------------------------------------------------------------------
	public bool UMO3d_AdvancedHarvesting_ValidateResourceNode() {
		var valid		= false;
		if (target != null && target is SimpleResourceNode) {
			valid = true;
			valid = (health > 0) ? true : false;
			valid = (currentSkill == -1) ? true : false;
			valid = Utils.ClosestDistance(target.collider, collider) <= interactionRange + ((SimpleResourceNode)target).increaseInteractionRange ? true : false;
			valid = HasProfession(((SimpleResourceNode)target).requiredProfession) ? true : false;
			valid = HasProfessionLevel(((SimpleResourceNode)target).requiredProfession, ((SimpleResourceNode)target).minProfessionLevel) ? true : false;
			valid = UMO3d_AdvancedHarvesting_checkEquipment(((SimpleResourceNode)target).requiredProfession.requiredEquipTool, ((SimpleResourceNode)target).requiredProfession.equipmentCategory) ? true : false;
			valid = UMO3d_AdvancedHarvesting_checkInventory(((SimpleResourceNode)target).requiredProfession.requiredInventoryTool) ? true : false;
			valid = UMO3d_AdvancedHarvesting_checkInventoryAmount(((SimpleResourceNode)target).requiredProfession.depletableInventoryItem, ((SimpleResourceNode)target).requiredProfession.depleteAmount) ? true : false;
		}
		return valid;
	}
	
    // -----------------------------------------------------------------------------------
	// StartHarvestingVisuals (Client)
	// -----------------------------------------------------------------------------------
	[Client]
    private void StartHarvestingVisuals() {
        if (isClient) {
        	if (UMO3d_AdvancedHarvesting_ValidateResourceNode()) {
        		
        		UMO3d_HarvestTimer = NetworkTime.time + ((SimpleResourceNode)target).harvestDuration;
				agent.ResetPath();
				LookAtY(target.transform.position);
				FindObjectOfType<UIAdvancedHarvestingBar>().Show(((SimpleResourceNode)target).requiredProfession.name, ((SimpleResourceNode)target).harvestDuration);
						
				foreach (var anim in GetComponentsInChildren<Animator>()) {
					if (anim.parameters.Any(p => p.name == ( ((SimpleResourceNode)target).requiredProfession.animatorState)) ) {
						anim.SetBool(((SimpleResourceNode)target).requiredProfession.animatorState, true);
					}
				}
            	
            }
        }
    }

	// -----------------------------------------------------------------------------------
	// StopHarvestingVisuals (Client)
	// -----------------------------------------------------------------------------------
	[Client]
    private void StopHarvestingVisuals() {
        if (isClient) {
            if (target != null && target is SimpleResourceNode) {
            
				foreach (var anim in GetComponentsInChildren<Animator>()) {
					if (anim.parameters.Any(p => p.name == ( ((SimpleResourceNode)target).requiredProfession.animatorState)) ) {
						 anim.SetBool(((SimpleResourceNode)target).requiredProfession.animatorState, false);
					}
					Destroy(indicator);
				}
			
				FindObjectOfType<UIAdvancedHarvestingBar>().Hide();
            
            }
        }
    }

	// -----------------------------------------------------------------------------------
	// Cmd_UMO3d_AdvancedHarvesting_TakeResources (Server)
	// -----------------------------------------------------------------------------------
    [Command(channel=Channels.DefaultUnreliable)] // unimportant => unreliable
    public void Cmd_UMO3d_AdvancedHarvesting_TakeResources(int index) {
        // validate: dead monster and close enough and valid loot index?
        // use collider point(s) to also work with big entities
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            UMO3d_AdvancedHarvesting_ValidateResourceNode() &&
            ((SimpleResourceNode)target).HasResources() &&
            0 <= index && index < target.inventory.Count &&
            target.inventory[index].valid)
        {
            var item = target.inventory[index];

            // try to add it to the inventory, clear  slot if it worked
            if (InventoryAddAmount(item.template, item.amount)) {
                item.valid = false;
                target.inventory[index] = item;
            }
            
            // check if resource is depleted (otherwise it takes too long to update)
            if (((SimpleResourceNode)target).EventDepleted()) {
            	((SimpleResourceNode)target).OnDepleted();
            }
            
        }
    }

	// -----------------------------------------------------------------------------------
	// OnUseInventoryItem_UMO3d_AdvancedHarvesting (Server)
	// -----------------------------------------------------------------------------------
	public void OnUseInventoryItem_UMO3d_AdvancedHarvesting(int index) {
		var item = inventory[index];
		if (item.valid && item.learnProfession != null) {
			
			var pass = false;
			
			// -- Player does not have Harvesting already
			if (!HasProfession(item.learnProfession)) {
				var tmpProf = new AdvancedHarvestingProfession(item.learnProfession.name);
				tmpProf.experience = item.gainProfessionExp;
            	UMO3d_Professions.Add(tmpProf);
            	pass = true;
            	
#if _FreeInfoBox
				FindObjectOfType<InfoBox>().TargetAddMessage(this.connectionToClient, UMO3d_MSG_ADVANCED_HARVEST_ADD + item.learnProfession.name);
#endif
			
			// -- Player has Harvesting but can gain experience
			} else if (HasProfession(item.learnProfession) && item.gainProfessionExp > 0) {
			
				var tmpProf = getProfession(item.learnProfession);
				if (tmpProf.level < item.learnProfession.levels.Length) {
				
					tmpProf.experience += item.gainProfessionExp;
					SetProfession(tmpProf);
					pass = true;
					
#if _FreeInfoBox
					FindObjectOfType<InfoBox>().TargetAddMessage(this.connectionToClient, UMO3d_MSG_ADVANCED_HARVEST_GAIN + item.learnProfession.name);
#endif
					
				}
			}

            // -- decrease amount or destroy
            if (pass && item.usageDestroy) {
            	--item.amount;
            	if (item.amount == 0) item.valid = false;
            	inventory[index] = item; // put new values in there	
            }
			
		}
	}
	
	// -----------------------------------------------------------------------------------
	// UMO3d_AdvancedHarvesting_checkEquipment
	// -----------------------------------------------------------------------------------
	public bool UMO3d_AdvancedHarvesting_checkEquipment(ItemTemplate item, string category = "") {
		if (item == null) return true;
		for (int i = 0; i < equipment.Count; ++i) {
       		if (equipment[i].valid
       			&& ( equipment[i].category.StartsWith(category) || category == "")
       			&& equipment[i].template == item) {
             	return true;
    		}
    	}
		return false;
	}
	
	// -----------------------------------------------------------------------------------
	// UMO3d_AdvancedHarvesting_checkInventory
	// -----------------------------------------------------------------------------------
	public bool UMO3d_AdvancedHarvesting_checkInventory(ItemTemplate item) {
		if (item == null) return true;
		return GetInventoryIndexByName(item.name) != -1;
	}
	
	// -----------------------------------------------------------------------------------
	// UMO3d_AdvancedHarvesting_checkInventoryAmount
	// -----------------------------------------------------------------------------------
	public bool UMO3d_AdvancedHarvesting_checkInventoryAmount(ItemTemplate item, int amount) {
		if (item == null || amount <= 0) return true;
		if (InventoryCountAmount(item.name) >= amount) return true;
		return false;
	}
	
	// -----------------------------------------------------------------------------------
	// UMO3d_AdvancedHarvesting_depleteInventory
	// -----------------------------------------------------------------------------------
	public bool UMO3d_AdvancedHarvesting_depleteInventory(ItemTemplate item, int amount) {
		if (item == null || amount <= 0) return true;
		if (UMO3d_AdvancedHarvesting_checkInventoryAmount(item, amount)) {
			return InventoryRemoveAmount(item.name, amount);
		}
		return false;
	}
	
	// -----------------------------------------------------------------------------------
	// AdvancedHarvestingProfession
	// -----------------------------------------------------------------------------------
    public AdvancedHarvestingProfession getProfession(AdvancedHarvestingProfessionTemplate aProf) {
        return UMO3d_Professions.First(pr => pr.templateName == aProf.name);
    }

	// -----------------------------------------------------------------------------------
	// getHarvestingExp
	// -----------------------------------------------------------------------------------
	public int getProfessionExp(AdvancedHarvestingProfession aProf) {
		int id = UMO3d_Professions.FindIndex(prof => prof.templateName == aProf.templateName);
        return UMO3d_Professions[id].experience;
	}

	// -----------------------------------------------------------------------------------
	// SetProfession
	// -----------------------------------------------------------------------------------
    public void SetProfession(AdvancedHarvestingProfession aProf) {
        int id = UMO3d_Professions.FindIndex(pr => pr.templateName == aProf.template.name);
        UMO3d_Professions[id] = aProf;
    }
	// -----------------------------------------------------------------------------------
	// HasProfession
	// -----------------------------------------------------------------------------------
    public bool HasProfession(AdvancedHarvestingProfessionTemplate aProf) {
    	return UMO3d_Professions.Any(prof => prof.templateName == aProf.name);
    }
	// -----------------------------------------------------------------------------------
	// HasProfessionLevel
	// -----------------------------------------------------------------------------------
    public bool HasProfessionLevel(AdvancedHarvestingProfessionTemplate aProf, int level) {
    	if (HasProfession(aProf)) {
    		var tmpProf = getProfession(aProf);
    		if (tmpProf.level >= level) return true;
    	
    	}
    	return false;
    }

    // -----------------------------------------------------------------------------------
    
}

#endif

// =======================================================================================