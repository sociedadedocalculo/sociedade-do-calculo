using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OaStudios.SoundManager
{
    public enum SoundType { Misc, Idle, Attack, Miss, LeftFoot, RightFoot, GenericAnimation, GetHit, Death, LevelUp, Skill }

    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance = null;

        // Use this for initialization
        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public void PlayOnce(AudioClip clip, AudioSource source, float volume)
        {
            source.PlayOneShot(clip, volume);
        }
    }
}