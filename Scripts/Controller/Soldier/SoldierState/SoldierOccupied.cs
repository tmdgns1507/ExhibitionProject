using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarGame
{
    public class SoldierOccupied : IState<SoldierController>
    {
        int destinationCount = 0;

        public void OperateEnter(SoldierController controller)
        {            
            controller.agent.isStopped = true;
            destinationCount = controller.destinations.Count;
            controller.destination = GetNextDestination(controller);
            controller.ChangeState(SoldierController.SoldierState.Move);
        }

        public void OperateUpdate(SoldierController controller)
        {
            controller.TakeDamageAnim();
        }

        public void OperateFixedUpdate(SoldierController controller)
        {

        }

        public void OperateExit(SoldierController controller)
        {
            controller.agent.isStopped = false;
        }

        int GetCurrentDestinationIdx(SoldierController controller)
        {
            for (int i = 0; i < destinationCount; i++)
            {
                if (controller.destinations[i].Equals(controller.destination))
                    return i;
            }
            return 0;            
        }

        Transform GetNextDestination(SoldierController controller)
        {
            while(destinationCount > 0)
            {
                int nextDest = Random.Range(0, destinationCount);
                if (nextDest != GetCurrentDestinationIdx(controller))
                    return controller.destinations[nextDest];
            }
            return controller.destinations[0];
        }
    }
}