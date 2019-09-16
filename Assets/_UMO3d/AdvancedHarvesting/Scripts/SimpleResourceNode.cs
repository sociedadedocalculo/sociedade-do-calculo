// =======================================================================================
// ADVANCED HARVESTING - SIMPLE RESOURCE NODE CLASS
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

#if _AdvancedHarvesting

namespace UMO3d {
	
	// =======================================================================================
	// SIMPLE RESOURCE NODE
	// =======================================================================================
	public partial class SimpleResourceNode : Entity {
		
		protected const string UMO3d_MSG_ADVANCED_HARVEST_SUCCESS	= "Success!";
		protected const string UMO3d_MSG_ADVANCED_HARVEST_CRIT		= "Crit!";
		
		[Header("UMO3d - Advanced Harvesting")]
		public float harvestDuration = 2;
		public AdvancedHarvestingProfessionTemplate requiredProfession;
		public int minProfessionLevel = 1;
		public int ProfessionExperienceReward = 2;
		public int increaseInteractionRange = 0;
		[Range(0,1)] public float baseCriticalChance = 0.1f;
		public bool shakeOnHarvest;
		[Tooltip("Object automatically unspawns if the amount of resources has been collected from it (set to 0 to disable)")]
		public int totalResources;
		
		[Header("Resource Drops")]
		public AdvancedHarvestingHarvestItems[] harvestItems;
		
    	[Header("Respawn")]
    	[Tooltip("How long the node stays empty in the scene when it has been completely harvested (in Seconds)")]
    	public float emptyTime = 5f;
    	
    	[Tooltip("Will the node respawn once it got harvested? Otherwise its available only once.")]
    	public bool respawn = true;
    	[Tooltip("How long it takes for a empty object to respawn (in Seconds) with full resources again. Set higher than 0 !")]
    	public float respawnTime = 10f;
    	
    	[Header("Text Meshes")]
    	public TextMesh resourceOverlay;

    	[HideInInspector, SyncVar] private int currentResources;
    	[HideInInspector] float emptyTimeEnd;
	    [HideInInspector] float respawnTimeEnd;
	    [HideInInspector] private float currentIntensity = 0f;
	    [HideInInspector] private float shakeDecay = .01f;
		[HideInInspector] private Vector3 originPosition;
		[HideInInspector] private Quaternion originRotation;
		
		
		
		// -----------------------------------------------------------------------------------
		// NetworkBehaviour
		// -----------------------------------------------------------------------------------
		protected override void Awake() {
			base.Awake();
		}

		public override void OnStartServer() {
			base.OnStartServer();
			currentResources = totalResources;
			this.name = this.name + " [L"+minProfessionLevel+"]";
		}

		[Client]
		void LateUpdate() {
			ContinueShake();
		}

		// -----------------------------------------------------------------------------------
		// FiniteStateMachineEvents
		// -----------------------------------------------------------------------------------
		public bool EventDepleted() {
			return (totalResources > 0 && currentResources <= 0 && !HasResources());
		}
		
 		bool EventDepletedTimeElapsed() {
         return state == "DEPLETED" && Time.time >= emptyTimeEnd;
     	}

    	bool EventRespawnTimeElapsed() {
        return state == "DEPLETED" && respawn && Time.time >= respawnTimeEnd;
    	}
    	
		// -----------------------------------------------------------------------------------
		// FiniteStateMachine Server
		// -----------------------------------------------------------------------------------
		[Server]
		string UpdateServer_IDLE() {
			// events sorted by priority (e.g. target doesn't matter if we died)
			if (EventDepleted()) {
				OnDepleted();
				return "DEPLETED";
			}
			return "IDLE"; // nothing interesting happened
		}

		[Server]
		string UpdateServer_DEPLETED() {
			// events sorted by priority (e.g. target doesn't matter if we died)
			if (EventRespawnTimeElapsed()) {
				Show();
				currentResources = totalResources;
				return "IDLE";
			}
			if (EventDepletedTimeElapsed()) {
				// we were lying around empty for long enough now.
				// hide while respawning, or disappear forever
				if (respawn) Hide();
				else NetworkServer.Destroy(gameObject);
				return "DEPLETED";
			}
			
			if (EventDepleted()) {} // don't care, of course we are empty

			return "DEPLETED"; // nothing interesting happened
		}

		[Server]
		protected override string UpdateServer() {
			if (state == "IDLE")    	return UpdateServer_IDLE();
			if (state == "DEPLETED")    return UpdateServer_DEPLETED();
			return "IDLE";
		}
		
		// -----------------------------------------------------------------------------------
		// 
		// -----------------------------------------------------------------------------------
    	[Client]
    	protected override void UpdateClient() {
    		if (totalResources > 0) {
				var resPercent =  (currentResources > 0 && totalResources > 0) ? ((float)currentResources / (float)totalResources)*100 : 0;
				resourceOverlay.text = resPercent.ToString() + "%";
			}
    	}

		// -----------------------------------------------------------------------------------
		// 
		// -----------------------------------------------------------------------------------
		[Server]
		public void OnDepleted() {
			// set death and respawn end times. we set both of them now to make sure
			// that everything works fine even if a monster isn't updated for a
			// while. so as soon as it's updated again, the death/respawn will
			// happen immediately if current time > end time.
			inventory.Clear();
			
			emptyTimeEnd = Time.time + emptyTime;
			respawnTimeEnd = emptyTimeEnd + respawnTime; // after empty time ended
		}
		
		// -----------------------------------------------------------------------------------
		// canHarvest
		// -----------------------------------------------------------------------------------
		public bool canHarvest() {
			return ((totalResources <= 0 || currentResources > 0) && !HasResources()); 
		}
		
		// -----------------------------------------------------------------------------------
		// RefillResources
		// -----------------------------------------------------------------------------------
		[Server]
		public void RefillResources() {
			if (!HasResources()) {
				var loop = true;
				var criticalSuccess = false;
				var shakeIntensity = 0f;
				
				// ----------------------------------------------------------------------- Critical Success?
				criticalSuccess = (UnityEngine.Random.value < baseCriticalChance) ? true : false;
				
				if (criticalSuccess) {
					Rpc_UMO3d_AdvancedHarvesting_ShowPopup(this.gameObject, UMO3d_MSG_ADVANCED_HARVEST_CRIT);
				} else {
					Rpc_UMO3d_AdvancedHarvesting_ShowPopup(this.gameObject, UMO3d_MSG_ADVANCED_HARVEST_SUCCESS);
				}
				
				// ----------------------------------------------------------------------- Generate Resources
				while (loop) {
					var resCount = 0;
					foreach (AdvancedHarvestingHarvestItems harvestItem in harvestItems) {
						if (UnityEngine.Random.value <= harvestItem.probability) {
							var amount = (UnityEngine.Random.Range(harvestItem.minAmount, harvestItem.maxAmount));
							
							for (int i = 1; i <= amount; i++) {
								inventory.Add(new Item(harvestItem.template));
							}
							resCount += amount;
						}
					}
					
					currentResources -= resCount;
					
					if (criticalSuccess) {
						criticalSuccess = false;
						shakeIntensity = .3f;
						
					} else {
						loop = false;
						shakeIntensity = .1f;
					}
				}
				
				// ----------------------------------------------------------------------- Shake Resource Node
				if (shakeOnHarvest) {
					StartShake(shakeIntensity);
				}
				
			}
		}		
		
		// -----------------------------------------------------------------------------------
		// HasResources
		// -----------------------------------------------------------------------------------
    	public bool HasResources() {
        	return inventory.Any(item => item.valid);
    	}

		// -----------------------------------------------------------------------------------
		// StartShake
		// -----------------------------------------------------------------------------------
		public void StartShake(float shakeIntensity = .3f) {
			if (shakeIntensity > 0) {
				originPosition = transform.position;
				originRotation = transform.rotation;
				currentIntensity = shakeIntensity;
			}
		}
		
		// -----------------------------------------------------------------------------------
		// ContinueShake
		// -----------------------------------------------------------------------------------
		public void ContinueShake() {
			if (currentIntensity > 0){
				transform.position = originPosition + Random.insideUnitSphere * currentIntensity;
				transform.rotation = new Quaternion(
					originRotation.x + Random.Range (-currentIntensity,currentIntensity) * .2f,
					originRotation.y + Random.Range (-currentIntensity,currentIntensity) * .2f,
					originRotation.z + Random.Range (-currentIntensity,currentIntensity) * .2f,
					originRotation.w + Random.Range (-currentIntensity,currentIntensity) * .2f);
				currentIntensity -= shakeDecay;
			}
		}

		// -----------------------------------------------------------------------------------
		// Entity Overrrides
		// -----------------------------------------------------------------------------------
		public override bool HasCastWeapon() { return true; }
		public override bool CanAttack(Entity type) { return false; }
		public override int healthMax { get { return 1; } }
    	public override int manaMax { get { return 0; } }
    	public override int damage { get { return 0; } }
    	public override int defense { get { return 0; } }
    	public override float blockChance { get { return 0; } }
    	public override float criticalChance { get { return 0; } }
		// -----------------------------------------------------------------------------------
	}

}

#endif

// =======================================================================================