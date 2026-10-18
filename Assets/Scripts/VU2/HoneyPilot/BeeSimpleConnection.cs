using System;
using UnityEngine;
using WebSocketSharp;

namespace BeeBloom.HoneyPilot
{
    // Uses SIMPLE's existing protocol/handshake without changing shared defaults.
    public sealed class BeeSimpleConnection : ConnectionManager
    {
        bool shuttingDown;
        volatile bool disconnected;
        public bool TransportReady => !disconnected && GetSocket()?.ReadyState == WebSocketState.Open;
        public void Configure(string endpoint)
        {
            var uri = new Uri(BeeConnectionProfile.ValidateEndpoint(endpoint));
            DesktopMode = false;
            fixedProperties = true;
            UseMiddlewareDM = true;
            DefaultIP = uri.Host;
            DefaultPort = uri.Port.ToString();
        }
        protected override void HandleConnectionClosed(object sender, CloseEventArgs e)
        {
            // The base manager reconnects immediately on close. Reconnect belongs
            // to the pilot pump here, and must never run while destroying a rig.
            disconnected = true;
        }
        public void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            GetSocket()?.CloseAsync();
            if (Instance == this) Instance = null;
        }
        void OnDestroy() { Shutdown(); }
    }
}
