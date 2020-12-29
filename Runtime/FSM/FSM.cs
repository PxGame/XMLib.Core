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
    public class FSM<T> : IFSM<T>
    {
        public virtual Type currentState { get; protected set; }

        public virtual Type previousState { get; protected set; }

        protected virtual Type nextState { get; set; }
        protected virtual bool hasNextState { get; set; }

        protected Dictionary<Type, IFSMState<T>> type2State = new Dictionary<Type, IFSMState<T>>();

        protected List<Type> typeTmps = new List<Type>();

        public virtual IFSMState<T> GetState(Type stateType)
        {
            return type2State.TryGetValue(stateType, out IFSMState<T> result) ? result : null;
        }

        public S GetState<S>() where S : class, IFSMState<T>
        {
            return GetState(typeof(S)) as S;
        }

        public virtual void AddState(IFSMState<T> state)
        {
            type2State.Add(state.GetType(), state);
        }

        public void AddStates<S>(IEnumerable<S> states) where S : IFSMState<T>
        {
            foreach (var state in states)
            {
                AddState(state);
            }
        }

        public virtual void RemoveState(Type type)
        {
            type2State.Remove(type);
            currentState = currentState != type ? currentState : null;
            previousState = previousState != type ? previousState : null;
            nextState = nextState != type ? nextState : null;
        }

        public void RemoveState<S>() where S : IFSMState<T>
        {
            RemoveState(typeof(S));
        }

        public void RemoveStateAll(Type type)
        {
            typeTmps.Clear();
            foreach (var item in type2State)
            {
                if (type.IsAssignableFrom(item.Key))
                {
                    typeTmps.Add(item.Key);
                }
            }

            foreach (var item in typeTmps)
            {
                RemoveState(item);
            }
            typeTmps.Clear();
        }

        public void RemoveStateAll<S>()
        {
            RemoveStateAll(typeof(S));
        }

        public virtual void ChangeState(Type stateType)
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

        public virtual void Update(T target, float deltaTime)
        {
            CheckStateChange(target);
            if (currentState != null)
            {
                type2State[currentState].Update(target, deltaTime);
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
                    type2State[currentState].Exit(target);
                }
                currentState = nextState;
                nextState = null;
                if (currentState != null)
                {
                    type2State[currentState].Enter(target);
                }
            }
        }
    }
}