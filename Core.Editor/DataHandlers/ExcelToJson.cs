/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 15:22:22
 */

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XMLib.DataHandlers
{
    /// <summary>
    /// ExcelToJson
    /// </summary>
    public class ExcelToJson : ExcelToX
    {
        protected override void OnExportSheet(string outDir, string name, Type type, List<object> items, List<Tuple<string, Type>> sheetInfos, List<List<object>> sheetObjs)
        {
            string fullPath = Path.Combine(outDir, $"{name}.json");
            string json = JsonConvert.SerializeObject(items);
            File.WriteAllText(fullPath, json);
        }
    }
}