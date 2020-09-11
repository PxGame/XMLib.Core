/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 11:10:32
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
    /// ExcelExportor
    /// </summary>
    public class ExcelExporter
    {
        public static void Export(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var typeCollection = TypeCache.GetTypesWithAttribute<DataContractAttribute>();
                foreach (var type in typeCollection)
                {
                    try
                    {
                        Export(type, dir);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"导出 {type} 异常", ex);
                    }
                }
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }

        private static void Export(Type t, string dir)
        {
            DataContractAttribute attr = t.GetCustomAttribute<DataContractAttribute>();
            if (!attr.genericFile)
            {
                return;
            }

            string fullPathFormater = Path.Combine(dir, "{0}.xlsx");
            string fileName = t.Name;
            string filefullPath = string.Format(fullPathFormater, fileName);

            if (File.Exists(filefullPath))
            {
                if (!EditorUtility.DisplayDialog("警告", $"文件已存在，是否继续生成以覆盖原有文件\n{filefullPath}", "是", "否"))
                {
                    return;
                }
            }

            using (var excel = new ExcelPackage())
            {
                if (attr.isMulti)
                {
                    foreach (var childName in attr.childNames)
                    {
                        excel.Workbook.Worksheets.Add(DataHandler.GetFileName(t, childName));
                    }
                }
                else
                {
                    excel.Workbook.Worksheets.Add(t.Name);
                }

                foreach (var sheet in excel.Workbook.Worksheets)
                {
                    sheet.Cells[1, 1].Value = $"{t.FullName},{t.Assembly.GetName().Name}";
                }

                int index = 1;
                Stack<FieldInfo> fieldDepth = new Stack<FieldInfo>();
                FieldInfo[] fields = t.GetFields();

                foreach (var field in fields)
                {
                    ExportTypeToSheet(field, ref index, excel.Workbook.Worksheets, fieldDepth);
                }

                excel.SaveAs(new FileInfo(filefullPath));
            }

            Debug.Log($"导出 {filefullPath}");
        }

        private static void ExportTypeToSheet(FieldInfo target, ref int index, ExcelWorksheets worksheets, Stack<FieldInfo> fieldDepth)
        {
            Type fieldType = target.FieldType;

            if (CheckType(fieldType))
            {
                DataContractAttribute attr = fieldType.GetCustomAttribute<DataContractAttribute>();
                if (attr == null)
                {
                    return;
                }

                var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                fieldDepth.Push(target);
                foreach (var field in fields)
                {
                    if (!attr.genericAllField && !field.IsDefined(typeof(DataMemberAttribute), true))
                    {
                        return;
                    }

                    ExportTypeToSheet(field, ref index, worksheets, fieldDepth);
                }
                fieldDepth.Pop();
            }
            else
            {
                string fieldName = PackName(target, fieldDepth);
                foreach (var sheet in worksheets)
                {
                    sheet.Cells[2, index].Value = $"{fieldType.FullName},{fieldType.Assembly.GetName().Name}";
                    sheet.Cells[3, index].Value = fieldName;
                }
                index++;
            }
        }

        public static string PackName(FieldInfo target, Stack<FieldInfo> fieldDepth)
        {
            string result = GetFieldName(target);
            foreach (var field in fieldDepth)
            {
                result += "." + GetFieldName(field);
            }
            return result;
        }

        public static string GetFieldName(FieldInfo info)
        {
            var attr = info.GetCustomAttribute<DataMemberAttribute>();
            string result = (attr != null && !string.IsNullOrEmpty(attr.aliasName)) ? attr.aliasName : info.Name;
            return result;
        }

        public static bool CheckType(Type fieldType)
        {
            if (fieldType == typeof(string)
            || fieldType.IsEnum)
            {
                return false;
            }

            if ((fieldType.IsClass || fieldType.IsValueType) && !(fieldType.IsPrimitive || fieldType.IsGenericType || fieldType.IsInterface))
            {
                return true;
            }
            return false;
        }
    }
}