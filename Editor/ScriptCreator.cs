/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 1/21/2019 9:50:16 PM
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XMLib
{
    public class ScriptCreator
    {
        /// <summary>
        /// 创建脚本
        /// </summary>
        public static void CreateLib()
        {
            CreateFile("XMLib.", XMLib_cs);
        }

        /// <summary>
        /// 创建编辑器脚本
        /// </summary>
        public static void CreateEditor()
        {
            CreateFile("XMLib.", XMLib_Editor_cs);
        }

        /// <summary>
        /// 创建测试脚本
        /// </summary>
        public static void CreateLibTest()
        {
            CreateFile("XMLib.", XMLib_Test_cs);
        }

        /// <summary>
        /// 创建测试运行脚本
        /// </summary>
        public static void CreateLibTestRunner()
        {
            CreateFile("XMLib.", XMLib_Test_TestRunner_cs);
        }

        /// <summary>
        /// 创建Mono脚本
        /// </summary>
        public static void CreateMono()
        {
            CreateFile("XMLib.", XMLib_Mono_cs);
        }

        /// <summary>
        /// 创建Proto脚本
        /// </summary>
        public static void CreateLibProto()
        {
            CreateFile("XMLib", GoogleProto_proto, ".proto");
        }

        /// <summary>
        /// 创建Flat脚本
        /// </summary>
        public static void CreateLibFlat()
        {
            CreateFile("XMLib", GoogleFlat_fbs, ".fbs");
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="templateClass">类模板</param>
        public static void CreateFile(string fileName, string templateClass, string suffixName = ".cs", string iconName = "DefaultAsset Icon", string defaultNS = null)
        {
            string filePath = CreatePath(fileName, suffixName);

            Texture2D icon = EditorGUIUtility.IconContent(iconName).image as Texture2D;

            ScriptCreatorAction sca = ScriptableObject.CreateInstance<ScriptCreatorAction>();
            if (defaultNS != null)
            {
                sca.defaultNS = defaultNS;
            }

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                sca,
                filePath, icon, templateClass
            );
        }

        /// <summary>
        /// 创建文件路径
        /// </summary>
        /// <returns>相对路径</returns>
        private static string CreatePath(string fileName, string suffix = ".cs")
        {
            string dir = "Assets";

            //是否选择路径
            string[] guids = Selection.assetGUIDs;
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                if (File.Exists(path))
                { //文件
                    dir = Path.GetDirectoryName(path);
                }
                else if (Directory.Exists(path))
                { //文件夹
                    dir = path;
                }
            }

            //文件路径优化
            dir = dir.Replace('\\', '/');
            if (!dir.EndsWith("/"))
            { //添加末尾斜线
                dir += '/';
            }

            fileName += suffix;
            string filePath = Path.Combine(dir, fileName);

            return filePath;
        }

        public const string XMLib_Editor_cs = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nusing System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing UnityEditor;\n\nnamespace #NS#\n{\n    /// <summary>\n    /// #SCRIPTNAME#\n    /// </summary>\n    public class #SCRIPTNAME#\n    {\n    }\n}";
        public const string XMLib_cs = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nusing System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\n\nnamespace #NS#\n{\n    /// <summary>\n    /// #SCRIPTNAME#\n    /// </summary>\n    public class #SCRIPTNAME# \n    {\n    }\n}";
        public const string XMLib_Test_cs = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nusing System.Collections;\nusing System.Collections.Generic;\nusing System;\n\nnamespace #NS#.Test\n{\n    /// <summary>\n    /// #SCRIPTNAME#\n    /// </summary>\n    public class #SCRIPTNAME#\n    {\n    }\n}";
        public const string XMLib_Test_TestRunner_cs = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nusing System.Collections;\nusing System.Collections.Generic;\nusing System;\nusing UnityEngine.TestTools;\nusing NUnit.Framework;\nusing System.Diagnostics;\nusing Debug = UnityEngine.Debug;\n\nnamespace #NS#.Test\n{\n    /// <summary>\n    /// #SCRIPTNAME#\n    /// </summary>\n    public class #SCRIPTNAME#\n    {\n        [Test]\n        public void Run()\n        {\n        }\n\n        [UnityTest]\n        public IEnumerator RunSync()\n        {\n            yield break;\n        }\n    }\n}";

        public const string XMLib_Mono_cs = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nusing System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing XMLib;\n\nnamespace #NS#\n{\n    /// <summary>\n    /// #SCRIPTNAME#\n    /// </summary>\n    public class #SCRIPTNAME# : MonoBehaviour \n    {\n    }\n}";
        public const string GoogleProto_proto = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nsyntax = \"proto3\";\n\noption csharp_namespace=\"XMLib\";\n\n//#SCRIPTNAME#\nmessage #SCRIPTNAME#{\n}\n";
        public const string GoogleFlat_fbs = "/*\n * 作者：#AUTHOR#\n * 联系方式：#CONTACT#\n * 文档: #DOC#\n * 创建时间: #CREATEDATE#\n */\n\nnamespace XMLib;\n\ntable #SCRIPTNAME# {\n}\nroot_type #SCRIPTNAME#;\n";
    }
}