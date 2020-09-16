/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 1/21/2019 9:51:26 PM
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace XMLib
{
    /// <summary>
    /// 脚本创建事件
    /// </summary>
    public class ScriptCreatorAction : EndNameEditAction
    {
        public string defaultNS { get; set; } = "XMLib";

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            //创建资源
            UnityEngine.Object obj = CreateFile(pathName, resourceFile);
            //高亮显示该资源
            ProjectWindowUtil.ShowCreatedAsset(obj);
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="resourceFile"></param>
        /// <returns>资源对象</returns>
        private UnityEngine.Object CreateFile(string filePath, string resourceFile)
        {
            string scriptStr = resourceFile;

            string str = Path.GetFileNameWithoutExtension(filePath).Replace(" ", "");

            string className = "";
            string ns = "";
            int matchIndex = str.LastIndexOf('.');
            if (matchIndex >= 0)
            {
                ns = str.Substring(0, matchIndex);
                className = str.Substring(matchIndex + 1);
                filePath = filePath.Replace(str, className);
            }
            else
            {
                ns = defaultNS;
                className = str;
            }

            string folderPath = Path.GetDirectoryName(filePath);
            int index = folderPath.LastIndexOfAny(new char[] { '/', '\\' }) + 1;
            string folderName = folderPath.Substring(index);

            ns = ns.Replace("{d}", folderName);

            //更新信息
            scriptStr = scriptStr.Replace("#SCRIPTNAME#", className);
            scriptStr = scriptStr.Replace("#AUTHOR#", "Peter Xiang");
            scriptStr = scriptStr.Replace("#CONTACT#", "565067150@qq.com");
            scriptStr = scriptStr.Replace("#DOC#", "https://github.com/PxGame");
            scriptStr = scriptStr.Replace("#CREATEDATE#", DateTime.Now.ToString());
            scriptStr = scriptStr.Replace("#FOLDERNAME#", folderName);
            scriptStr = scriptStr.Replace("#DEVICENAME#", SystemInfo.deviceName);
            scriptStr = scriptStr.Replace("#NS#", ns);

            File.WriteAllText(filePath, scriptStr);

            //刷新本地资源
            AssetDatabase.ImportAsset(filePath);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath(filePath, typeof(UnityEngine.Object));
        }
    }
}