/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2021/6/25 2:28:35
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XMLib
{
    /// <summary>
    /// Launcher
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        [SerializeField]
        protected LaunchMode mode = LaunchMode.Normal;

        protected void Awake()
        {
            if (mode != LaunchMode.Normal && App.app != null)
            {
                Destroy(gameObject);
                return;
            }

            App.Initialize(mode);
            Destroy(gameObject);
        }
    }
}