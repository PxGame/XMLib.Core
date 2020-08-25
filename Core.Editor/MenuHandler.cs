/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 1/21/2019 9:42:23 PM
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XMLib
{
    public class MenuHandler
    {
        /// <summary>
        /// 创建Mono脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/Mono 脚本", priority = 0)]
        public static void CreateMonoScript()
        {
            ScriptCreator.CreateMono();
        }

        /// <summary>
        /// 创建脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/Simple 脚本", priority = 0)]
        public static void CreateLibScript()
        {
            ScriptCreator.CreateLib();
        }

        /// <summary>
        /// 创建编辑器脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/编辑器脚本")]
        public static void CreateEditorScript()
        {
            ScriptCreator.CreateEditor();
        }

        /// <summary>
        /// 创建测试脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/测试脚本")]
        public static void CreateLibTestScript()
        {
            ScriptCreator.CreateLibTest();
        }

        /// <summary>
        /// 创建测试运行脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/测试运行脚本")]
        public static void CreateLibTestRunnerScript()
        {
            ScriptCreator.CreateLibTestRunner();
        }

        /// <summary>
        /// 创建Proto脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/Proto脚本")]
        public static void CreateLibProtoScript()
        {
            ScriptCreator.CreateLibProto();
        }

        /// <summary>
        /// 创建Flat脚本
        /// </summary>
        [MenuItem("Assets/Create/XMLib/Flat脚本")]
        public static void CreateLibFlatScript()
        {
            ScriptCreator.CreateLibFlat();
        }
    }
}