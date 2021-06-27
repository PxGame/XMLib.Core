/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2021/6/25 2:06:39
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace XMLib
{
    public enum LaunchMode
    {
        Normal
    }

    public enum LaunchStatus
    {
        None = 0,
        Initing,
        Inited,
        TypeReged,
        TypeReging,
        ServiceCreating,
        ServiceCreated,
        ServiceIniting,
        ServiceInited,
        ServiceLastIniting,
        ServiceLastInited,
    }

    /// <summary>
    /// 应用程序初始化
    /// </summary>
    public interface IAppInitializer
    {
        string tag { get; }

        void OnRegistServices(Application target, List<Type> serviceTypes);
    }

    /// <summary>
    /// App
    /// </summary>
    public static class App
    {
        public static Application app { get; private set; }
        public static bool isInited => launchStatus == LaunchStatus.Inited;
        public static LaunchMode launchMode { get; private set; } = LaunchMode.Normal;
        public static LaunchStatus launchStatus { get; private set; } = LaunchStatus.None;
        public static UnityApplication unityApp { get; private set; }
        public static Action onInitialized { get; set; }
        public static Action onDisposed { get; set; }
        public static IAppInitializer initializer { get; private set; }
        public static string tag { get; private set; }

        public static void Initialize(IAppInitializer init = null, LaunchMode mode = LaunchMode.Normal)
        {
            if (null != app)
            {
                throw new RuntimeException("Application 已经创建");
            }

            initializer = init ?? FindAppInitializer();
            launchMode = mode;//启动模式
            tag = initializer?.tag ?? UnityEngine.Application.productName;

            SuperLog.Log($"初始化器类型[{initializer?.GetType().GetTypeName() ?? "无"}]，启动模式[{launchMode}]，标签[{tag}]");

            SuperLog.tag = tag;
            GameObject obj = new GameObject($"[{tag}]App", typeof(UnityApplication));
            GameObject.DontDestroyOnLoad(obj);
            unityApp = obj.GetComponent<UnityApplication>();
            unityApp.onDestroyed += OnDispose;
            unityApp.StartCoroutine(_OnInitialize());
        }

        private static IEnumerator _OnInitialize()
        {
            launchStatus = LaunchStatus.Initing;
            app = new Application();
            yield return InitializeService(app);
            unityApp.app = app;
            launchStatus = LaunchStatus.Inited;
            onInitialized?.Invoke();
            onInitialized = null;
        }

        private static void OnDispose()
        {
            app.Flush();
            launchStatus = LaunchStatus.None;
            onDisposed?.Invoke();
            onDisposed = null;
            app = null;
            unityApp = null;
        }

        private static IAppInitializer FindAppInitializer()
        {
            if (initializer != null)
            {
                return initializer;
            }

            using (var watcher = new TimeWatcher("查找初始化程序"))
            {
                List<Type> types = AssemblyUtility.FindAllAssignable<IAppInitializer>();
                if (types.Count == 0) { return null; }
                if (types.Count > 1) { SuperLog.LogWarning($"{nameof(IAppInitializer)} 子类大于 1 个，只取第一个类型 {types[0].FullName}"); }

                Type type = types[0];
                IAppInitializer result = Activator.CreateInstance(type) as IAppInitializer;

                if (result != null)
                {
                    SuperLog.Log($"使用 {type.FullName} 初始化程序");
                }
                return result;
            }
        }

        private static IEnumerator InitializeService(Application target)
        {
            //初始化基础服务
            using (TimeWatcher watcher = new TimeWatcher("服务初始化"))
            {
                List<Type> serviceTypes = new List<Type>();

                //注册服务
                launchStatus = LaunchStatus.TypeReging;
                watcher.Start();

                initializer?.OnRegistServices(target, serviceTypes);

                watcher.End($"注册服务");
                launchStatus = LaunchStatus.TypeReged;
                //=======================================

                //注册基础服务
                launchStatus = LaunchStatus.ServiceCreating;
                List<IServiceInitialize> initServices = new List<IServiceInitialize>(serviceTypes.Count);
                List<IServiceLateInitialize> lateInitServices = new List<IServiceLateInitialize>(serviceTypes.Count);

                foreach (var serviceType in serviceTypes)
                {
                    watcher.Start();
                    target.Singleton(serviceType);
                    object obj = target.Make(serviceType);
                    if (obj is IServiceInitialize init)
                    {//需要初始化
                        initServices.Add(init);
                    }
                    if (obj is IServiceLateInitialize lateInit)
                    {//需要后初始化
                        lateInitServices.Add(lateInit);
                    }
                    watcher.End($"注册并创建 [{serviceType}] 服务");
                }
                launchStatus = LaunchStatus.ServiceCreated;
                //=========================================

                //初始化服务
                launchStatus = LaunchStatus.ServiceIniting;
                foreach (var service in initServices)
                {
                    watcher.Start();
                    yield return service.OnServiceInitialize();
                    watcher.End($"初始化 [{service.GetType()}] 服务");
                }
                launchStatus = LaunchStatus.ServiceInited;
                //=========================================

                //后初始化服务
                launchStatus = LaunchStatus.ServiceLastIniting;
                foreach (var service in lateInitServices)
                {
                    watcher.Start();
                    yield return service.OnServiceLateInitialize();
                    watcher.End($"后初始化 [{service.GetType()}] 服务");
                }
                launchStatus = LaunchStatus.ServiceLastInited;
                //========================================
            }

            yield break;
        }

        #region static

        public static EventDispatcher evt => _event ?? (_event = app.Make<EventDispatcher>());
        private static EventDispatcher _event;
        public static ObjectDriver obj => _obj ?? (_obj = app.Make<ObjectDriver>());
        private static ObjectDriver _obj;

        #endregion static

        #region Common

        public static bool isMainThread => app.isMainThread;

        public static void Run(Action callback)
        {
            unityApp.Run(callback);
        }

        public static Coroutine StartCoroutine(IEnumerator callback)
        {
            return unityApp.StartCoroutine(callback);
        }

        public static void StopCoroutine(Coroutine target)
        {
            unityApp.StopCoroutine(target);
        }

        #endregion Common

        #region Dispatcher

        public static EventHandler On(int eventType, object target, MethodInfo method, object group = null, bool matchingParams = false)
        {
            return evt.On((int)eventType, target, method, group, matchingParams);
        }

        public static void Trigger(int eventType, params object[] args)
        {
            evt.Trigger(eventType, args);
        }

        public static void Off(object target)
        {
            evt.Off(target);
        }

        #region Extensions

        public static EventHandler On(int eventType, Action callback, object group = null, bool matchingParams = false)
        {
            return evt.On(eventType, callback.Target, callback.Method, group, matchingParams);
        }

        public static EventHandler On<T>(int eventType, Action<T> callback, object group = null, bool matchingParams = false)
        {
            return evt.On(eventType, callback.Target, callback.Method, group, matchingParams);
        }

        public static EventHandler On<T1, T2>(int eventType, Action<T1, T2> callback, object group = null, bool matchingParams = false)
        {
            return evt.On(eventType, callback.Target, callback.Method, group, matchingParams);
        }

        public static EventHandler On<T1, T2, T3>(int eventType, Action<T1, T2, T3> callback, object group = null, bool matchingParams = false)
        {
            return evt.On(eventType, callback.Target, callback.Method, group, matchingParams);
        }

        public static EventHandler On<T1, T2, T3, T4>(int eventType, Action<T1, T2, T3, T4> callback, object group = null, bool matchingParams = false)
        {
            return evt.On(eventType, callback.Target, callback.Method, group, matchingParams);
        }

        #endregion Extensions

        #endregion Dispatcher

        #region Container

        public static Container Alias(string alias, string service)
        {
            return app.Alias(alias, service);
        }

        public static BindData Bind(string service, Func<Container, object[], object> concrete, bool isStatic)
        {
            return app.Bind(service, concrete, isStatic);
        }

        public static BindData Bind(string service, Type concrete, bool isStatic)
        {
            return app.Bind(service, concrete, isStatic);
        }

        public static object Call(object target, MethodInfo methodInfo, params object[] userParams)
        {
            return app.Call(target, methodInfo, userParams);
        }

        public static bool CanMake(string service)
        {
            return app.CanMake(service);
        }

        public static void Flush()
        {
            app.Flush();
        }

        public static BindData GetBind(string service)
        {
            return app.GetBind(service);
        }

        public static bool HasBind(string service)
        {
            return app.HasBind(service);
        }

        public static bool HasInstance(string service)
        {
            return app.HasInstance(service);
        }

        public static object Instance(string service, object instance)
        {
            return app.Instance(service, instance);
        }

        public static bool IsAlias(string name)
        {
            return app.IsAlias(name);
        }

        public static bool IsResolved(string service)
        {
            return app.IsResolved(service);
        }

        public static bool IsStatic(string service)
        {
            return app.IsStatic(service);
        }

        public static object Make(string service, params object[] userParams)
        {
            return app.Make(service, userParams);
        }

        public static Container OnAfterResolving(Action<BindData, object> callback)
        {
            return app.OnAfterResolving(callback);
        }

        public static Container OnRebound(string service, Action<object> callback)
        {
            return app.OnRebound(service, callback);
        }

        public static Container OnRelease(Action<BindData, object> callback)
        {
            return app.OnRelease(callback);
        }

        public static Container OnResolving(Action<BindData, object> callback)
        {
            return app.OnResolving(callback);
        }

        public static bool Release(object mixed)
        {
            return app.Release(mixed);
        }

        public static string Type2Service(Type type)
        {
            return app.Type2Service(type);
        }

        public static void Unbind(string service)
        {
            app.Unbind(service);
        }

        #region Extensions

        public static Container Alias<TAlias, TService>()
        {
            return app.Alias<TAlias, TService>();
        }

        public static Container RemoveAlias<TAlias>()
        {
            return app.RemoveAlias<TAlias>();
        }

        public static Container ResetAlias<TAlias, TService>()
        {
            return app.ResetAlias<TAlias, TService>();
        }

        public static BindData Bind<T>()
        {
            return app.Bind<T>();
        }

        public static BindData Bind<TService, TConcrete>()
        {
            return app.Bind<TService, TConcrete>();
        }

        public static BindData Bind<TService>(Func<Container, object[], object> concrete)
        {
            return app.Bind<TService>(concrete);
        }

        public static BindData Bind<TService>(Func<object[], object> concrete)
        {
            return app.Bind<TService>(concrete);
        }

        public static BindData Bind(string service, Func<Container, object[], object> concrete)
        {
            return app.Bind(service, concrete);
        }

        public static void Call<T>(Action<T> method, params object[] userParams)
        {
            app.Call<T>(method, userParams);
        }

        public static void Call<T1, T2>(Action<T1, T2> method, params object[] userParams)
        {
            app.Call<T1, T2>(method, userParams);
        }

        public static void Call<T1, T2, T3>(Action<T1, T2, T3> method, params object[] userParams)
        {
            app.Call<T1, T2, T3>(method, userParams);
        }

        public static void Call<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, params object[] userParams)
        {
            app.Call<T1, T2, T3, T4>(method, userParams);
        }

        public static object Call(object target, string method, params object[] userParams)
        {
            return app.Call(target, method, userParams);
        }

        public static TService Make<TService>(params object[] userParams)
        {
            return app.Make<TService>(userParams);
        }

        public static bool CanMake<T>()
        {
            return app.CanMake<T>();
        }

        public static Func<TService> Factory<TService>(params object[] userParams)
        {
            return app.Factory<TService>(userParams);
        }

        public static Func<object> Factory(string service, params object[] userParams)
        {
            return app.Factory(service, userParams);
        }

        public static BindData GetBind<T>()
        {
            return app.GetBind<T>();
        }

        public static bool HasBind<T>()
        {
            return app.HasBind<T>();
        }

        public static bool HasInstance<T>()
        {
            return app.HasInstance<T>();
        }

        public static object Instance<TService>(object instance)
        {
            return app.Instance<TService>(instance);
        }

        public static bool IsAlias<T>()
        {
            return app.IsAlias<T>();
        }

        public static bool IsResolved<T>()
        {
            return app.IsResolved<T>();
        }

        public static bool IsStatic<T>()
        {
            return app.IsStatic<T>();
        }

        public static Container OnAfterResolving(Action<object> callback)
        {
            return app.OnAfterResolving(callback);
        }

        public static Container OnAfterResolving<T>(Action<T> callback)
        {
            return app.OnAfterResolving<T>(callback);
        }

        public static Container OnAfterResolving<T>(Action<BindData, T> callback)
        {
            return app.OnAfterResolving<T>(callback);
        }

        public static Container OnRelease(Action<object> callback)
        {
            return app.OnRelease(callback);
        }

        public static Container OnRelease<T>(Action<T> callback)
        {
            return app.OnRelease<T>(callback);
        }

        public static Container OnRelease<T>(Action<BindData, T> callback)
        {
            return app.OnRelease<T>(callback);
        }

        public static Container OnResolving(Action<object> callback)
        {
            return app.OnResolving(callback);
        }

        public static Container OnResolving<T>(Action<T> callback)
        {
            return app.OnResolving<T>(callback);
        }

        public static Container OnResolving<T>(Action<BindData, T> callback)
        {
            return app.OnResolving<T>(callback);
        }

        public static bool Release<TService>()
        {
            return app.Release<TService>();
        }

        public static BindData Singleton(string service, Func<Container, object[], object> concrete)
        {
            return app.Singleton(service, concrete);
        }

        public static BindData Singleton<TService, TConcrete>()
        {
            return app.Singleton<TService, TConcrete>();
        }

        public static BindData Singleton<TService>()
        {
            return app.Singleton<TService>();
        }

        public static BindData Singleton<TService>(Func<Container, object[], object> concrete)
        {
            return app.Singleton<TService>(concrete);
        }

        public static BindData Singleton<TService>(Func<object[], object> concrete)
        {
            return app.Singleton<TService>(concrete);
        }

        public static BindData Singleton<TService>(Func<object> concrete)
        {
            return app.Singleton<TService>(concrete);
        }

        public static string Type2Service<TService>()
        {
            return app.Type2Service<TService>();
        }

        public static void Unbind<TService>()
        {
            app.Unbind<TService>();
        }

        public static void Watch<TService>(Action method)
        {
            app.Watch<TService>(method);
        }

        public static void Watch<TService>(Action<TService> method)
        {
            app.Watch<TService>(method);
        }

        #endregion Extensions

        #endregion Container

        #region Mono

        public static void AttachMono(object target)
        {
            obj.Attach(target);
        }

        public static bool ContainsMono(object target)
        {
            return obj.Contains(target);
        }

        public static bool DetachMono(object target)
        {
            return obj.Detach(target);
        }

        #endregion Mono
    }
}