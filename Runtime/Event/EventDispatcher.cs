/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/11 14:15:57
 */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace XMLib
{
    /// <summary>
    /// EventDispatcher
    /// </summary>
    public class EventDispatcher
    {
        private readonly Dictionary<object, List<EventHandler>> _groupMapping;

        private readonly Dictionary<int, List<EventHandler>> _eventMapping;

        private readonly object _syncRoot;

        private readonly Container container;

        public EventDispatcher(Container container)
        {
            _groupMapping = new Dictionary<object, List<EventHandler>>();
            _eventMapping = new Dictionary<int, List<EventHandler>>();
            _syncRoot = new object();
            this.container = container;
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="target">函数目标对象</param>
        /// <param name="method">函数</param>
        /// <param name="group">分组，当函数非静态时，默认为目标对象</param>
        /// <param name="matchingParams">是否利用容器匹配参数</param>
        /// <returns>事件对象</returns>
        public EventHandler On(int eventType, object target, MethodInfo method, object group = null, bool matchingParams = false)
        {
            if (!method.IsStatic && null == target)
            {//非静态函数,必须有实例
                throw new RuntimeException($"事件 [{eventType}] 注册失败,非静态函数需要指定调用实例");
            }

            EventHandler handler = null;

            lock (_syncRoot)
            {
                handler = EventHandler.Create(eventType, target, method, group, matchingParams);

                List<EventHandler> handlers;

                //注册
                if (!_eventMapping.TryGetValue(handler.eventType, out handlers))
                {
                    handlers = new List<EventHandler>();
                    _eventMapping.Add(handler.eventType, handlers);
                }
                handlers.Add(handler);

                //添加到组
                if (null != handler.group)
                {
                    if (!_groupMapping.TryGetValue(handler.group, out handlers))
                    {
                        handlers = new List<EventHandler>();
                        _groupMapping.Add(handler.group, handlers);
                    }
                    handlers.Add(handler);
                }
            }

            return handler;
        }

        /// <summary>
        /// 取消注册事件
        /// </summary>
        /// <param name="target">
        /// 事件解除目标
        /// <para>如果传入的是字符串(<code>EventType</code>)将会解除对应事件名的所有事件</para>
        /// <para>如果传入的是事件对象(<code>EventHandler</code>)那么解除对应事件</para>
        /// <para>如果传入的是分组(<code>object</code>)会解除该分组下的所有事件</para>
        /// </param>
        public void Off(object target)
        {
            lock (_syncRoot)
            {
                if (target is EventHandler)
                {
                    ForgetHandler((EventHandler)target, true, true);
                    return;
                }

                if (target is int)
                {
                    ForgetEvent((int)target);
                }

                ForgetGroup(target);
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="args">参数</param>
        public void Trigger(int eventType, params object[] args)
        {
            lock (_syncRoot)
            {
                List<EventHandler> handlers;

                if (!_eventMapping.TryGetValue(eventType, out handlers) || handlers.Count == 0)
                {
                    return;
                }

                EventHandler currentHandler = null;
                try
                {
                    //拷贝一份,防止执行过程中修改,
                    //这样做可能出现执行中被移除的EventHandler仍然被执行,
                    //可每次执行时查看handlers中是否存在当前准备执行的EventHandler,
                    //但这样开销有点大,目前先不处理
                    List<EventHandler> copyHandlers = new List<EventHandler>(handlers);
                    foreach (var handler in copyHandlers)
                    {
                        currentHandler = handler;
                        InvokeHandler(currentHandler, args);
                    }
                }
                catch (Exception ex)
                {
                    throw new RuntimeException($"事件 [{eventType}] 调用异常>{currentHandler}\n{ex}");
                }
            }
        }

        private void InvokeHandler(EventHandler handler, params object[] args)
        {
            if (handler.filter != null && !handler.filter(args))
            {//判断
                return;
            }

            if (handler.matchingParams)
            {
                container.Call(handler.target, handler.method, args);
            }
            else
            {
                handler.method.Invoke(handler.target, args);
            }
        }

        private void ForgetGroup(object target)
        {
            List<EventHandler> handlers;

            if (!_groupMapping.TryGetValue(target, out handlers))
            {
                return;
            }

            _groupMapping.Remove(target);

            foreach (var handler in handlers)
            {
                ForgetHandler(handler, true, false);
            }
        }

        private void ForgetEvent(int target)
        {
            List<EventHandler> handlers;

            if (!_eventMapping.TryGetValue(target, out handlers))
            {
                return;
            }

            _eventMapping.Remove(target);

            foreach (var handler in handlers)
            {
                ForgetHandler(handler, false, true);
            }
        }

        private void ForgetHandler(EventHandler target, bool removeEvent, bool removeGroup)
        {
            List<EventHandler> handlers;

            //组中移除
            if (removeGroup && null != target.group && _groupMapping.TryGetValue(target.group, out handlers))
            {
                handlers.Remove(target);
                if (0 == handlers.Count)
                {
                    _groupMapping.Remove(target.group);
                }
            }

            //事件中移除
            if (removeEvent && _eventMapping.TryGetValue(target.eventType, out handlers))
            {
                handlers.Remove(target);
                if (0 == handlers.Count)
                {
                    _eventMapping.Remove(target.eventType);
                }
            }
        }
    }
}