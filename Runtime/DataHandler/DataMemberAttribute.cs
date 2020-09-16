/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/11/1 10:54:26
 */

using System;

namespace XMLib.DataHandlers
{
    /// <summary>
    /// DataMemberAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DataMemberAttribute : Attribute
    {
        public string aliasName { get; private set; } = null;
        public DataMemberAttribute(string aliasName)
        {
            this.aliasName = aliasName;
        }
        
        public DataMemberAttribute()
        {
        }
    }
}