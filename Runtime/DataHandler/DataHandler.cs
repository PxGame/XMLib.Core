/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 10:49:03
 */

using System;

namespace XMLib.DataHandlers
{
    /// <summary>
    /// DataHandler
    /// </summary>
    public class DataHandler
    {
        public static string GetFileName<T>(string childName = null)
        {
            return GetFileName(typeof(T), childName);
        }

        public static string GetFileName(Type type, string childName = null)
        {
            string fileName = string.IsNullOrEmpty(childName)
                ? type.Name
                : $"{childName}.{type.Name}";

            return fileName;
        }
    }
}