/*
 * 作者：Peter Xiang
 * 联系方式：565067150@qq.com
 * 文档: https://github.com/PxGame
 * 创建时间: 2019/10/14 14:01:33
 */

using System.Collections;

namespace XMLib
{
    /// <summary>
    /// IServiceInitialize
    /// </summary>
    public interface IServiceInitialize
    {
        IEnumerator OnServiceInitialize();
    }

    /// <summary>
    /// IServiceInitialize
    /// </summary>
    public interface IServiceLateInitialize
    {
        IEnumerator OnServiceLateInitialize();
    }
}