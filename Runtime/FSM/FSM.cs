/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2020/10/16 17:07:49
 */

using System;
using System.Collections.Generic;

namespace XMLib.FSM
{
    /// <summary>
    /// FSM
    /// </summary>
    public class FSM<T> : Dictionary<Type, IFSMState<T>>, IFSM<T>
    {
        public Type currentState { get; protected set; }

        public Type previousState { get; protected set; }

        protected Type nextState { get; set; }
        protected bool hasNextState { get; set; }

        public void AddState<S>(S state) where S : IFSMState<T>
        {
            Add(typeof(S), state);
        }

        public void ChangeState(Type stateType)
        {
            if (stateType == currentState)
            {
                return;
            }

            nextState = stateType;
            hasNextState = true;
        }

        public void ChangeState<S>() where S : IFSMState<T>
        {
            ChangeState(typeof(S));
        }

        public void Update(T target, float deltaTime)
        {
            CheckStateChange(target);
            if (currentState != null)
            {
                this[currentState].Update(target, deltaTime);
                CheckStateChange(target);
            }
        }

        protected void CheckStateChange(T target)
        {
            if (hasNextState)
            {//切换状态
                hasNextState = false;
                if (currentState != null)
                {
                    this[currentState].Exit(target);
                }
                currentState = nextState;
                nextState = null;
                if (currentState != null)
                {
                    this[currentState].Enter(target);
                }
            }
        }
    }
}