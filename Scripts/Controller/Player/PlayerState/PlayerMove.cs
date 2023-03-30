using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WarGame
{
    public class PlayerMove : IState<PlayerController>
    {
        public void OperateEnter(PlayerController controller)
        {

        }

        public void OperateUpdate(PlayerController controller)
        {
            Debug.Log("Move State");
        }
        public void OperateFixedUpdate(PlayerController controller)
        {
            if(controller.IsGrounded) 
                controller.Move();
            else 
                controller.ApplyGravity();

            //if (controller.IsGrounded && controller.IsMoving)
            //{
            //    Debug.Log("Move Method");
            //    controller.Move();
            //}
        }

        public void OperateExit(PlayerController controller)
        {

        }
    }
}