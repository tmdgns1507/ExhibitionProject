using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WarGame
{
    public class SoldierAttack : IState<SoldierController>
    {        
        float nextFireTime = 0f;

        public void OperateEnter(SoldierController controller)
        {
            controller.animator.SetBool("Idling", true);
            controller.agent.destination = controller.transform.position;
            controller.agent.isStopped = true;            
        }

        public void OperateUpdate(SoldierController controller)
        {
            GameObject nearestTarget = controller.rader.GetNearestTarget(controller.rader.seenObjects);
            ChangeState(controller, nearestTarget);

                        
            if(controller.IsValidTarget(nearestTarget))
                controller.transform.LookAt(nearestTarget.transform);

            controller.TakeDamageAnim();
            
            nextFireTime += Time.deltaTime;
            if (nearestTarget != null && !nearestTarget.CompareTag("Temp"))
            {
                if (nextFireTime >= controller.data.autoFireRate)
                {
                    Fire(controller, nearestTarget);
                }
            }
        }

        public void OperateFixedUpdate(SoldierController controller)
        {            

        }

        public void OperateExit(SoldierController controller)
        {
            controller.agent.isStopped = false;
        }        

        void ChangeState(SoldierController controller, GameObject nearestTarget = null)
        {
            if (!controller.health.IsAlive) controller.ChangeState(SoldierController.SoldierState.Dead);

            if (controller.IsOccupied) controller.ChangeState(SoldierController.SoldierState.Occupied);

            if (!controller.IsValidTarget(nearestTarget)) controller.ChangeState(SoldierController.SoldierState.Move);

            if (controller.rader.GetSqrMagnitude(nearestTarget) > controller.data.sightDistance) 
                controller.ChangeState(SoldierController.SoldierState.Move);

            if (controller.rader.GetSqrMagnitude(nearestTarget) > controller.ShootDistance
                && controller.rader.GetSqrMagnitude(nearestTarget) < controller.data.sightDistance)
                controller.ChangeState(SoldierController.SoldierState.Track);

        }

        void Fire(SoldierController controller, GameObject nearestTarget = null)
        {
            ShootAnim(controller);
            EmitHitEffect(controller, nearestTarget);
            nextFireTime = 0f;
        }

        void ShootAnim(SoldierController controller)
        {
            controller.animator.SetBool("Idling", true);
            controller.animator.SetTrigger("Use");
        }


        void EmitHitEffect(SoldierController controller, GameObject nearestTarget)
        {
            GameObject hands = controller.data.handsObject;
            Vector3 rayDirection = controller.transform.forward;
            if (nearestTarget != null) rayDirection = (nearestTarget.transform.position - hands.transform.position).normalized;

            Ray r = new Ray(hands.transform.position, rayDirection);
            RaycastHit hitInfo;

            if (Physics.Raycast(r, out hitInfo, 1000f, controller.FireCollisionLayer, QueryTriggerInteraction.Ignore))
            {
                IHittableObject hittable = hitInfo.collider.GetComponent<IHittableObject>();

                if (hittable == null)
                {
                    hitInfo.collider.GetComponentInParent<IHittableObject>();
                }

                if (hittable != null)
                {
                    DamageData damage = new DamageData();
                    damage.DamageAmount = controller.data.damageAmount;
                    damage.HitDirection = r.direction;
                    damage.HitPosition = hitInfo.point;
                    hittable.TakeDamage(damage);
                }
            }
        }
    }
}