using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WarGame
{
    public class SoldierMove : IState<SoldierController>
    {
        public void OperateEnter(SoldierController controller)
        {
            controller.agent.isStopped = false;
            controller.animator.SetBool("Idling", false);
            controller.animator.SetBool("Walk", false);
        }

        public void OperateUpdate(SoldierController controller)
        {
            controller.TakeDamageAnim();
            ChangeState(controller);
        }

        public void OperateFixedUpdate(SoldierController controller)
        {
            controller.SetNaviAgent(controller.data.runSpeed, false);
            controller.agent.destination = controller.destination.transform.position;
        }

        public void OperateExit(SoldierController controller)
        {

        }

        void ChangeState(SoldierController controller)
        {
            GameObject nearestTarget = controller.rader.GetNearestTarget(controller.rader.seenObjects);

            if (!controller.health.IsAlive) controller.ChangeState(SoldierController.SoldierState.Dead);

            if (controller.IsOccupied) controller.ChangeState(SoldierController.SoldierState.Occupied);

            if (controller.IsValidTarget(nearestTarget) && nearestTarget != null)
            {
                if (controller.rader.GetSqrMagnitude(nearestTarget) < controller.ShootDistance)
                    controller.ChangeState(SoldierController.SoldierState.Attack);
                else
                    controller.ChangeState(SoldierController.SoldierState.Track);
            }

        }
    }
}