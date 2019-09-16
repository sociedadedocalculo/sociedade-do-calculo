======================================================================
UNITY-MMO - UMMORPG & UNITY ASSETS
======================================================================
* Copyright by Unity-MMO - no permission to share, re-sell or give away
* Authors: Fhiz & Stue
* This asset is an Add-On for uMMORPG3d, it is useless without.
* Tested under MacOS, Windows and Linux Dedicated Server.
* Get uMMORPG here: https://www.assetstore.unity3d.com/en/#!/content/51212

* Our AddOn/Asset Store: http://www.unity-mmo.com
* NuCore Download (required for most AddOns): http://www.unity-mmo.com/butler/index.php
* Support via eMail: support@unity-mmo.com
* Support via Discord: https://discord.gg/NGNqpnw
======================================================================

INSTALLATION
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
Import this AddOn. It does not require NuCore or any other AddOn.

Find the following prefabs in this AddOns Prefabs folder and put it into your scenes
Canvas:

AdvancedHarvestingHarvestBarUI
AdvancedHarvestingLootUI
AdvancedHarvestingUI

Now go to your players Animator and copy the basic attack animation state and rename it.

Add another Parameter to your list and call it HARVESTING, make a transition from any
state to your freshly created animation state.

Finally choose the SimpleResourceNode prefab and put it somewhere in your scene.

Add a miners handbook to a NPC shop or loot drop. Acquire that item in order to test.

Move the items that come with this AddOn to your projects resources folder. Do not
keep duplicates around or it will cause errors.

Go near a Resource Node and click it to start mining. After its finished, when you
are succesful, click it again to collect the harvested resources.

DESCRIPTION
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

Add a Profession and Harvesting system to your game with this AddOn!

You can now place ResourceNodes in your scene that your players can harvest in order to
gain valueable items.  You can create all kinds of different professions, that allow to
harvest various items from resource nodes on the map. Each player can have multiple
professions, that can be learned from items. Each profession has a certain level that
allow the player to collect items from various Resource Nodes.

* Unlimited amount of professions per player
* Professions learnable and upgradeable by item
* Professions upgradeable via learning-by-doing as well
* Custom Animation for Professions (you have to add the animations yourself)
* Base Harvest Chance that increases with profession level
* Optional required equipped item
* Optional required item in players inventory
* Optional depletable item in players inventory + amount
* Harvest Failure, Success and Critical Success
* Resource Nodes have a doubled chance of generating resources on Critical Success
* Limited or unlimited amount of resources on a resource node
* Resource Nodes hide and respawn after their resource amount is depleted
* Optional Shake Animation on Resource Node / Heavy Shake on Critical Success
* Resource Node Name Overlay contains resource node level and remaining resources in %
* Gain profession experience for each harvest attempt (succesful or not)
* Set minimum profession level required for harvesting a resource node
* Optional increase interaction range for resource nodes placed in the water etc.
* Set a base critical chance for critical success on a resource node (or set to 0)
* Set any amount of harvestable items, min amount, max amount and drop chances

======================================================================