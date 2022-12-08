//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Net.WebSockets;

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private sealed class ConnectState
        {
            private readonly ClientWebSocket m_Socket;
            private readonly object m_UserData;

            public ConnectState(ClientWebSocket socket, object userData)
            {
                m_Socket = socket;
                m_UserData = userData;
            }

            public ClientWebSocket Socket
            {
                get
                {
                    return m_Socket;
                }
            }

            public object UserData
            {
                get
                {
                    return m_UserData;
                }
            }
        }
    }
}
