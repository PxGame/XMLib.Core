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
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(new RuntimeException(ex, $"excel转换异常>{fileInfo}"));
                    }
                }
            }
            finally
            {
                AssetDatabase.Refresh();
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
                        throw new RuntimeException(ex, $"sheet转换异常>sheet name:{sheet.Name}");
                    }
                }
            }
        }

        private void Export(ExcelWorksheet sheet, string outDir)
        {
            string assemblyName = sheet.Cells[1, 1].Text;
            string typeFullName = sheet.Cells[1, 2].Text;

            string fullName = $"{typeFullName},{assemblyName}";
            Type type = Type.GetType(fullName, false, true);
            if (type == null)
            {
                throw new RuntimeException($"未找到 {typeFullName} 类型");
            }

            Dictionary<int, FieldInfo> col2field = new Dictionary<int, FieldInfo>();

            int cols = sheet.Dimension.Columns;
            int rows = sheet.Dimension.Rows;

            if (cols < 3)
            {
                throw new RuntimeException($"未发现 {type} 类型可用参数");
            }

            for (int i = 1; i <= cols; i++)
            {
                string fieldName = sheet.Cells[3, i].Text;
                FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo == null)
                {
                    throw new RuntimeException($"未在 {type} 类型中找到 {fieldName} 变量");
                }

                col2field.Add(i, fieldInfo);
            }

            List<object> objs = new List<object>();

            for (int i = 4; i <= rows; i++)
            {
                object obj = Activator.CreateInstance(type);

                for (int j = 1; j <= cols; j++)
                {
                    FieldInfo fieldInfo = col2field[j];
                    object fieldObj = sheet.Cells[i, j].Value;

                    if (!ChangeType(ref fieldObj, fieldInfo.FieldType))
                    {//给默认值
                        Debug.LogWarning($"{sheet}[{i},{j}] 转换到 {fieldInfo.FieldType} 失败, 使用默认值");
                        fieldObj = fieldInfo.FieldType.IsValueType ? Activator.CreateInstance(fieldInfo.FieldType) : null;
                    }

                    fieldInfo.SetValue(obj, fieldObj);
                }

                objs.Add(obj);
            }

            string fullPath = OnExportSheet(outDir, sheet.Name, type, objs);
        }

        private bool ChangeType(ref object result, Type type)
        {
            try
            {
                if (null == result || type.IsInstanceOfType(result))
                {
                    return true;
                }

                if (type.IsEnum)
                {
                    result = Enum.Parse(type, (string)result);
                    if (null != result)
                    {
                        return true;
                    }
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

        protected abstract string OnExportSheet(string outDir, string name, Type type, List<object> objs);
    }
}