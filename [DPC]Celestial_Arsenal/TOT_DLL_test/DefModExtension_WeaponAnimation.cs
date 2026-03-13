using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class DefModExtension_WeaponAnimation : DefModExtension
    {
        public float attackDuration = 4f;    
        public float impactPause = 2f;      
        public float recoveryDuration = 20f; 
        public float startAngle = 60f;       
        public float endAngle = -70f;       
        public float pivotShift = 0.25f;
    }
}