/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/9/12 23:57:59
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XMLib
{
    /// <summary>
    /// UnityApplication
    /// </summary>
    public class UnityApplication : MonoBehaviour
    {
        public Application app;
        public Action onDestroyed;

        #region Mono

        protected void Update()
        {
            app?.RunUpdate();
        }

        protected void LateUpdate()
        {
            app?.RunLateUpdate();
        }

        protected void FixedUpdate()
        {
            app?.RunFixedUpdate();
        }

        protected void OnDestroy()
        {
            app?.RunDestroy();

            onDestroyed?.Invoke();
        }

        #endregion Mono
    }
}