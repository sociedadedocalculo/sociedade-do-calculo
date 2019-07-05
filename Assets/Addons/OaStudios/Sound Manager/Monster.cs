using OaStudios.SoundManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public partial class Monster {

    private string lastState;
    private AudioSource audioSource;
    private BaseSoundEffect soundEffects;
    private int previousHealth;

    private SoundEffectModel leftFootSound, rightFootSound, getHitSound, hitSound, deathSound;

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

        previousHealth = health;

        hitSound = soundEffects.Effects.Where(i => i.soundType == SoundType.Attack).FirstOrDefault();
        deathSound = soundEffects.Effects.Where(i => i.soundType == SoundType.Death).FirstOrDefault();
        getHitSound = soundEffects.Effects.Where(i => i.soundType == SoundType.GetHit).FirstOrDefault();
        leftFootSound = soundEffects.Effects.Where(i => i.soundType == SoundType.LeftFoot).FirstOrDefault();
        rightFootSound = soundEffects.Effects.Where(i => i.soundType == SoundType.RightFoot).FirstOrDefault();

        if (animator != null)
        {
            if (leftFootSound.soundClip != null && leftFootSound.animationClip != null)
            {
                AnimationEvent clipEvent = new AnimationEvent();
                clipEvent.time = leftFootSound.animSecond;
                clipEvent.functionName = "LeftFootStep";
                leftFootSound.animationClip.AddEvent(clipEvent);
            }

            if (rightFootSound.soundClip != null && rightFootSound.animationClip != null)
            {
                AnimationEvent clipEvent = new AnimationEvent();
                clipEvent.time = rightFootSound.animSecond;
                clipEvent.functionName = "RightFootStep";
                rightFootSound.animationClip.AddEvent(clipEvent);
            }
        }
    }

    void LeftFootStep()
    {
        if ( !enableSoundEffects )
            return;

        SoundManager.Instance.PlayOnce(leftFootSound.soundClip, audioSource, leftFootSound.volume / 100f);
    }

    void RightFootStep()
    {
        if ( !enableSoundEffects )
            return;

        SoundManager.Instance.PlayOnce(rightFootSound.soundClip, audioSource, rightFootSound.volume / 100f);
    }

    [Client]
    void UpdateClient_SoundManager()
    {
        if (!enableSoundEffects)
            return;

        if (lastState != state)
        {
            if (state == "DEAD") {
                if (deathSound.soundClip != null)
                    SoundManager.Instance.PlayOnce(deathSound.soundClip, audioSource, deathSound.volume / 100f);
            }
            else if (state == "CASTING")
            {
                // Is there a sound attached to current skill?
                string skillName = skills[currentSkill].name;
                SoundEffectModel skillSound = soundEffects.Effects.Where(i => i.skill != null && i.skill.name.Equals(skillName)).FirstOrDefault();

                if (skillSound.soundClip != null)
                    SoundManager.Instance.PlayOnce(skillSound.soundClip, audioSource, skillSound.volume / 100f);
            }

            lastState = state;
        }

        if (health < previousHealth && health > 0)
            SoundManager.Instance.PlayOnce(getHitSound.soundClip, audioSource, getHitSound.volume / 100f);

        previousHealth = health;
    }

    public override void DealDamageAt(Entity entity, int amount, float stunChance = 0, float stunTime = 0) {
        base.DealDamageAt(entity, amount, stunChance, stunTime);

        if (!enableSoundEffects)
            return;

        SoundManager.Instance.PlayOnce(hitSound.soundClip, audioSource, hitSound.volume / 100f);
    }
}
