using OaStudios.SoundManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public partial class Player
{
    private string lastState;
    private AudioSource audioSource;
    private BaseSoundEffect soundEffects;

    private SoundEffectModel levelUpSound;
    private SoundEffectModel[] getHitSounds, hitSounds, deathSounds, leftFootSounds, rightFootSounds;

    public bool enableSoundEffects = true;

    void Start_SoundManager()
    {
        audioSource = GetComponent<AudioSource>();
        soundEffects = GetComponent<BaseSoundEffect>();

        if (soundEffects == null)
        {
            enableSoundEffects = false;
            return;
        }

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        levelUpSound = soundEffects.Effects.Where(i => i.soundType == SoundType.LevelUp).FirstOrDefault();
        hitSounds = soundEffects.Effects.Where(i => i.soundType == SoundType.Attack).ToArray();
        deathSounds = soundEffects.Effects.Where(i => i.soundType == SoundType.Death).ToArray();
        getHitSounds = soundEffects.Effects.Where(i => i.soundType == SoundType.GetHit).ToArray();
        leftFootSounds = soundEffects.Effects.Where(i => i.soundType == SoundType.LeftFoot).ToArray();
        rightFootSounds = soundEffects.Effects.Where(i => i.soundType == SoundType.RightFoot).ToArray();

        var animationSoundEffects = soundEffects.Effects.Where(i => i.soundType == SoundType.GenericAnimation);
        foreach (SoundEffectModel sfm in animationSoundEffects)
        {
            if (sfm.soundClip != null && sfm.animationClip != null)
            {
                AnimationEvent clipEvent = new AnimationEvent();
                clipEvent.time = sfm.animSecond;
                clipEvent.stringParameter = sfm.friendlyName;
                clipEvent.functionName = "PlayGenericAnimationSound";
                sfm.animationClip.AddEvent(clipEvent);
            }
        }

        if (animator != null)
        {
            foreach (SoundEffectModel leftFootSound in leftFootSounds)
            {
                if (leftFootSound.soundClip != null && leftFootSound.animationClip != null)
                {
                    AnimationEvent clipEvent = new AnimationEvent();
                    clipEvent.time = leftFootSound.animSecond;
                    clipEvent.functionName = "LeftFootStep";
                    leftFootSound.animationClip.AddEvent(clipEvent);
                }
            }

            foreach (SoundEffectModel rightFootSound in rightFootSounds)
            {
                if (rightFootSound.soundClip != null && rightFootSound.animationClip != null)
                {
                    AnimationEvent clipEvent = new AnimationEvent();
                    clipEvent.time = rightFootSound.animSecond;
                    clipEvent.functionName = "RightFootStep";
                    rightFootSound.animationClip.AddEvent(clipEvent);
                }
            }
        }
    }

    void PlayGenericAnimationSound(string friendlyName)
    {
        if (!enableSoundEffects || Utils.ClientLocalPlayer() == null)
            return;

        var sfx = soundEffects.Effects.Where(i => i.soundType == SoundType.GenericAnimation
                                            && i.friendlyName.Equals(friendlyName)).FirstOrDefault();

        if (sfx.soundClip != null)
            SoundManager.Instance.PlayOnce(sfx.soundClip, audioSource, sfx.volume / 100f);
    }

    // Test done v1.121
    void LeftFootStep()
    {
        if (!enableSoundEffects || leftFootSounds.Length <= 0 || Utils.ClientLocalPlayer() == null)
            return;

        // Get material under player
        // See if there is an attached sound to that material and that foot
        // Play it,
        // or else, play at 0 index always.

        Texture t = GetMaterialUnderFoot();

        SoundEffectModel sfx = leftFootSounds.Where(i => i.material != null && i.material == t).FirstOrDefault();
        if (sfx.soundClip != null)
            SoundManager.Instance.PlayOnce(sfx.soundClip, audioSource, sfx.volume / 100f);
        else
            SoundManager.Instance.PlayOnce(leftFootSounds[0].soundClip, audioSource, leftFootSounds[0].volume / 100f);
    }

    // Test done v1.121
    void RightFootStep()
    {
        if (!enableSoundEffects || rightFootSounds.Length <= 0 || Utils.ClientLocalPlayer() == null)
            return;

        // Get material under player
        // See if there is an attached sound to that material and that foot
        // Play it,
        // or else, play at 0 index always.

        Texture t = GetMaterialUnderFoot();

        SoundEffectModel sfx = rightFootSounds.Where(i => i.material != null && i.material == t).FirstOrDefault();
        if (sfx.soundClip != null)
            SoundManager.Instance.PlayOnce(sfx.soundClip, audioSource, sfx.volume / 100f);
        else
            SoundManager.Instance.PlayOnce(rightFootSounds[0].soundClip, audioSource, rightFootSounds[0].volume / 100f);
    }

    // Test done v1.121
    void OnLevelUp_SoundManager()
    {
        if (!enableSoundEffects || levelUpSound.soundClip == null || Utils.ClientLocalPlayer() == null)
            return;

        SoundManager.Instance.PlayOnce(levelUpSound.soundClip, audioSource, levelUpSound.volume / 100f);
    }

    [Client]
    void UpdateClient_SoundManager()
    {
        if (!enableSoundEffects)
            return;

        if (lastState != state)
        {
            if (state == "CASTING")
            {
                // Test done v1.121

                // Is there a sound attached to current skill?
                string skillName = skills[currentSkill].name;
                SoundEffectModel skillSound = soundEffects.Effects.Where(i => i.skill != null && i.skill.name.Equals(skillName)).FirstOrDefault();

                if (skillSound.soundClip != null)
                    SoundManager.Instance.PlayOnce(skillSound.soundClip, audioSource, skillSound.volume / 100f);
                else if ( hitSounds.Length > 0 )
                {
                    SoundEffectModel randomHitSound = GetRandom(hitSounds);
                    if (randomHitSound.soundClip != null)
                        SoundManager.Instance.PlayOnce(randomHitSound.soundClip, audioSource, randomHitSound.volume / 100f);
                }
            }
            else if (state == "DEAD")
            {
                if (deathSounds.Length > 0)
                {
                    // Test done v1.112
                    SoundEffectModel randomDeathSound = GetRandom(deathSounds);
                    if (randomDeathSound.soundClip != null)
                        SoundManager.Instance.PlayOnce(randomDeathSound.soundClip, audioSource, randomDeathSound.volume / 100f);
                }
            }

            lastState = state;
        }
    }

    public void OnDamaged(int amount, DamageType type)
    {
        if (amount <= 0)
            return;

        if (getHitSounds.Length == 0)
            return;

        SoundEffectModel randomGetHitSound = GetRandom(getHitSounds);

        if (health > 0 && randomGetHitSound.soundClip != null)
            SoundManager.Instance.PlayOnce(randomGetHitSound.soundClip, audioSource, randomGetHitSound.volume / 100f);
    }

    private SoundEffectModel GetRandom(SoundEffectModel[] list)
    {
        return list[Random.Range(0, list.Length)];
    }

    private Texture GetMaterialUnderFoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(new Ray(transform.position, -Vector3.up), out hit))
        {
            // Check if its a terrain or 3D Object.
            Terrain t = hit.transform.GetComponent<Terrain>();
            if (t != null)
            {
                // Its a terrain! Boy we have our work to do.
                // Get the terrain data.
                TerrainData data = t.terrainData;
                Vector3 initialPos = t.transform.position;
                int textureIndex = GetMainTexturePosition(transform.position, data, initialPos);

                Texture splat = data.splatPrototypes[textureIndex].texture;

                return splat;
            }
            else
            {
                // Its not a terrain! Easy.
                Material m = hit.transform.gameObject.GetComponent<Renderer>().material;
                return m.mainTexture;
            }
        }

        return null;
    }

    #region Code From Unity Answers

    /*
     * This piece of utility code comes from here:
     * https://answers.unity.com/questions/456973/getting-the-texture-of-a-certain-point-on-terrain.html
     */

    private int GetMainTexturePosition( Vector3 pos, TerrainData data, Vector3 initialPos )
    {
        float[] mix = GetTextureMix(pos, data, initialPos);

        float maxMix = 0;
        int maxIndex = 0;

        // loop through each mix value and find the maximum
        for (int n = 0; n < mix.Length; n++)
        {
            if (mix[n] > maxMix)
            {
                maxIndex = n;
                maxMix = mix[n];
            }
        }
        return maxIndex;
    }

    private float[] GetTextureMix ( Vector3 pos, TerrainData data, Vector3 initialPos)
    {
        // returns an array containing the relative mix of textures
        // on the main terrain at this world position.

        // The number of values in the array will equal the number
        // of textures added to the terrain.

        // calculate which splat map cell the worldPos falls within (ignoring y)

        int mapX = (int)(((pos.x - initialPos.x) / data.size.x) * data.alphamapWidth);
        int mapZ = (int)(((pos.z - initialPos.z) / data.size.z) * data.alphamapHeight);

        // get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
        float[,,] splatmapData = data.GetAlphamaps(mapX, mapZ, 1, 1);

        // extract the 3D array data to a 1D array:
        float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

        for (int n = 0; n < cellMix.Length; n++)
        {
            cellMix[n] = splatmapData[0, 0, n];
        }
        return cellMix;
    }

    #endregion
}
