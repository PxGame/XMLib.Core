/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/13 0:09:58
 */

using System;

namespace XMLib
{
    /// <summary>
    /// ContainerExtension
    /// </summary>
    public static class ContainerExtension
    {
        public static Container Alias<TAlias, TService>(this Container container)
        {
            return container.Alias(container.Type2Service(typeof(TAlias)), container.Type2Service(typeof(TService)));
        }

        public static Container RemoveAlias<TAlias>(this Container container)
        {
            return container.RemoveAlias(container.Type2Service(typeof(TAlias)));
        }

        public static Container ResetAlias<TAlias, TService>(this Container container)
        {
            return container.ResetAlias(container.Type2Service(typeof(TAlias)), container.Type2Service(typeof(TService)));
        }

        public static BindData Bind<T>(this Container container)
        {
            return container.Bind(container.Type2Service(typeof(T)), typeof(T), false);
        }

        public static BindData Bind<TService, TConcrete>(this Container container)
        {
            return container.Bind(container.Type2Service(typeof(TService)), typeof(TConcrete), false);
        }

        public static BindData Bind<TService>(this Container container, Func<Container, object[], object> concrete)
        {
            return container.Bind(container.Type2Service(typeof(TService)), concrete, false);
        }

        public static BindData Bind<TService>(this Container container, Func<object[], object> concrete)
        {
            return container.Bind(container.Type2Service(typeof(TService)), (c, p) => concrete.Invoke(p), false);
        }

        public static BindData Bind(this Container container, string service, Func<Container, object[], object> concrete)
        {
            return container.Bind(service, concrete, false);
        }

        public static void Call<T>(this Container container, Action<T> method, params object[] userParams)
        {
            container.Call(method.Target, method.Method, userParams);
        }

        public static void Call<T1, T2>(this Container container, Action<T1, T2> method, params object[] userParams)
        {
            container.Call(method.Target, method.Method, userParams);
        }

        public static void Call<T1, T2, T3>(this Container container, Action<T1, T2, T3> method, params object[] userParams)
        {
            container.Call(method.Target, method.Method, userParams);
        }

        public static void Call<T1, T2, T3, T4>(this Container container, Action<T1, T2, T3, T4> method, params object[] userParams)
        {
            container.Call(method.Target, method.Method, userParams);
        }

        public static object Call(this Container container, object target, string method, params object[] userParams)
        {
            var methodInfo = target.GetType().GetMethod(method);

            if (methodInfo == null)
            {
                throw new RuntimeException($"在对象 [{target}] 上没有找到 [{method}] 函数");
            }

            return container.Call(target, methodInfo, userParams);
        }

        public static TService Make<TService>(this Container container, params object[] userParams)
        {
            return (TService)container.Make(container.Type2Service(typeof(TService)), userParams);
        }

        public static object Make(this Container container, Type service, params object[] userParams)
        {
            return container.Make(container.Type2Service(service), userParams);
        }

        public static bool CanMake<T>(this Container container)
        {
            return container.CanMake(container.Type2Service(typeof(T)));
        }

        public static Func<TService> Factory<TService>(this Container container, params object[] userParams)
        {
            return () => (TService)container.Make(container.Type2Service(typeof(TService)), userParams);
        }

        public static Func<object> Factory(this Container container, string service, params object[] userParams)
        {
            return () => container.Make(service, userParams);
        }

        public static BindData GetBind<T>(this Container container)
        {
            return container.GetBind(container.Type2Service(typeof(T)));
        }

        public static bool HasBind<T>(this Container container)
        {
            return container.HasBind(container.Type2Service(typeof(T)));
        }

        public static bool HasInstance<T>(this Container container)
        {
            return container.HasInstance(container.Type2Service(typeof(T)));
        }

        public static object Instance<TService>(this Container container, object instance)
        {
            return container.Instance(container.Type2Service(typeof(TService)), instance);
        }

        public static bool IsAlias<T>(this Container container)
        {
            return container.IsAlias(container.Type2Service(typeof(T)));
        }

        public static bool IsResolved<T>(this Container container)
        {
            return container.IsResolved(container.Type2Service(typeof(T)));
        }

        public static bool IsStatic<T>(this Container container)
        {
            return container.IsStatic(container.Type2Service(typeof(T)));
        }

        public static Container OnAfterResolving(this Container container, Action<object> callback)
        {
            return container.OnAfterResolving((bindData, instance) =>
            {
                callback(instance);
            });
        }

        public static Container OnAfterResolving<T>(this Container container, Action<T> callback)
        {
            return container.OnAfterResolving((bindData, instance) =>
            {
                if (instance is T)
                {
                    callback((T)instance);
                }
            });
        }

        public static Container OnAfterResolving<T>(this Container container, Action<BindData, T> callback)
        {
            return container.OnAfterResolving((bindData, instance) =>
            {
                if (instance is T)
                {
                    callback(bindData, (T)instance);
                }
            });
        }

        public static Container OnRelease(this Container container, Action<object> callback)
        {
            return container.OnRelease((bindData, instance) => callback(instance));
        }

        public static Container OnRelease<T>(this Container container, Action<T> callback)
        {
            return container.OnRelease((bindData, instance) =>
            {
                if (instance is T)
                {
                    callback((T)instance);
                }
            });
        }

        public static Container OnRelease<T>(this Container container, Action<BindData, T> callback)
        {
            return container.OnRelease((bindData, instance) =>
            {
                if (instance is T)
                {
                    callback(bindData, (T)instance);
                }
            });
        }

        public static Container OnResolving(this Container container, Action<object> callback)
        {
            return container.OnResolving((bindData, instance) =>
            {
                callback(instance);
            });
        }

        public static Container OnResolving<T>(this Container container, Action<T> callback)
        {
            return container.OnResolving((bindData, instance) =>
            {
                if (instance is T)
                {
                    callback((T)instance);
                }
            });
        }

        public static Container OnResolving<T>(this Container container, Action<BindData, T> callback)
        {
            return container.OnResolving((bindData, instance) =>
            {
                if (instance is T)
                {
                    callback(bindData, (T)instance);
                }
            });
        }

        public static bool Release<TService>(this Container container)
        {
            return container.Release(container.Type2Service(typeof(TService)));
        }

        public static BindData Singleton(this Container container, string service, Func<Container, object[], object> concrete)
        {
            return container.Bind(service, concrete, true);
        }

        public static BindData Singleton<TService, TConcrete>(this Container container)
        {
            return container.Bind(container.Type2Service(typeof(TService)), typeof(TConcrete), true);
        }

        public static BindData Singleton<TService>(this Container container)
        {
            return container.Bind(container.Type2Service(typeof(TService)), typeof(TService), true);
        }

        public static BindData Singleton(this Container container, Type service)
        {
            return container.Bind(container.Type2Service(service), service, true);
        }

        public static BindData Singleton<TService>(this Container container, Func<Container, object[], object> concrete)
        {
            return container.Bind(container.Type2Service(typeof(TService)), concrete, true);
        }

        public static BindData Singleton<TService>(this Container container, Func<object[], object> concrete)
        {
            return container.Bind(container.Type2Service(typeof(TService)), (c, p) => concrete.Invoke(p), true);
        }

        public static BindData Singleton<TService>(this Container container, Func<object> concrete)
        {
            return container.Bind(container.Type2Service(typeof(TService)), (c, p) => concrete.Invoke(), true);
        }

        public static object CreateInstance<T>(this Container container, Type realServiceType, object[] userParams)
        {
            return container.CreateInstance(container.GetBindFillable<T>(), realServiceType, userParams);
        }

        public static BindData GetBindFillable<T>(this Container container)
        {
            return container.GetBindFillable(container.Type2Service<T>());
        }

        public static string Type2Service<TService>(this Container container)
        {
            return container.Type2Service(typeof(TService));
        }

        public static void Unbind<TService>(this Container container)
        {
            container.Unbind(container.Type2Service(typeof(TService)));
        }

        public static void Watch<TService>(this Container container, Action method)
        {
            container.OnRebound(container.Type2Service(typeof(TService)), (instance) => method());
        }

        public static void Watch<TService>(this Container container, Action<TService> method)
        {
            container.OnRebound(container.Type2Service(typeof(TService)), (instance) => method((TService)instance));
        }
    }
}