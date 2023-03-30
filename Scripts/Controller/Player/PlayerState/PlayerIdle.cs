using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WarGame
{
    public class PlayerIdle : IState<PlayerController>
    {
        public void OperateEnter(PlayerController controller)
        {

        }

        public void OperateUpdate(PlayerController controller)
        {   
            if (controller.IsGrounded && controller.IsMoving)
            {
                controller.ChangeState(PlayerController.PlayerState.Move);
            }
        }
        public void OperateFixedUpdate(PlayerController controller)
        {
            if (!controller.IsGrounded) controller.ApplyGravity();
        }

        public void OperateExit(PlayerController controller)
        {

        }
    }
}