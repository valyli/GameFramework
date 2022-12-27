//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;

namespace GameFramework.Network
{
    /// <summary>
    /// 网络频道辅助器接口。
    /// </summary>
    public interface IWebSocketNetworkHelper
    {
        /// <summary>
        /// 初始化连接
        /// </summary>
        /// <param name="url">连接url</param>
        /// <param name="origin">连接origin</param>
        void Initialize(string url, string origin);
        
        /// <summary>
        /// 建立连接。
        /// </summary>
        void Connect();
        
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="bytes">发送字符串</param>
        void Send(byte[] bytes);

        /// <summary>
        /// 断开连接
        /// </summary>
        void Close();

        /// <summary>
        /// 连接是否打开
        /// </summary>
        /// <returns>连接已打开</returns>
        bool isWebSocketOpen();

        /// <summary>
        /// 更新数据队列
        /// </summary>
        void DispatchMessageQueue();

        /// <summary>
        /// 网络连接回调
        /// </summary>
        GameFrameworkAction<object> OnOpen
        {
            get;
            set;
        }
        
        /// <summary>
        /// 接收网络数据回调
        /// </summary>
        GameFrameworkAction<object> OnMessage
        {
            get;
            set;
        }
        
        /// <summary>
        /// 断开网络连接回调
        /// </summary>
        GameFrameworkAction<object> OnClose
        {
            get;
            set;
        }
        
        /// <summary>
        /// 错误返回回调
        /// </summary>
        GameFrameworkAction<object> OnError
        {
            get;
            set;
        }

    }
}
