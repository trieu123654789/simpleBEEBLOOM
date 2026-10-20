using System;
using UnityEngine;
using WebSocketSharp;

namespace LetItBee.HoneyPilot
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
        void Start()
        {
            // ConnectionManager.Start calls Connect synchronously. An unreachable
            // LAN address then stalls scene startup on Quest before its first frame.
            // Keep SIMPLE's handshake, but let the pilot own retries and timeouts.
            UpdateConnectionState(ConnectionState.PENDING);
            var socket = GetSocket();
            socket.WaitTime = TimeSpan.FromSeconds(5);
            socket.ConnectAsync();
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
