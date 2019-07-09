using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OaStudios.SoundManager
{
    [Serializable]
    public struct SoundEffectModel
    {
        public string friendlyName;
        public SoundType soundType;
        public ScriptableSkill skill;
        public float animSecond;
        public AudioClip soundClip;
        public AnimationClip animationClip;
        public Texture material;



        [Range(0, 100)]
        public int volume;
    }
}