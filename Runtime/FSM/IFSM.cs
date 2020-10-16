/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2020/10/16 17:08:38
 */

using System;

namespace XMLib.FSM
{
    /// <summary>
    /// IFSM
    /// </summary>
    public interface IFSM<T>
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        Type currentState { get; }

        /// <summary>
        /// 上一个状态
        /// </summary>
        Type previousState { get; }

        /// <summary>
        /// 状态更新
        /// </summary>
        void Update(T target, float deltaTime);

        /// <summary>
        /// 切换状态
        /// </summary>
        void ChangeState(Type stateType);
    }
}