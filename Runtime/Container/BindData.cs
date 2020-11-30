/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/11 16:51:31
 */

using System;
using System.Collections.Generic;

namespace XMLib
{
    /// <summary>
    /// BindData
    /// </summary>
    public class BindData
    {
        public Func<Container, object[], object> concrete { get; private set; }
        public string service { get; private set; }
        public bool isStatic { get; private set; }

        private readonly Container container;
        private List<Action<BindData, object>> _resolving;
        private List<Action<BindData, object>> _afterResolving;
        private List<Action<BindData, object>> _release;

        public BindData(Container container, string service, Func<Container, object[], object> concrete, bool isStatic)
        {
            this.container = container;
            this.service = service;
            this.concrete = concrete;
            this.isStatic = isStatic;
        }

        public void Unbind()
        {
            ReleaseBind();
        }

        public BindData Alias(string alias)
        {
            container.Alias(alias, service);
            return this;
        }

        public BindData Alias<T>()
        {
            container.Alias(container.Type2Service<T>(), service);
            return this;
        }

        public BindData ResetAlias<T>()
        {
            container.ResetAlias(container.Type2Service<T>(), service);
            return this;
        }

        public BindData Alias(Type aliasType)
        {
            container.Alias(container.Type2Service(aliasType), service);
            return this;
        }

        private void ReleaseBind()
        {
            container.Unbind(this);
        }

        public BindData OnResolving(Action<BindData, object> callback)
        {
            AddCallback(callback, ref _resolving);
            return this;
        }

        public BindData OnAfterResolving(Action<BindData, object> callback)
        {
            AddCallback(callback, ref _afterResolving);
            return this;
        }

        public BindData OnRelease(Action<BindData, object> callback)
        {
            if (!isStatic)
            {
                throw new RuntimeException($"服务 [{service}] 不是单例 , 不能调用 {nameof(OnRelease)}().");
            }

            AddCallback(callback, ref _release);
            return this;
        }

        internal object TriggerResolving(object instance)
        {
            return Container.Trigger(this, instance, _resolving);
        }

        internal object TriggerAfterResolving(object instance)
        {
            return Container.Trigger(this, instance, _afterResolving);
        }

        internal object TriggerRelease(object instance)
        {
            return Container.Trigger(this, instance, _release);
        }

        private void AddCallback(Action<BindData, object> callback, ref List<Action<BindData, object>> collection)
        {
            if (collection == null)
            {
                collection = new List<Action<BindData, object>>();
            }

            collection.Add(callback);
        }
    }
}