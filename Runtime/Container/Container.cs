/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/11 16:03:43
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace XMLib
{
    /// <summary>
    /// Container
    /// </summary>
    public class Container
    {
        private readonly List<Action<BindData, object>> afterResloving;
        private readonly Dictionary<string, string> aliases;
        private readonly Dictionary<string, List<string>> aliasesReverse;
        private readonly Dictionary<string, BindData> bindings;
        private readonly Stack<string> buildStack;
        private readonly Dictionary<string, object> instances;
        private readonly Dictionary<object, string> instancesReverse;
        private readonly List<string> instancesTiming;
        private readonly Dictionary<string, List<Action<object>>> rebound;
        private readonly List<Action<BindData, object>> release;
        private readonly HashSet<string> resolved;
        private readonly List<Action<BindData, object>> resolving;

        private const BindingFlags propertyBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public Container(int prime = 32)
        {
            prime = Math.Max(8, prime);

            bindings = new Dictionary<string, BindData>(prime);
            aliases = new Dictionary<string, string>(prime);
            aliasesReverse = new Dictionary<string, List<string>>(prime);
            instances = new Dictionary<string, object>(prime);
            instancesReverse = new Dictionary<object, string>(prime);
            instancesTiming = new List<string>(prime);
            rebound = new Dictionary<string, List<Action<object>>>(prime);
            resolved = new HashSet<string>();
            buildStack = new Stack<string>(16);
            resolving = new List<Action<BindData, object>>();
            afterResloving = new List<Action<BindData, object>>();
            release = new List<Action<BindData, object>>();
        }

        public Container Alias(string alias, string service)
        {
            if (alias == service)
            {
                throw new RuntimeException($"别名 [{alias}] 和服务名 [{service}] 不能相同");
            }

            alias = FormatService(alias);
            service = AliasToService(service);

            if (aliases.ContainsKey(alias))
            {
                throw new RuntimeException($"已存在该别名 [{alias}]");
            }

            if (bindings.ContainsKey(alias))
            {
                throw new RuntimeException($"该别名 [{alias}] 已用于绑定");
            }

            if (!bindings.ContainsKey(service)
                && !instances.ContainsKey(service))
            {
                throw new RuntimeException($"服务 [{service}] 未绑定或注册单例,不能起别名 [{alias}]");
            }

            aliases.Add(alias, service);

            List<string> collection;
            if (!aliasesReverse.TryGetValue(service, out collection))
            {
                collection = new List<string>();
                aliasesReverse.Add(service, collection);
            }
            collection.Add(alias);

            return this;
        }

        public Container RemoveAlias(string alias)
        {
            alias = FormatService(alias);

            if (aliases.TryGetValue(alias, out string service))
            {
                aliasesReverse[service].Remove(alias);
                aliases.Remove(alias);
            }

            return this;
        }

        public Container ResetAlias(string alias, string service)
        {
            RemoveAlias(alias);
            Alias(alias, service);
            return this;
        }

        public BindData Bind(string service, Type concrete, bool isStatic)
        {
            if (IsUnableType(concrete))
            {
                throw new RuntimeException($"类型 {concrete} 不能被绑定");
            }

            service = FormatService(service);
            return Bind(service, WrapperTypeBuilder(service, concrete), isStatic);
        }

        public BindData Bind(string service, Func<Container, object[], object> concrete, bool isStatic)
        {
            service = FormatService(service);
            if (bindings.ContainsKey(service))
            {
                throw new RuntimeException($"服务 [{service}] 已经绑定");
            }

            if (instances.ContainsKey(service))
            {
                throw new RuntimeException($"服务 [{service}] 已在单例中存在");
            }

            if (aliases.ContainsKey(service))
            {
                throw new RuntimeException($"服务名 [{service}] 已在别名中存在");
            }

            BindData bindData = new BindData(this, service, concrete, isStatic);
            bindings.Add(service, bindData);

            if (!IsResolved(service))
            {
                return bindData;
            }

            if (isStatic)
            {
                Make(service);
            }
            else
            {
                TriggerOnRebound(service);
            }

            return bindData;
        }

        public object Call(object target, MethodInfo methodInfo, params object[] userParams)
        {
            if (!methodInfo.IsStatic && null == target)
            {
                throw new RuntimeException($"非静态函数必须指定调用实例>{methodInfo}");
            }

            var parameter = methodInfo.GetParameters();
            var bindData = GetBindFillable(target != null ? Type2Service(target.GetType()) : null);
            userParams = GetDependencies(bindData, parameter, userParams) ?? Array.Empty<object>();
            return methodInfo.Invoke(target, userParams);
        }

        public bool CanMake(string service)
        {
            service = AliasToService(service);
            if (HasBind(service)
                || HasInstance(service))
            {
                return true;
            }

            Type type = SpeculatedServiceType(service);
            return !IsBasicType(type) && !IsUnableType(type);
        }

        public void Flush()
        {
            try
            {
                List<string> releaseServices = new List<string>(instancesTiming);
                releaseServices.Reverse();

                using (TimeWatcher watcher = new TimeWatcher("所有服务释放"))
                {
                    foreach (var service in releaseServices)
                    {
                        watcher.Start();
                        Release(service);
                        watcher.End($"释放 [{service}] 服务");
                    }
                }

                bindings.Clear();
                aliases.Clear();
                aliasesReverse.Clear();
                instances.Clear();
                instancesReverse.Clear();
                instancesTiming.Clear();
                rebound.Clear();
                resolved.Clear();
                buildStack.Clear();
                resolving.Clear();
                afterResloving.Clear();
                release.Clear();
            }
            finally
            {
            }
        }

        public BindData GetBind(string service)
        {
            if (string.IsNullOrEmpty(service))
            {
                return null;
            }

            service = AliasToService(service);
            BindData bindData;
            return bindings.TryGetValue(service, out bindData) ? bindData : null;
        }

        public bool HasBind(string service)
        {
            return null != GetBind(service);
        }

        public bool HasInstance(string service)
        {
            service = AliasToService(service);
            return instances.ContainsKey(service);
        }

        public object Instance(string service, object instance)
        {
            service = AliasToService(service);

            BindData bindData = GetBind(service);
            if (null != bindData)
            {
                if (!bindData.isStatic)
                {
                    throw new RuntimeException($"服务 [{service}] 不是单例绑定");
                }
            }
            else
            {
                bindData = MakeEmptyBindData(service);
            }

            instance = TriggerOnResolving(bindData, instance);
            if (null != instance)
            {
                string realService;
                if (instancesReverse.TryGetValue(instance, out realService)
                    && realService != service)
                {
                    throw new RuntimeException($"该实例已经绑定单例服务 [{realService}]");
                }
            }

            bool isResolved = IsResolved(service);
            Release(service);

            instances.Add(service, instance);

            if (null != instance)
            {
                instancesReverse.Add(instance, service);
            }

            if (!instancesTiming.Contains(service))
            {
                instancesTiming.Add(service);
            }

            if (isResolved)
            {
                TriggerOnRebound(service, instance);
            }

            return instance;
        }

        public bool IsAlias(string name)
        {
            name = FormatService(name);
            return aliases.ContainsKey(name);
        }

        public bool IsResolved(string service)
        {
            service = AliasToService(service);
            return resolved.Contains(service)
                || instances.ContainsKey(service);
        }

        public bool IsStatic(string service)
        {
            var bind = GetBind(service);
            return bind != null && bind.isStatic;
        }

        public object Make(string service, params object[] userParams)
        {
            return Resolve(service, userParams);
        }

        public Container OnAfterResolving(Action<BindData, object> callback)
        {
            AddCallback(callback, afterResloving);
            return this;
        }

        public Container OnRebound(string service, Action<object> callback)
        {
            service = AliasToService(service);
            if (!IsResolved(service) && !CanMake(service))
            {
                throw new RuntimeException($"服务 [{service}] 未绑定或单例化");
            }

            List<Action<object>> callbacks;
            if (!rebound.TryGetValue(service, out callbacks))
            {
                callbacks = new List<Action<object>>();
                rebound.Add(service, callbacks);
            }
            callbacks.Add(callback);

            return this;
        }

        public Container OnRelease(Action<BindData, object> callback)
        {
            AddCallback(callback, release);
            return this;
        }

        public Container OnResolving(Action<BindData, object> callback)
        {
            AddCallback(callback, resolving);
            return this;
        }

        public bool Release(object mixed)
        {
            if (null == mixed)
            {
                return false;
            }

            string service;
            object instance = null;
            if (!(mixed is string))
            {
                service = GetServiceWithInstanceObject(mixed);
            }
            else
            {
                service = AliasToService((string)mixed);
                if (!instances.TryGetValue(service, out instance))
                {
                    service = GetServiceWithInstanceObject(mixed);
                }
            }

            if (null == instance
                && (string.IsNullOrEmpty(service) || !instances.TryGetValue(service, out instance)))
            {
                return false;
            }

            BindData bindData = GetBindFillable(service);
            bindData.TriggerRelease(instance);
            TriggerOnRelease(bindData, instance);

            if (null != instance)
            {
                DisposeInstance(instance);
                instancesReverse.Remove(instance);
            }

            instances.Remove(service);

            if (!HasOnReboundCallbacks(service))
            {
                instancesTiming.Remove(service);
            }

            return true;
        }

        public string Type2Service(Type type)
        {
            return type.ToString();
        }

        public void Unbind(string service)
        {
            service = AliasToService(service);
            BindData bind = GetBind(service);
            bind?.Unbind();
        }

        public object CreateInstance(BindData bindData, Type serviceType, object[] userParams)
        {
            if (IsUnableType(serviceType))
            {
                return null;
            }

            userParams = GetConstructorsInjectParams(bindData, serviceType, userParams);

            try
            {
                object result = CreateInstance(serviceType, userParams);

                result = ResolveFieldInject(serviceType, result);

                return result;
            }
            catch (Exception ex)
            {
                throw new RuntimeException($"服务 [{bindData.service}] 实例创建异常>服务类型:{serviceType}\n{ex}");
            }
        }

        public BindData GetBindFillable(string service)
        {
            BindData bindData;
            return service != null && bindings.TryGetValue(service, out bindData) ? bindData : MakeEmptyBindData(service);
        }

        internal static object Trigger(BindData bindData, object instance, List<Action<BindData, object>> collection)
        {
            if (collection == null)
            {
                return instance;
            }

            foreach (var callback in collection)
            {
                callback(bindData, instance);
            }

            return instance;
        }

        internal void Unbind(BindData bindData)
        {
            Release(bindData.service);
            if (aliasesReverse.TryGetValue(bindData.service, out List<string> serviceList))
            {
                foreach (var alias in serviceList)
                {
                    aliases.Remove(alias);
                }

                aliasesReverse.Remove(bindData.service);
            }

            bindings.Remove(bindData.service);
        }

        protected virtual bool IsUnableType(Type type)
        {
            return type == null || type.IsAbstract || type.IsInterface || type.IsArray || type.IsEnum
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        protected virtual object TriggerOnAfterResolving(BindData bindData, object instance)
        {
            instance = bindData.TriggerAfterResolving(instance);
            return Trigger(bindData, instance, afterResloving);
        }

        protected virtual void TriggerOnRebound(string service, object instance = null)
        {
            var callbacks = GetOnReboundCallbacks(service);
            if (callbacks == null || callbacks.Count <= 0)
            {
                return;
            }

            BindData bindData = GetBind(service);
            instance = instance ?? Make(service);

            int length = callbacks.Count;
            for (var index = 0; index < length; index++)
            {
                callbacks[index](instance);

                if (index + 1 < length && (null == bindData || !bindData.isStatic))
                {
                    instance = Make(service);
                }
            }
        }

        protected virtual void TriggerOnRelease(BindData bindData, object instance)
        {
            Trigger(bindData, instance, release);
        }

        protected virtual object TriggerOnResolving(BindData bindData, object instance)
        {
            instance = bindData.TriggerResolving(instance);
            instance = Trigger(bindData, instance, resolving);
            return TriggerOnAfterResolving(bindData, instance);
        }

        private void AddCallback(Action<BindData, object> callback, List<Action<BindData, object>> collection)
        {
            collection.Add(callback);
        }

        private string AliasToService(string name)
        {
            name = FormatService(name);
            string alias;
            return aliases.TryGetValue(name, out alias) ? alias : name;
        }

        private object Build(BindData bindData, object[] userParams)
        {
            object instance = null != bindData.concrete
                ? bindData.concrete(this, userParams)
                : CreateInstance(bindData, SpeculatedServiceType(bindData.service), userParams);

            return instance;
        }

        private bool CanInject(Type type, object instance)
        {
            return null != instance && type.IsInstanceOfType(instance);
        }

        private bool ChangeType(ref object result, Type type)
        {
            try
            {
                if (null == result || type.IsInstanceOfType(result))
                {
                    return true;
                }

                if (result is IConvertible
                    && typeof(IConvertible).IsAssignableFrom(type))
                {
                    result = Convert.ChangeType(result, type);
                    return true;
                }
            }
            catch (Exception)
            {//忽略异常
            }

            return false;
        }

        private bool CheckCompactInjectUserParams(ParameterInfo baseParam, object[] userParams)
        {
            if (null == userParams || 0 >= userParams.Length)
            {
                return false;
            }

            return typeof(object[]) == baseParam.ParameterType
                || typeof(object) == baseParam.ParameterType;
        }

        private object CreateInstance(Type serviceType, object[] userParams)
        {
            if (null == userParams || 0 >= userParams.Length)
            {
                return Activator.CreateInstance(serviceType);
            }

            return Activator.CreateInstance(serviceType, userParams);
        }

        private void DisposeInstance(object instance)
        {
            if (instance is IDisposable)
            {
                ((IDisposable)instance).Dispose();
            }
        }

        private string FormatService(string service)
        {
            return service.Trim();
        }

        private object GetCompactInjectUserParams(ParameterInfo baseParam, ref object[] userParams)
        {
            if (!CheckCompactInjectUserParams(baseParam, userParams))
            {
                return null;
            }

            try
            {
                if (typeof(object) == baseParam.ParameterType
                    && null != userParams
                    && 1 == userParams.Length)
                {
                    return userParams[0];
                }

                return userParams;
            }
            finally
            {
                userParams = null;
            }
        }

        private object[] GetConstructorsInjectParams(BindData bindData, Type serviceType, object[] userParams)
        {
            ConstructorInfo[] constructors = serviceType.GetConstructors();
            if (constructors.Length <= 0)
            {
                return Array.Empty<object>();
            }
            Exception error = null;
            foreach (var constructor in constructors)
            {
                try
                {
                    return GetDependencies(bindData, constructor.GetParameters(), userParams);
                }
                catch (Exception ex)
                {
                    if (null == error)
                    {
                        error = ex;
                    }
                }
            }
            throw error;
        }

        private object[] GetDependencies(BindData bindData, ParameterInfo[] baseParams, object[] userParams)
        {
            if (0 >= baseParams.Length)
            {
                return Array.Empty<object>();
            }

            int length = baseParams.Length;
            object[] results = new object[length];

            //保护用户参数列表，防止参数列表重用时参数数组被修改
            userParams = (object[])userParams.Clone();

            ParameterInfo baseParam;
            for (int i = 0; i < length; i++)
            {
                baseParam = baseParams[i];
                object result = null;

                result = result ?? GetCompactInjectUserParams(baseParam, ref userParams);

                result = result ?? GetDependenciesFromUserParams(baseParam, ref userParams);

                string needService = null;

                if (null == result)
                {
                    needService = GetParamNeedsService(baseParam);

                    if (baseParam.ParameterType.IsClass
                        || baseParam.ParameterType.IsInterface)
                    {
                        //如果为object[] 或 object 则返回对应的默认参数
                        //防止生成 object[] 或 object 服务产生异常
                        //用于支持任意参数情况，例如调用函数时，函数参数为object[]或object,就可以接收所有传入参数
                        if (typeof(object[]) == baseParam.ParameterType)
                        {
                            result = Array.Empty<object>();
                        }
                        else if (typeof(object) == baseParam.ParameterType)
                        {
                            result = null;
                        }
                        else
                        {
                            result = ResolveClass(bindData, needService, baseParam);
                        }
                    }
                    else
                    {
                        result = ResolvePrimitive(bindData, needService, baseParam);
                    }
                }

                if (!CanInject(baseParam.ParameterType, result))
                {
                    throw new RuntimeException($"参数类型 [{baseParam.ParameterType}] 注入失败>实例类型:{result?.GetType()}");
                }

                results[i] = result;
            }

            return results;
        }

        private object GetDependenciesFromUserParams(ParameterInfo baseParam, ref object[] userParams)
        {
            if (null == userParams)
            {
                return null;
            }

            int length = userParams.Length;
            for (int i = 0; i < length; i++)
            {
                object userParam = userParams[i];

                if (!ChangeType(ref userParam, baseParam.ParameterType))
                {
                    continue;
                }

                ArrayUtility.RemoveAt(ref userParams, i);
                return userParam;
            }

            return null;
        }

        private IList<Action<object>> GetOnReboundCallbacks(string service)
        {
            List<Action<object>> result;
            return rebound.TryGetValue(service, out result) ? result : null;
        }

        private string GetParamNeedsService(ParameterInfo baseParam)
        {
            return Type2Service(baseParam.ParameterType);
        }

        private string GetServiceWithInstanceObject(object instance)
        {
            string service;
            return instancesReverse.TryGetValue(instance, out service) ? service : null;
        }

        private bool HasOnReboundCallbacks(string service)
        {
            IList<Action<object>> result = GetOnReboundCallbacks(service);
            return null != result && 0 < result.Count;
        }

        private bool IsBasicType(Type type)
        {
            return type == null || type.IsPrimitive || type == typeof(string);
        }

        private BindData MakeEmptyBindData(string service)
        {
            return new BindData(this, service, null, false);
        }

        private bool MakeFromContextualService(string service, Type needType, out object instance)
        {
            instance = null;
            if (!CanMake(service))
            {
                return false;
            }

            instance = Make(service);
            return ChangeType(ref instance, needType);
        }

        private object Resolve(string service, object[] userParams)
        {
            object instance = null;
            service = AliasToService(service);
            if (instances.TryGetValue(service, out instance))
            {
                return instance;
            }

            if (buildStack.Contains(service))
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in buildStack)
                {
                    builder.Append($"{item}->");
                }
                throw new RuntimeException($"解决 [{service}] 服务出现循环依赖>{builder}{service}");
            }

            buildStack.Push(service);
            try
            {
                BindData bindData = GetBindFillable(service);

                instance = Build(bindData, userParams);

                instance = bindData.isStatic
                    ? Instance(bindData.service, instance)
                    : TriggerOnResolving(bindData, instance);

                resolved.Add(bindData.service);

                return instance;
            }
            finally
            {
                buildStack.Pop();
            }
        }

        private object ResolveClass(BindData bindData, string service, ParameterInfo baseParam)
        {
            object instance = null;

            if (ResolveFromContextual(bindData, service, baseParam.Name, baseParam.ParameterType, out instance))
            {
                return instance;
            }

            if (baseParam.IsOptional)
            {
                return baseParam.DefaultValue;
            }

            throw new RuntimeException($"处理类类型参数异常>参数名:{baseParam.Name},参数类型:{baseParam.ParameterType}");
        }

        private bool ResolveFromContextual(BindData bindData, string service, string paramName, Type parameterType, out object instance)
        {
            return MakeFromContextualService(service, parameterType, out instance);
        }

        private object ResolvePrimitive(BindData bindData, string needService, ParameterInfo baseParam)
        {
            object instance = null;

            if (ResolveFromContextual(bindData, needService, baseParam.Name, baseParam.ParameterType, out instance))
            {
                return instance;
            }

            if (baseParam.IsOptional)
            {
                return baseParam.DefaultValue;
            }

            if (baseParam.ParameterType.IsGenericType
                && typeof(Nullable<>) == baseParam.ParameterType.GetGenericTypeDefinition())
            {
                return null;
            }

            throw new RuntimeException($"处理基础类型参数异常>参数名:{baseParam.Name},参数类型:{baseParam.Member?.DeclaringType}");
        }

        private Type SpeculatedServiceType(string service)
        {//目前不需要该功能
            return null;
        }

        private Func<Container, object[], object> WrapperTypeBuilder(string service, Type concrete)
        {
            return (container, userParams) => container.CreateInstance(GetBindFillable(service), concrete, userParams);
        }

        private object ResolveFieldInject(Type type, object obj)
        {
            PropertyInfo[] propertyInfos = type.GetProperties(propertyBindingFlags);
            foreach (var info in propertyInfos)
            {
                ResolveFieldInject(info, obj);
            }

            return obj;
        }

        private void ResolveFieldInject(PropertyInfo target, object obj)
        {
            InjectObject injectObject = target.GetCustomAttribute<InjectObject>();
            if (injectObject == null)
            {
                return;
            }

            object instance = null;
            if (!MakeFromContextualService(Type2Service(target.PropertyType), target.PropertyType, out instance))
            {
                throw new RuntimeException($"处理属性注入异常>类型:{target.DeclaringType},注入属性名:{target.Name},属性类型:{target.PropertyType}");
            }
            target.SetValue(obj, instance);
        }
    }
}