using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WarGame
{
    public class SoldierTrack : IState<SoldierController>
    {
        GameObject nearestTarget;

        public void OperateEnter(SoldierController controller)
        {
            Init(controller);
            controller.agent.isStopped = false;
            controller.animator.SetBool("Idling", false);
            controller.animator.SetBool("Walk", true);
        }

        public void OperateUpdate(SoldierController controller)
        {
            controller.TakeDamageAnim();
            ChangeState(controller);
        }

        public void OperateFixedUpdate(SoldierController controller)
        {
            controller.SetNaviAgent(controller.data.speed, false);
            controller.agent.destination = nearestTarget.transform.position;
        }

        public void OperateExit(SoldierController controller)
        {
            controller.animator.SetBool("Walk", false);
        }

        void Init(SoldierController controller)
        {
            nearestTarget = controller.rader.GetNearestTarget(controller.rader.seenObjects);
        }

        void ChangeState(SoldierController controller)
        {
            if (!controller.health.IsAlive) controller.ChangeState(SoldierController.SoldierState.Dead);

            else if (controller.IsOccupied) controller.ChangeState(SoldierController.SoldierState.Occupied);

            else if (controller.IsValidTarget(nearestTarget) && controller.rader.GetSqrMagnitude(nearestTarget) <= controller.ShootDistance)
                controller.ChangeState(SoldierController.SoldierState.Attack);

            else
                controller.ChangeState(SoldierController.SoldierState.Move);
        }
    }
}