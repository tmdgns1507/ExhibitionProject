using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WarGame
{
    public class SoldierIdle : IState<SoldierController>
    {
        float time;
        float minTime = 80f;
        float maxTime = 100f;
        float startTime;

        public void OperateEnter(SoldierController controller)
        {            
            time = 0f;
            startTime = Random.Range(minTime, maxTime);
            Debug.Log($"{controller.gameObject.name} @@ {startTime}");
            controller.animator.SetBool("Idling", true);
            controller.SetNaviAgent(0f);
            controller.agent.isStopped = true;
        }

        public void OperateUpdate(SoldierController controller)
        {            
            time += Time.deltaTime;

            controller.TakeDamageAnim();

            if (controller.health.IsAlive && time > startTime)
                controller.ChangeState(SoldierController.SoldierState.Move);
        }

        public void OperateFixedUpdate(SoldierController controller)
        {
            
        }

        public void OperateExit(SoldierController controller)
        {
            
        }
    }
}