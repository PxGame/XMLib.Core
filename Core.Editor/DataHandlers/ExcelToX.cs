/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 15:17:55
 */

using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace XMLib.DataHandlers
{
    /// <summary>
    /// ExcelToX
    /// </summary>
    public abstract class ExcelToX
    {
        public void Export(string excelDir, string outDir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(excelDir);

            string logMsg = $"导出完成 {excelDir} => {outDir}\n";
            try
            {
                foreach (var fileInfo in dirInfo.GetFiles())
                {
                    if (0 != string.Compare(fileInfo.Extension, ".xlsx", true))
                    {
                        continue;
                    }
                    try
                    {
                        Export(fileInfo, outDir);

                        logMsg += $"{fileInfo.Name}\n";
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(new Exception($"excel转换异常>{fileInfo}", ex));
                    }
                }
            }
            finally
            {
                AssetDatabase.Refresh();
                Debug.Log(logMsg);
            }
        }

        private void Export(FileInfo fileInfo, string outDir)
        {
            using (var excel = new ExcelPackage(fileInfo))
            {
                foreach (var sheet in excel.Workbook.Worksheets)
                {
                    try
                    {
                        Export(sheet, outDir);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"sheet转换异常>sheet name:{sheet.Name}", ex);
                    }
                }
            }
        }

        private void Export(ExcelWorksheet sheet, string outDir)
        {
            string typeFullName = sheet.Cells[1, 1].Text;

            Type type = Type.GetType(typeFullName, false, true);
            if (type == null)
            {
                throw new Exception($"未找到 {typeFullName} 类型");
            }

            int cols = sheet.Dimension.Columns;
            int rows = sheet.Dimension.Rows;

            if (cols < 2)
            {
                throw new Exception($"未发现 {type} 类型可用参数");
            }

            List<Tuple<string, Type>> sheetInfos = new List<Tuple<string, Type>>();
            List<List<object>> sheetObjs = new List<List<object>>();
            List<object> items = new List<object>();

            for (int j = 1; j <= cols; j++)
            {
                string cellTypeName = sheet.Cells[2, j].Text;
                string cellName = sheet.Cells[3, j].Text;
                Type cellType = Type.GetType(cellTypeName, false, true);

                sheetInfos.Add(new Tuple<string, Type>(cellName, cellType));
            }

            for (int i = 4; i <= rows; i++)
            {
                List<object> rowObjs = new List<object>();
                for (int j = 1; j <= cols; j++)
                {
                    var cellInfo = sheetInfos[j - 1];
                    object cellValue = sheet.Cells[i, j].Value;

                    cellValue = cellValue.ConvertAutoTo(cellInfo.Item2);
                    rowObjs.Add(cellValue);
                }
                sheetObjs.Add(rowObjs);

                object item = CreateItem(type, sheetInfos, rowObjs);
                items.Add(item);
            }

            OnExportSheet(outDir, sheet.Name, type, items, sheetInfos, sheetObjs);
        }

        private object CreateItem(Type type, List<Tuple<string, Type>> fieldInfos, List<object> objs)
        {
            object result = Activator.CreateInstance(type);
            FieldInfo[] fields = type.GetFields();
            Stack<FieldInfo> fieldDepth = new Stack<FieldInfo>();

            foreach (var field in fields)
            {
                ImportFieldToItem(ref result, field, fieldDepth, fieldInfos, objs);
            }

            return result;
        }

        private void ImportFieldToItem(ref object target, FieldInfo childField, Stack<FieldInfo> fieldDepth, List<Tuple<string, Type>> fieldInfos, List<object> objs)
        {
            Type fieldType = childField.FieldType;

            if (ExcelExporter.HasChild(fieldType))
            {
                object result = Activator.CreateInstance(fieldType);
                var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                fieldDepth.Push(childField);
                foreach (var field in fields)
                {
                    ImportFieldToItem(ref result, field, fieldDepth, fieldInfos, objs);
                }
                fieldDepth.Pop();
                childField.SetValue(target, result);
            }
            else
            {
                string fieldName = ExcelExporter.PackName(childField, fieldDepth);
                int index = fieldInfos.FindIndex(t => string.Compare(fieldName, t.Item1) == 0);
                if (index >= 0 && index < objs.Count)
                {
                    object value = objs[index];
                    childField.SetValue(target, value);
                }
            }
        }

        protected abstract void OnExportSheet(string outDir, string name, Type type, List<object> items, List<Tuple<string, Type>> sheetInfos, List<List<object>> sheetObjs);
    }
}