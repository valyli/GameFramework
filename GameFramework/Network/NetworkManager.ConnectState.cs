//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private sealed class ConnectState
        {
            private readonly object m_UserData;

            public ConnectState(object userData)
            {
                m_UserData = userData;
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
