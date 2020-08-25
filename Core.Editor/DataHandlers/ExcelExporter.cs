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
        public void Export(string dir)
        {
            try
            {
                AssemblyUtility.ForeachTypeWithAttr<DataContractAttribute>((t, attr) =>
                {
                    try
                    {
                        Export(t, attr, dir);
                    }
                    catch (Exception ex)
                    {
                        throw new RuntimeException(ex, $"导出 {t} 异常");
                    }
                });
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }

        private void Export(Type t, DataContractAttribute attr, string dir)
        {
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

                var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
                int index = 1;

                foreach (var sheet in excel.Workbook.Worksheets)
                {
                    sheet.Cells[1, 1].Value = t.Assembly.GetName().Name;
                    sheet.Cells[1, 2].Value = t.FullName;
                }

                foreach (var field in fields)
                {
                    if (!field.IsDefined(typeof(DataMemberAttribute), true))
                    {
                        return;
                    }
                    var mAttr = field.GetCustomAttributes(typeof(DataMemberAttribute), true)[0];

                    foreach (var sheet in excel.Workbook.Worksheets)
                    {
                        sheet.Cells[2, index].Value = field.FieldType;
                        sheet.Cells[3, index].Value = field.Name;
                    }

                    index++;
                }

                excel.SaveAs(new FileInfo(filefullPath));
            }

            Debug.Log($"导出 {filefullPath}");
        }
    }
}