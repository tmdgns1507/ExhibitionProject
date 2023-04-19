using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WarGame
{
    public class SoldierWalk : IState<SoldierController>
    {
        NavMeshAgent agent;
        Animator animator;        
        List<GameObject> seenObjs;
        GameObject nearestTarget;
        float speed;

        public void OperateEnter(SoldierController controller)
        {
            Init(controller);
        }

        public void OperateUpdate(SoldierController controller)
        {
            Debug.Log("SoldierWalk");   //@@ Áö¿ö¾ßµÊ
            ChangeState(controller);
        }

        public void OperateFixedUpdate(SoldierController controller)
        {
            controller.SetNaviAgent(speed, false);
            agent.destination = nearestTarget.transform.position;
        }

        public void OperateExit(SoldierController controller)
        {

        }

        void Init(SoldierController controller)
        {
            speed = controller.data.speed;
            nearestTarget = controller.GetNearestTarget(seenObjs);
            animator = controller.animator;
            agent = controller.agent;
            seenObjs = controller.seenObjects;
        }

        void ChangeState(SoldierController controller)
        {
            if (controller.IsSearchValidTarget() && string.CompareOrdinal(nearestTarget.name, string.Empty) != 0)
            {
                if (controller.GetSqrMagnitude(nearestTarget) < controller.ShootDistance)
                    controller.ChangeState(SoldierController.SoldierState.Attack);
            }
            else
                controller.ChangeState(SoldierController.SoldierState.Run);
        }
    }
}