/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 10:52:34
 */

using System;

namespace XMLib.DataHandlers
{
    /// <summary>
    /// DataContractAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class DataContractAttribute : Attribute
    {
        public string[] childNames = null;
        public bool isMulti { get { return childNames != null && childNames.Length > 0; } }
        public bool genericAllField { get; set; } = false;
        public bool genericFile { get; set; } = true;

        public DataContractAttribute(params string[] childNames)
        {
            this.childNames = childNames;
        }

        public DataContractAttribute()
        {
        }
    }
}