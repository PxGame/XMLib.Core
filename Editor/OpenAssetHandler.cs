/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 12/10/2018 2:54:46 PM
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace XMLib
{
    /// <summary>
    /// 打开资源方式
    /// </summary>
    public class OpenAssetHandler
    {
        protected static Dictionary<string, string> _setting = new Dictionary<string, string>();

        static OpenAssetHandler()
        {
            //添加启动方式

            //{后缀,启动程序}
            _setting.Add(".json", @"Code");
            _setting.Add(".shader", @"Code");
            _setting.Add(".cginc", @"Code");
            _setting.Add(".proto", @"Code");
            _setting.Add(".fbs", @"Code");
        }

        [OnOpenAsset]
        public static bool OpenTextAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);

            string ext = Path.GetExtension(assetPath).ToLower();

            string exe;
            if (_setting.TryGetValue(ext, out exe))
            {
                ProcessStartInfo info = new ProcessStartInfo(exe, assetPath);
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.UseShellExecute = true;
                Process.Start(info);
                return true;
            }

            return false;
        }
    }
}