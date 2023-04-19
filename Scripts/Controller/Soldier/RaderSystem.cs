using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace WarGame
{
    public class RaderSystem : MonoBehaviour
    {
        public GameObject temp;

        [HideInInspector] public List<GameObject> seenObjects;

        SoldierData data;
        GameObject parent;
        string tag = string.Empty;
        bool drawRays;
        bool drawVisionCone;
        bool drawOverlapSphere;
        bool isClearingTargetObj;
        Vector3 offset = Vector3.zero;

        int OverlapSphereLayer
        {
            get
            {
                if (data.IsEnemy)
                {
                    return 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Ally");
                }
                else if (!data.IsEnemy)
                {
                    return 1 << LayerMask.NameToLayer("Enemy");
                }
                else
                    return 0;
            }
        }

        private void Awake()
        {
            data = GetComponentInParent<SoldierData>();

            parent = transform.parent.gameObject;
            tag = parent.tag;
        }

        private void OnEnable()
        {
            if (seenObjects != null) seenObjects.Clear();
        }

        void Start()
        {
            seenObjects = new List<GameObject>();
        }

        void Update()
        {
            SearchTargetObject();
        }

        public float GetSqrMagnitude(GameObject target)
        {
            return (target.transform.position - parent.transform.position).sqrMagnitude;
        }

        public GameObject GetNearestTarget(List<GameObject> seenObjs)
        {
            float minDistance = data.sightDistance;
            GameObject target = temp;

            if (seenObjs == null) return target;

            foreach (GameObject go in seenObjs)
            {
                float targetDistance = GetSqrMagnitude(go);
                if (targetDistance < minDistance)
                {
                    minDistance = targetDistance;
                    target = go;
                }
            }

            return target;
        }

        void SearchTargetObject()
        {
            drawRays = data.isDrawRays;
            drawVisionCone = data.isDrawVisionCone;
            drawOverlapSphere = data.isDrawVisionCone;

            RaycastHit hit;

            Collider[] hitColliders = Physics.OverlapSphere(parent.transform.position, data.sightDistance, OverlapSphereLayer);
            for (int i = 0; i < hitColliders.Length; i += 1)
            {
                GameObject target = hitColliders[i].gameObject;

                Vector3 dirToTarget = (target.transform.position - parent.transform.position).normalized;
                if (Vector3.Angle(parent.transform.forward, dirToTarget) < data.viewAngle / 2)
                {
                    if (drawRays)
                        Debug.DrawRay(parent.transform.position, dirToTarget * data.sightDistance, Color.blue);

                    if (Physics.Raycast(parent.transform.position, dirToTarget, out hit, data.sightDistance))
                    {
                        if (hit.collider.gameObject.activeSelf)
                            seenObjects.Add(hit.collider.gameObject);

                        if (!isClearingTargetObj)
                            StartCoroutine(ClearSeenList());
                    }
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (drawOverlapSphere)
                Gizmos.DrawWireSphere(parent.transform.position, data.sightDistance);

            if (drawVisionCone)
            {
#if UNITY_EDITOR
                Color mColor = Handles.color;
                Color color = Color.blue;
                color.a = 0.1f;
                Handles.color = color;
                var halfFOV = data.viewAngle * 0.5f;
                var beginDirection = Quaternion.AngleAxis(-halfFOV, (Vector3.up)) * (parent.transform.forward);
                Handles.DrawSolidArc(parent.transform.TransformPoint(offset), parent.transform.up, beginDirection,
                    data.viewAngle, data.sightDistance);
                Handles.color = mColor;
#endif
            }

        }

        IEnumerator ClearSeenList()
        {
            isClearingTargetObj = true;
            yield return new WaitForSecondsRealtime(0.5f);
            seenObjects.Clear();
            isClearingTargetObj = false;
        }


    }
}