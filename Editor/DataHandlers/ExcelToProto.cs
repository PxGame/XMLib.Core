/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 13:53:30
 */

using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace XMLib.DataHandlers
{
    /// <summary>
    /// ExcelToProto
    /// </summary>
    public class ExcelToProto : ExcelToX
    {
        protected override void OnExportSheet(string outDir, string name, Type type, List<object> items, List<Tuple<string, Type>> sheetInfos, List<List<object>> sheetObjs)
        {
            string fullPath = Path.Combine(outDir, $"{name}.bytes");
            using (var steam = new MemoryStream())
            {
                for (int i = 0; i < items.Count; i++)
                {
                    object obj = items[i];
                    ProtoBuf.Serializer.NonGeneric.SerializeWithLengthPrefix(steam, obj, ProtoBuf.PrefixStyle.Base128, 1);
                }

                File.WriteAllBytes(fullPath, steam.ToArray());
            }
        }
    }
}