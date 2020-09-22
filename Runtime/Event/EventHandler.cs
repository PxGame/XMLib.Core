/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/11 14:16:58
 */

using System;
using System.Reflection;

namespace XMLib
{
    /// <summary>
    /// EventHandler
    /// </summary>
    public class EventHandler
    {
        public int eventType { get; private set; }
        public object group { get; private set; }
        public MethodInfo method { get; private set; }
        public object target { get; private set; }
        public bool matchingParams { get; private set; }
        public Func<object[], bool> filter { get; private set; }

        public EventHandler SetFilter(Func<object[], bool> filter)
        {
            this.filter = filter;
            return this;
        }

        public static EventHandler Create(int eventType, object target, MethodInfo method, object group, bool matchingParams)
        {
            if (!method.IsStatic && null == target)
            {
                throw new RuntimeException($"事件 [{eventType}] 函数 [{method}] 不是静态函数，必须指定目标对象");
            }

            EventHandler handler = new EventHandler();

            handler.eventType = eventType;
            handler.method = method;
            handler.target = target;
            handler.matchingParams = matchingParams;
            handler.group = group;

            if (!method.IsStatic && null == group)
            {//如果没有设置组，那么默认设置为目标对象
                handler.group = target;
            }

            return handler;
        }

        public override string ToString()
        {
            return $"[{nameof(EventHandler)}]EventType:{eventType},MethodInfo:{method},Target:{target},Group:{group},MatchingParams:{matchingParams}";
        }
    }
}