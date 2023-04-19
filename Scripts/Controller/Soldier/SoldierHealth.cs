using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WarGame
{
    public class SoldierHealth : BaseHittableObject
    {        
        public float PlayerGrenadeDamageMul = 2f;
        bool isTakeDamage = false;

        public new bool IsAlive { get { return Health > 0; } }

        public bool IsTakeDamage { get { return IsAlive && isTakeDamage; } }

        private void Awake()
        {   
            StartHealth = Health;
        }

        public override void TakeDamage(DamageData damage)
        {
            isTakeDamage = true;

            if (damage.HitType == DamageData.DamageType.Explosion)
            {
                damage.DamageAmount *= PlayerGrenadeDamageMul;
            }

            damage.Receiver = this;

            if (!Invulnerable)
                Health -= damage.DamageAmount;

            if (damage.Deadly)
                Health = 0;

            isTakeDamage = false;
        }

       
    }
}