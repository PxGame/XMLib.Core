/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/14 0:25:48
 */

using System.Collections.Generic;

namespace XMLib
{
    /// <summary>
    /// ObjectDriver
    /// </summary>
    public class ObjectDriver
    {
        private readonly List<IMonoFixedUpdate> onFixedUpdates;
        private readonly List<IMonoLateUpdate> onLateUpdates;
        private readonly List<IMonoUpdate> onUpdates;
        private readonly HashSet<object> removings;
        private readonly HashSet<object> addings;
        private readonly HashSet<object> objs;

        public ObjectDriver(int prime = 32)
        {
            onUpdates = new List<IMonoUpdate>(prime);
            onLateUpdates = new List<IMonoLateUpdate>(prime);
            onFixedUpdates = new List<IMonoFixedUpdate>(prime);
            objs = new HashSet<object>();
            addings = new HashSet<object>();
            removings = new HashSet<object>();
        }

        public void Attach(object obj)
        {
            if (null == obj)
            {
                throw new RuntimeException($"添加对象不能为null");
            }

            if (objs.Contains(obj))
            {
                throw new RuntimeException($"对象 [{obj}] 已经添,不能重复添加");
            }

            objs.Add(obj);

            if (!removings.Remove(obj))
            {//没有移除成功，则添加，因为移除成功说明还未反注册，则不需要再次注册，延用即可
                addings.Add(obj);
            }

            if (obj is IMonoStart)
            {
                ((IMonoStart)obj).OnMonoStart();
            }
        }

        public bool Contains(object obj)
        {
            return objs.Contains(obj);
        }

        public bool Detach(object obj)
        {
            if (null == obj || !objs.Contains(obj))
            {
                return false;
            }

            objs.Remove(obj);

            if (!addings.Remove(obj))
            {//没有移除成功，则移除，因为移除成功说明还未注册，则不需要反注册
                removings.Add(obj);
            }

            if (obj is IMonoDestroy)
            {
                ((IMonoDestroy)obj).OnMonoDestroy();
            }

            return true;
        }

        private bool AddToCollection<T>(object obj, List<T> collection)
        {
            if (!(obj is T))
            {
                return false;
            }

            collection.Add((T)obj);

            return true;
        }

        private void RegistUpdate(object obj)
        {
            bool add = AddToCollection(obj, onUpdates);
            add = AddToCollection(obj, onLateUpdates) || add;
            add = AddToCollection(obj, onFixedUpdates) || add;
        }

        private bool RemoveFromCollection<T>(object obj, List<T> collection)
        {
            if (!(obj is T))
            {
                return false;
            }

            return collection.Remove((T)obj);
        }

        private void UnRegistUpdate(object obj)
        {
            bool remove = RemoveFromCollection(obj, onUpdates);
            remove = RemoveFromCollection(obj, onLateUpdates) || remove;
            remove = RemoveFromCollection(obj, onFixedUpdates) || remove;
        }

        private void UpdateObject()
        {
            if (0 < addings.Count)
            {
                foreach (var obj in addings)
                {
                    RegistUpdate(obj);
                }
                addings.Clear();
            }

            if (0 < removings.Count)
            {
                foreach (var obj in removings)
                {
                    UnRegistUpdate(obj);
                }
                removings.Clear();
            }
        }

        #region Mono 事件

        public void RunDestroy()
        {
            foreach (var obj in objs)
            {
                if (obj is IMonoDestroy)
                {
                    ((IMonoDestroy)obj).OnMonoDestroy();
                }
            }

            objs.Clear();
            addings.Clear();
            removings.Clear();
            onUpdates.Clear();
            onLateUpdates.Clear();
            onFixedUpdates.Clear();
        }

        public void RunFixedUpdate()
        {
            foreach (var obj in onFixedUpdates)
            {
                if (removings.Contains(obj))
                {
                    continue;
                }
                obj.OnMonoFixedUpdate();
            }
        }

        public void RunLateUpdate()
        {
            foreach (var obj in onLateUpdates)
            {
                if (removings.Contains(obj))
                {
                    continue;
                }
                obj.OnMonoLateUpdate();
            }
        }

        public void RunUpdate()
        {
            foreach (var obj in onUpdates)
            {
                if (removings.Contains(obj))
                {
                    continue;
                }
                obj.OnMonoUpdate();
            }

            //更新添加删除对象
            UpdateObject();
        }

        #endregion Mono 事件
    }
}