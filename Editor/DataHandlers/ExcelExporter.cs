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
            string fileFullPath = string.Format(fullPathFormater, fileName);

            var oldData = new List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>>();
            if (File.Exists(fileFullPath))
            {
                if (!EditorUtility.DisplayDialog("警告", $"文件已存在，是否继续生成以覆盖原有文件\n{fileFullPath}", "是", "否"))
                {
                    return;
                }

                oldData = ReadExcel(fileFullPath);
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
                    ExportTypeToSheet(field, ref index, excel.Workbook.Worksheets, fieldDepth, oldData);
                }

                excel.SaveAs(new FileInfo(fileFullPath));
            }

            Debug.Log($"导出 {fileFullPath}");
        }

        private static void ExportTypeToSheet(FieldInfo target, ref int index, ExcelWorksheets worksheets, Stack<FieldInfo> fieldDepth, List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>> oldData)
        {
            Type fieldType = target.FieldType;

            if (HasChild(fieldType))
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

                    ExportTypeToSheet(field, ref index, worksheets, fieldDepth, oldData);
                }
                fieldDepth.Pop();
            }
            else
            {
                string fieldName = PackName(target, fieldDepth);
                string fieldTypeName = $"{fieldType.FullName},{fieldType.Assembly.GetName().Name}";
                foreach (var sheet in worksheets)
                {
                    sheet.Cells[2, index].Value = fieldTypeName;
                    sheet.Cells[3, index].Value = fieldName;
                }

                ImportData(worksheets, index, fieldName, fieldType, oldData);

                index++;
            }
        }

        private static void ImportData(ExcelWorksheets sheets, int index, string fieldName, Type fieldType, List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>> oldData)
        {
            foreach (var sheet in sheets)
            {
                var sheetData = oldData.Find(t => string.Compare(t.Item1, sheet.Name) == 0);
                if (sheetData == null)
                {
                    continue;
                }

                var data = sheetData.Item3.Find(t => string.Compare(t.Item2, fieldName) == 0 && t.Item3.ConvertToChecker(fieldType));
                if (data == null)
                {
                    continue;
                }

                for (int i = 0; i < data.Item4.Count; i++)
                {
                    sheet.Cells[i + 4, index].Value = data.Item4[i];
                }
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

        public static bool HasChild(Type fieldType)
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

        public static List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>> ReadExcel(string filePath)
        {
            List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>> result;
            try
            {
                using (var excel = new ExcelPackage(new FileInfo(filePath)))
                {
                    result = ReadExcel(excel);
                }
            }
            catch (Exception ex)
            {
                result = new List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>>();
                Debug.LogWarning($"ReadExcel {filePath} 读取异常\n{ex}");
            }

            return result;
        }

        public static List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>> ReadExcel(ExcelPackage excel)
        {
            var result = new List<Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>>();

            try
            {
                foreach (var sheet in excel.Workbook.Worksheets)
                {
                    var sheetData = ReadSheet(sheet);
                    result.Add(sheetData);
                }
            }
            catch (Exception ex)
            {
                throw new RuntimeException(ex, $"ReadExcel {excel} 读取异常");
            }

            return result;
        }

        private static Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>> ReadSheet(ExcelWorksheet sheet)
        {
            Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>> result = null;
            try
            {
                int colCnt = sheet.Dimension.Columns;
                int rowCnt = sheet.Dimension.Rows;
                string typeFullName = sheet.Cells[1, 1].Text;

                string sheetName = sheet.Name;
                Type sheetType = Type.GetType(typeFullName);

                var sheetData = new List<Tuple<int, string, Type, List<object>>>();
                for (int i = 1; i <= colCnt; i++)
                {
                    string fieldTypeName = sheet.Cells[2, i].Text;
                    string fieldName = sheet.Cells[3, i].Text;
                    Type fieldType = Type.GetType(fieldTypeName, false);
                    if (fieldType == null)
                    {
                        continue;
                    }

                    sheetData.Add(new Tuple<int, string, Type, List<object>>(i, fieldName, fieldType, new List<object>()));
                }

                foreach (var item in sheetData)
                {
                    for (int i = 4; i <= rowCnt; i++)
                    {
                        object obj = sheet.Cells[i, item.Item1].Value.ConvertAutoTo(item.Item3);
                        item.Item4.Add(obj);
                    }
                }

                result = new Tuple<string, Type, List<Tuple<int, string, Type, List<object>>>>(sheetName, sheetType, sheetData);
            }
            catch (Exception ex)
            {
                throw new RuntimeException(ex, $"ReadSheet {sheet} 读取异常");
            }

            return result;
        }
    }
}