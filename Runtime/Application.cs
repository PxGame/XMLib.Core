/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/11 14:14:47
 */

using System.Threading;

namespace XMLib
{
    /// <summary>
    /// Application
    /// </summary>
    public class Application : Container
    {
        protected readonly ObjectDriver objectDriver;
        protected readonly EventDispatcher eventDispatcher;

        public readonly int mainThreadId;

        public bool isMainThread { get { return Thread.CurrentThread.ManagedThreadId == mainThreadId; } }

        public Application(int prime = 32)
            : base(prime)
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            this.Instance<EventDispatcher>(eventDispatcher = new EventDispatcher(this));
            this.Instance<ObjectDriver>(objectDriver = new ObjectDriver());
        }

        public void RunUpdate()
        {
            objectDriver?.RunUpdate();
        }

        public void RunLateUpdate()
        {
            objectDriver?.RunLateUpdate();
        }

        public void RunFixedUpdate()
        {
            objectDriver?.RunFixedUpdate();
        }

        public void RunDestroy()
        {
            objectDriver?.RunDestroy();
        }

        #region 重写

        protected override object TriggerOnAfterResolving(BindData bindData, object instance)
        {
            object obj = base.TriggerOnAfterResolving(bindData, instance);
            if (bindData.isStatic)
            {
                objectDriver?.Attach(instance);
            }
            return obj;
        }

        protected override void TriggerOnRelease(BindData bindData, object instance)
        {
            if (bindData.isStatic)
            {
                objectDriver?.Detach(instance);
            }
            base.TriggerOnRelease(bindData, instance);
        }

        #endregion 重写
    }
}