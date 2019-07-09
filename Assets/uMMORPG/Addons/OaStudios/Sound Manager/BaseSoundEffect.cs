using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OaStudios.SoundManager
{
    public class BaseSoundEffect : MonoBehaviour
    {
        void Start()
        {
            
        }

        [SerializeField]
        private SoundEffectModel[] effects;

        public SoundEffectModel[] Effects
        {
            get { return effects; }
            set { effects = value; }
        }
    }
}
