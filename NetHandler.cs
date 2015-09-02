using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace debugger
{
    public class DebugSymbolInfo
    {
        public string name;
        public uint address;
        public uint type;
    };

    public class DebugModuleInfo
    {
        public string name;
        public uint entryPoint;
        public DebugSymbolInfo[] symbols;
    };

    public class DebugThreadInfo
    {
        public string name;
        public int curCoreId;
        public uint attribs;
        public uint state;

        public uint cia;
        public uint[] gpr;
        public uint crf;
    }

    public class DebugPauseInfo
    {
        public DebugModuleInfo[] modules;
        public uint userModuleIdx;
        public DebugThreadInfo[] threads;
    }

    public class ConnectedEventArgs : EventArgs
    {

    }

    public class DisconnectedEventArgs : EventArgs
    {

    }

    public class PrelaunchEventArgs : EventArgs
    {
        public DebugPauseInfo PauseInfo;
    }

    public class BpHitEventArgs : EventArgs
    {
        public uint coreId;
        public uint userData;

        public DebugPauseInfo PauseInfo;
    }

    class NetHandler
    {
        public class StateObject
        {
            // Listen socket.
            public Socket listenSocket = null;
            // Client  socket.
            public Socket workSocket = null;
            // Size of inbound packet
            public int recvPacketSize = 0;
            // Total received data
            public int recvPacketCur = 0;
            // Buffer for incoming data
            public byte[] buffer = null;
        }

        public static StateObject currentState = null;

        public static void StartListening()
        {
            Debug.WriteLine("StartListening");

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 11234);

            Socket listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            listener.Bind(localEndPoint);
            listener.Listen(100);

            StateObject state = new StateObject();
            state.listenSocket = listener;
            currentState = state;

            StartAccept(state);
        }

        private static void StartAccept(StateObject state)
        {
            state.listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), state);
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            Debug.WriteLine("AcceptCallback");

            // Get the socket that handles the client request.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.listenSocket;

            Socket newSocket = handler.EndAccept(ar);

            // Create the state object.
            state.workSocket = newSocket;

            // Start reading data
            StartReadPacket(state);

            // Notify a new person connected
            var e = new ConnectedEventArgs();
            ConnectedEvent.Invoke(null, e);
        }

        public static void HandleDisconnected(StateObject state)
        {
            // Notify that the user disconnected
            var e = new DisconnectedEventArgs();
            DisconnectedEvent.Invoke(null, e);

            // Clear the current work Socket
            currentState.workSocket = null;

            // Start listening for a new connection
            StartAccept(state);
        }

        public static void StartReadPacket(StateObject state)
        {
            Debug.WriteLine("StartReadPacket");

            state.buffer = new byte[6];
            state.recvPacketCur = 0;
            state.recvPacketSize = 0;

            StartReadHeader(state);
        }

        public static void StartReadHeader(StateObject state)
        {
            Debug.WriteLine("StartReadHeader");

            state.workSocket.BeginReceive(
                state.buffer, state.recvPacketCur, 6 - state.recvPacketCur, 0,
                new AsyncCallback(ReadHeaderCallback), state);
        }

        public static void ReadHeaderCallback(IAsyncResult ar)
        {
            Debug.WriteLine("ReadHeaderCallback");

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = 0;
            try {
                bytesRead = handler.EndReceive(ar);
            } catch(SocketException) { }

            if (bytesRead > 0)
            {
                state.recvPacketCur += bytesRead;
                if (state.recvPacketCur < 6)
                {
                    // Need more data for header
                    StartReadHeader(state);
                }
                else
                {
                    // We have all the header data...
                    state.recvPacketSize = BitConverter.ToUInt16(state.buffer, 0);

                    if (state.recvPacketSize > state.recvPacketCur)
                    {
                        byte[] oldBuffer = state.buffer;
                        state.buffer = new byte[state.recvPacketSize];
                        System.Array.Copy(oldBuffer, state.buffer, 6);

                        StartReadPayload(state);
                    }
                    else
                    {
                        HandlePacket(state.buffer);
                        StartReadPacket(state);
                    }
                }
            } else
            {
                // Disconnected...
                HandleDisconnected(state);
            }
        }

        public static void StartReadPayload(StateObject state)
        {
            Debug.WriteLine("StartReadPayload");

            state.workSocket.BeginReceive(
                state.buffer, state.recvPacketCur, state.recvPacketSize - state.recvPacketCur, 0,
                new AsyncCallback(ReadPayloadCallback), state);
        }

        public static void ReadPayloadCallback(IAsyncResult ar)
        {
            Debug.WriteLine("ReadPayloadCallback");

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (SocketException) { }

            if (bytesRead > 0)
            {
                state.recvPacketCur += bytesRead;
                if (state.recvPacketCur < state.recvPacketSize)
                {
                    // Need more data for header
                    StartReadPayload(state);
                }
                else
                {
                    HandlePacket(state.buffer);
                    StartReadPacket(state);
                }
            } else
            {
                // Disconnected...
                HandleDisconnected(state);
            }
        }

        const ushort PacketCmdPreLaunch = 1;
        const ushort PacketCmdBpHit = 2;
        const ushort PacketCmdPause = 3;
        const ushort PacketCmdResume = 4;
        const ushort PacketCmdAddBreakpoint = 5;
        const ushort PacketCmdRemoveBreakpoint = 6;

        private static string readString(BinaryReader rdr)
        {
            ulong strLen = rdr.ReadUInt64();
            return new string(rdr.ReadChars((int)strLen));
        }

        private static DebugSymbolInfo readDebugSymbolInfo(BinaryReader rdr)
        {
            var info = new DebugSymbolInfo();

            info.name = readString(rdr);
            info.address = rdr.ReadUInt32();
            info.type = rdr.ReadUInt32();

            return info;
        }

        private static DebugModuleInfo readDebugModuleInfo(BinaryReader rdr)
        {
            var info = new DebugModuleInfo();

            info.name = readString(rdr);
            info.entryPoint = rdr.ReadUInt32();

            ulong numSymbols = rdr.ReadUInt64();
            info.symbols = new DebugSymbolInfo[numSymbols];
            for (ulong i = 0; i < numSymbols; ++i)
            {
                info.symbols[i] = readDebugSymbolInfo(rdr);
            }

            return info;
        }

        private static DebugThreadInfo readDebugThreadInfo(BinaryReader rdr)
        {
            var info = new DebugThreadInfo();

            info.name = readString(rdr);
            info.curCoreId = rdr.ReadInt32();
            info.attribs = rdr.ReadUInt32();
            info.state = rdr.ReadUInt32();

            info.cia = rdr.ReadUInt32();

            info.gpr = new uint[32];
            for (int i = 0; i < 32; ++i)
            {
                info.gpr[i] = rdr.ReadUInt32();
            }
            info.crf = rdr.ReadUInt32();

            return info;
        }
        
        private static DebugPauseInfo readDebugPauseInfo(BinaryReader rdr)
        {
            var info = new DebugPauseInfo();

            ulong numModules = rdr.ReadUInt64();
            info.modules = new DebugModuleInfo[numModules];
            for (ulong i = 0; i < numModules; ++i)
            {
                info.modules[i] = readDebugModuleInfo(rdr);
            }

            info.userModuleIdx = rdr.ReadUInt32();

            ulong numThreads = rdr.ReadUInt64();
            info.threads = new DebugThreadInfo[numThreads];
            for (ulong i = 0; i < numThreads; ++i)
            {
                info.threads[i] = readDebugThreadInfo(rdr);
            }

            return info;
        }

        public static void HandlePacket(byte[] data)
        {
            Debug.WriteLine("HandlePacket");

            BinaryReader rdr = new BinaryReader(new MemoryStream(data));

            var size = rdr.ReadUInt32();
            var cmd = rdr.ReadUInt16();
            var reqId = rdr.ReadUInt16();

            if (cmd == PacketCmdPreLaunch)
            {
                var e = new PrelaunchEventArgs();
                e.PauseInfo = readDebugPauseInfo(rdr);
                PrelaunchEvent.Invoke(null, e);
            }
            else if (cmd == PacketCmdBpHit)
            {
                var e = new BpHitEventArgs();
                e.coreId = rdr.ReadUInt32();
                e.userData = rdr.ReadUInt32();
                e.PauseInfo = readDebugPauseInfo(rdr);
                BpHitEvent.Invoke(null, e);
            }
            else
            {
                Debug.WriteLine(data);
            }
        }

        private static void Send(Socket handler, byte[] data)
        {
            // Begin sending the data to the remote device.
            handler.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public delegate void PacketBuilder(BinaryWriter wrt);
        public static void SendPacket(ushort cmd, ushort reqId, PacketBuilder fn)
        {
            var ms = new MemoryStream();
            var wrt = new BinaryWriter(ms);

            wrt.Write((uint)ms.Length);
            wrt.Write(cmd);
            wrt.Write(reqId);

            if (fn != null)
            {
                fn(wrt);
            }

            wrt.Seek(0, SeekOrigin.Begin);
            wrt.Write((uint)ms.Length);

            Send(currentState.workSocket, ms.ToArray());
        }

        public static void SendAddBreakpoint(uint address, uint userData)
        {
            SendPacket(PacketCmdAddBreakpoint, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(address);
                wrt.Write(userData);
            });
        }

        public static void SendPause()
        {
            SendPacket(PacketCmdPause, 0, null);
        }

        public static void SendResume()
        {
            SendPacket(PacketCmdResume, 0, null);
        }

        public static event EventHandler<ConnectedEventArgs> ConnectedEvent;
        public static event EventHandler<DisconnectedEventArgs> DisconnectedEvent;
        public static event EventHandler<PrelaunchEventArgs> PrelaunchEvent;
        public static event EventHandler<BpHitEventArgs> BpHitEvent;
    }
}
