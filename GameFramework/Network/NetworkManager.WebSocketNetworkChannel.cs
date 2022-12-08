//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;

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
            public WebSocketNetworkChannel(string name, INetworkChannelHelper networkChannelHelper)
                : base(name, networkChannelHelper)
            {
                
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
                m_Socket = new ClientWebSocket();
                if (m_Socket == null)
                {
                    string errorMessage = "Initialize network channel failure.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SocketError, WebSocketError.Success, errorMessage);
                        return;
                    }
            
                    throw new GameFrameworkException(errorMessage);
                }
            
                m_NetworkChannelHelper.PrepareForConnecting();
                ConnectAsync(ipAddress, port, userData);
            }

            protected override bool ProcessSend()
            {
                if (base.ProcessSend())
                {
                    SendAsync();
                    return true;
                }

                return false;
            }

            private async void ConnectAsync(IPAddress ipAddress, int port, object userData)
            {
                try
                {
                    string url = string.Format("ws://{0}:{1}", ipAddress.ToString(), port);
                    Uri uri = new Uri(url);
                    await m_Socket.ConnectAsync(uri, CancellationToken.None);
                    ConnectCallback(new ConnectState(m_Socket, userData));
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        WebSocketException socketException = exception as WebSocketException;
                        NetworkChannelError(this, NetworkErrorCode.ConnectError, socketException != null ? socketException.WebSocketErrorCode : WebSocketError.Success, exception.ToString());
                        return;
                    }

                    throw;
                }
            }

            private void ConnectCallback(ConnectState socketUserData)
            {
                if (m_Socket.State != WebSocketState.Open)
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
                    NetworkChannelConnected(this, socketUserData.UserData);
                }

                m_Active = true;
                ReceiveAsync();
            }

            private async void SendAsync()
            {
                try
                {
                    byte[] bsend = m_SendState.Stream.GetBuffer();
                    await m_Socket.SendAsync(new ArraySegment<byte>(bsend), WebSocketMessageType.Binary, true, CancellationToken.None);
                    SendCallback(m_Socket);
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        WebSocketException socketException = exception as WebSocketException;
                        NetworkChannelError(this, NetworkErrorCode.SendError, socketException != null ? socketException.WebSocketErrorCode : WebSocketError.Success, exception.ToString());
                        return;
                    }

                    throw;
                }
            }

            private void SendCallback(ClientWebSocket socket)
            {
                if (m_Socket.State != WebSocketState.Connecting)
                {
                    m_Active = false;
                    return;
                }
                m_SentPacketCount++;
                m_SendState.Reset();
            }

            private async void ReceiveAsync()
            {
                try
                {
                    byte[] breceive = m_ReceiveState.Stream.GetBuffer();
                    WebSocketReceiveResult wsrResult = await m_Socket.ReceiveAsync(new ArraySegment<byte>(breceive), new CancellationToken());//接受数据
                    ReceiveCallback(m_Socket, wsrResult);
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        WebSocketException socketException = exception as WebSocketException;
                        NetworkChannelError(this, NetworkErrorCode.ReceiveError, socketException != null ? socketException.WebSocketErrorCode : WebSocketError.Success, exception.ToString());
                        return;
                    }

                    throw;
                }
            }

            private void ReceiveCallback(ClientWebSocket socket, WebSocketReceiveResult wsrResult)
            {
                if (m_Socket.State != WebSocketState.Connecting)
                {
                    m_Active = false;
                    return;
                }

                int bytesReceived = wsrResult.Count;
                if (bytesReceived <= 0)
                {
                    Close();
                    return;
                }
                
                m_ReceiveState.Stream.Position = 0L;
                bool processSuccess = false;
                if (m_ReceiveState.PacketHeader != null)
                {
                    processSuccess = ProcessPacket();
                    m_ReceivedPacketCount++;
                }
                else
                {
                    processSuccess = ProcessPacketHeader();
                }

                if (processSuccess)
                {
                    ReceiveAsync();
                    return;
                }
            }
        }
    }
}
