/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2020/10/16 17:08:54
 */

namespace XMLib.FSM
{
    /// <summary>
    /// IFSMState
    /// </summary>
    public interface IFSMState<T>
    {
        /// <summary>
        /// 进入
        /// </summary>
        void Enter(T target);

        /// <summary>
        /// 退出
        /// </summary>
        void Exit(T target);

        /// <summary>
        /// 更新
        /// </summary>
        void Update(T target, float deltaTime);
    }
}