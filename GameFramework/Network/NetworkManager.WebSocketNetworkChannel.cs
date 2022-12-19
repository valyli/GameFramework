//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Net;

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        /// <summary>
        /// TCP 网络频道。
        /// </summary>
        private sealed class WebSocketNetworkChannel : NetworkChannelBase
        {
            /// <summary>
            /// 初始化网络频道的新实例。
            /// </summary>
            /// <param name="name">网络频道名称。</param>
            /// <param name="networkChannelHelper">网络频道辅助器。</param>
            /// <param name="webSocketNetworkHelper">网络连接辅助器。</param>
            public WebSocketNetworkChannel(string name, INetworkChannelHelper networkChannelHelper, IWebSocketNetworkHelper webSocketNetworkHelper)
                : base(name, networkChannelHelper, webSocketNetworkHelper)
            {
                webSocketNetworkHelper.OnOpen += OnWebSocketOpen;
                webSocketNetworkHelper.OnMessage += OnWebSocketMessage;
                webSocketNetworkHelper.OnError += OnWebSocketError;
            }
            
            public override ServiceType ServiceType
            {
                get
                {
                    return ServiceType.WebSocket;
                }
            }

            public override void Connect(IPAddress ipAddress, int port, object userData)
            {
                base.Connect(ipAddress, port, userData);
                try
                {
                    string url = string.Format("ws://{0}:{1}/ws", ipAddress.ToString(), port);
                    m_webSocketNetworkHelper.Initialize(url);
                    m_NetworkChannelHelper.PrepareForConnecting();
                    m_webSocketNetworkHelper.Connect();
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.ConnectError, exception.ToString());
                        return;
                    }

                    throw;
                }
            }

            protected override bool ProcessSend()
            {
                if (base.ProcessSend())
                {
                    SendData();
                    return true;
                }

                return false;
            }

            private void SendData()
            {
                try
                {
                    byte[] data = m_SendState.Stream.GetBuffer();
                    int length = Convert.ToInt32(m_SendState.Stream.Length);
                    m_webSocketNetworkHelper.Send(new ArraySegment<byte>(data, 0, length).Array);
                    m_SentPacketCount++;
                    m_SendState.Reset();
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, exception.ToString());
                        return;
                    }

                    throw;
                }
            }
            
            private void OnWebSocketOpen(object userData)
            {
                if (m_webSocketNetworkHelper.isWebSocketOpen())
                {
                    m_Active = false;
                    return;
                }
                
                m_SentPacketCount = 0;
                m_ReceivedPacketCount = 0;

                lock (m_SendPacketPool)
                {
                    m_SendPacketPool.Clear();
                }

                m_ReceivePacketPool.Clear();

                lock (m_HeartBeatState)
                {
                    m_HeartBeatState.Reset(true);
                }

                if (NetworkChannelConnected != null)
                {
                    NetworkChannelConnected(this, m_UserData);
                }

                m_Active = true;
            }

            private void OnWebSocketMessage(object userData)
            {
                byte[] bytes = userData as byte[];
                try
                {
                    m_ReceiveState.Stream.Write(bytes, 0, bytes.Length);
                    int bytesReceived = bytes.Length;
                    if (bytesReceived <= 0)
                    {
                        Close();
                        return;
                    }
                    m_ReceiveState.Stream.Position = 0L;
                    m_ReceiveState.Stream.SetLength(0);
                    int packetLength = ProcessPacketHeader();
                    if (packetLength > 0)
                    {
                        ProcessPacket();
                        m_ReceivedPacketCount++;
                    }
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.ReceiveError, exception.ToString());
                        return;
                    }

                    throw;
                }
            }

            private void OnWebSocketError(object userData)
            {
                string error = userData as string;
                m_Active = false;
                if (NetworkChannelError != null)
                {
                    NetworkChannelError(this, NetworkErrorCode.Unknown, error);
                }
            }
        }
    }
}
