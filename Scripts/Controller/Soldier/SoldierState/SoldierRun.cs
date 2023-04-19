using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WarGame
{
    public class SoldierRun : IState<SoldierController>
    {
        NavMeshAgent agent;
        Animator animator;
        Transform dest;        
        float runSpeed;

        public void OperateEnter(SoldierController controller)
        {
            Init(controller);
        }

        public void OperateUpdate(SoldierController controller)
        {
            Debug.Log("SoldierRun");   //@@ 지워야됨
            ChangeState(controller);
        }

        public void OperateFixedUpdate(SoldierController controller)
        {
            controller.SetNaviAgent(runSpeed, false);
            agent.destination = dest.transform.position;
        }

        public void OperateExit(SoldierController controller)
        {

        }


        void Init(SoldierController controller)
        {
            runSpeed = controller.data.runSpeed;
            dest = controller.destination;
            animator = controller.animator;
            agent = controller.agent;            
        }

        void ChangeState(SoldierController controller)
        {
            GameObject nearestTarget = controller.GetNearestTarget(controller.seenObjects);
            Debug.Log($"가장 가까운 타겟 : {nearestTarget}");

            if (string.CompareOrdinal(nearestTarget.name, string.Empty) != 0)
            {
                float near = controller.GetSqrMagnitude(nearestTarget);
                float shoot = controller.ShootDistance;
                if (controller.GetSqrMagnitude(nearestTarget) < controller.ShootDistance)
                    controller.ChangeState(SoldierController.SoldierState.Attack);
                else
                    controller.ChangeState(SoldierController.SoldierState.Walk);
            }
        }
    }
}