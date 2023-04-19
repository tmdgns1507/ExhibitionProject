using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarGame
{
    public class SoldierData : MonoBehaviour
    {
        [Header("Stats")]
        public GameObject handsObject;
        public float damageAmount = 10f;
        public float speed = 0.5f;
        public float runSpeed = 1.5f;
        public float waitTime = 3f;
        public float autoFireRate = 0.3f;
        public float effectiveAttackRange = 20f;
        public float viewAngle = 110f;
        public float sightDistance = 2000f;
        public float gravity = 9.81f;
        
        [Header("Draw Ray")]
        public bool isDrawRays = true;
        public bool isDrawVisionCone = true;
        public bool isDrawOverlapSphere = false;


        [HideInInspector] public string Player = "Player";
        [HideInInspector] public string Ally = "Ally";
        [HideInInspector] public string Enemy = "Enemy";
        bool isEnemy = false;

        [HideInInspector] public float retreatDistance;
        [HideInInspector] public Dictionary<GameObject, float> distanceToTargets = new Dictionary<GameObject, float>();

        public bool IsEnemy { get { return isEnemy; } }

        private void Awake()
        {
            IdentificationOfPeer();
            retreatDistance = sightDistance + (sightDistance / 3);
        }

        private void OnEnable()
        {
            distanceToTargets = new Dictionary<GameObject, float>();
            distanceToTargets.Clear();
        }

        private void Update()
        {
            retreatDistance = sightDistance + (sightDistance / 3);
            SetDistanceToTargets();
        }

        void SetDistanceToTargets()
        {
            if (distanceToTargets == null) return;

            foreach (GameObject target in GetTargets(IsEnemy))
            {
                if (!distanceToTargets.ContainsKey(target))
                    distanceToTargets.Add(target, GetTargetDistance(target));
                else
                    distanceToTargets[target] = GetTargetDistance(target);
            }
        }

        List<GameObject> GetTargets(bool isEnemy)
        {
            List<GameObject> targets = new List<GameObject>();
            if (isEnemy)
            {
                targets = GameObject.FindGameObjectsWithTag("Ally").ToList();
                targets.Add(GameObject.FindGameObjectWithTag("Player"));
            }
            else
            {
                targets = GameObject.FindGameObjectsWithTag("Enemy").ToList();
            }
            return targets;
        }

        float GetTargetDistance(GameObject target)
        {
            return (target.transform.position - this.transform.position).sqrMagnitude;
        }

        void IdentificationOfPeer()
        {
            if (string.CompareOrdinal(Enemy, this.gameObject.tag) == 0)
                isEnemy = true;
            else if (string.CompareOrdinal(Ally, this.gameObject.tag) == 0)
                isEnemy = false;
            else
                Debug.LogError($" @{this.gameObject.name} is Invalid Tag.");
        }
    }
}