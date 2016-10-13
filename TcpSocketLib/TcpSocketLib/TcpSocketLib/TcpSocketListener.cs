using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace TcpSocketLib
{
    public class TcpSocketListener
    {

        public delegate void PacketReceivedEventHandler(TcpSocket sender, PacketReceivedArgs PacketReceivedArgs);
        public delegate void FloodDetectedEventHandler(TcpSocket sender);
        public delegate void ClientConnectedEventHandler(TcpSocket sender);
        public delegate void ClientDisconnectedEventHandler(TcpSocket sender);
        public delegate void ReceiveProgressChangedHandler(TcpSocket sender, int Received, int BytesToReceive);

        public event ReceiveProgressChangedHandler ReceiveProgressChanged;
        public event PacketReceivedEventHandler PacketReceived;
        public event ClientConnectedEventHandler ClientConnected;
        public event ClientDisconnectedEventHandler ClientDisconnected;
        public event FloodDetectedEventHandler FloodDetected;

        public List<TcpSocket> ConnectedClients {
            get {
                lock (_syncLock) { return _connectedClients; }
            }
            private set { }
        }

        public bool Running { get; private set; }
        public int Port { get; private set; }
        public int MaxConnectionQueue { get; private set; }
        public int MaxPacketSize { get; private set; }

        Socket _listener;
        List<TcpSocket> _connectedClients;
        object _syncLock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Port">Port to listen on</param>
        /// <param name="MaxPacketSize">Determines the max allowed packet size to be received, unless specifically set by the client object</param>
        public TcpSocketListener(int Port, int MaxPacketSize = 85000) {
            this.MaxPacketSize = MaxPacketSize;
            this.Port = Port;
            this.Running = false;
        }

        public void Start(int MaxConnectionQueue = 25) {
            if (Running) {
                throw new InvalidOperationException("Listener is already running");
            } else {
                this.ConnectedClients = new List<TcpSocket>();
                this._connectedClients = new List<TcpSocket>();
                this.MaxConnectionQueue = MaxConnectionQueue;
                this._listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this._listener.Bind(new IPEndPoint(0, Port));
                this._listener.Listen(MaxConnectionQueue);
                this.Running = true;
                this._listener.BeginAccept(AcceptCallBack, null);
            }
        }

        public void Stop() {
            if (Running) {
                _listener.Close();
                _listener = null;
                _connectedClients = null;
                ConnectedClients = null;
                MaxConnectionQueue = 0;
                Running = false;
            } else {
                throw new InvalidOperationException("Listener isn't running");
            }
        }

        private void AcceptCallBack(IAsyncResult iar) {
            try {
                Socket accepted = _listener.EndAccept(iar);
                TcpSocket tcpSocket = new TcpSocket(accepted, MaxPacketSize);

                tcpSocket.ClientConnected += TcpSocket_ClientConnected;
                tcpSocket.ClientDisconnected += TcpSocket_ClientDisconnected;
                tcpSocket.PacketReceived += TcpSocket_PacketReceived;
                tcpSocket.ReceiveProgressChanged += TcpSocket_ReceiveProgressChanged;
                tcpSocket.FloodDetected += TcpSocket_FloodDetected;

                tcpSocket.Start();
                this._listener.BeginAccept(AcceptCallBack, null);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + " \n\n [" + ex.StackTrace + "]");
            }
        }

        private void TcpSocket_FloodDetected(TcpSocket sender) {
            FloodDetected?.Invoke(sender);
        }

        private void TcpSocket_ReceiveProgressChanged(TcpSocket sender, int Received, int BytesToReceive) {
            ReceiveProgressChanged?.Invoke(sender, Received, BytesToReceive);
        }

        private void TcpSocket_PacketReceived(TcpSocket sender, PacketReceivedArgs PacketReceivedArgs) {
            PacketReceived?.Invoke(sender, PacketReceivedArgs);
        }

        private void TcpSocket_ClientDisconnected(TcpSocket sender) {
            lock (_syncLock) {
                _connectedClients.Remove(sender);
            }
            ClientDisconnected?.Invoke(sender);
        }

        private void TcpSocket_ClientConnected(TcpSocket sender) {
            lock (_syncLock) {
                _connectedClients.Add(sender);
            }
            ClientConnected?.Invoke(sender);
        }
    }
}
