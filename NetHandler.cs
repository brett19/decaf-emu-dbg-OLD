using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace debugger
{
    public class DebugSymbolInfo
    {
        public uint moduleIdx;
        public string name;
        public uint address;
        public uint type;
    };

    public class DebugModuleInfo
    {
        public string name;
        public uint entryPoint;
    };

    public class DebugThreadInfo
    {
        public string name;
        public uint id;

        public int curCoreId;
        public uint attribs;
        public uint state;

        public uint entryPoint;
        public uint stackStart;
        public uint stackEnd;

        public uint cia;
        public uint[] gpr;
        public uint lr;
        public uint ctr;
        public uint crf;

        public DebugThreadInfo Clone()
        {
            DebugThreadInfo n = new DebugThreadInfo();

            n.name = name;
            n.id = id;

            n.curCoreId = curCoreId;
            n.attribs = attribs;
            n.state = state;

            n.entryPoint = entryPoint;
            n.stackStart = stackStart;
            n.stackEnd = stackEnd;

            n.cia = cia;
            n.gpr = new uint[32];
            for (var i = 0; i < 32; ++i)
            {
                n.gpr[i] = gpr[i];
            }
            n.lr = lr;
            n.ctr = ctr;
            n.crf = crf;

            return n;
        }

        public void ApplyTraceData(StateField type, byte[] data)
        {
            if (type >= StateField.GPR0 && type <= StateField.GPR31)
            {
                gpr[type - StateField.GPR] = BitConverter.ToUInt32(data, 0);
            }
            else if (type >= StateField.FPR0 && type <= StateField.FPR31)
            {
                // TODO: Implement me
            } else if (type >= StateField.GQR0 && type <= StateField.GQR7)
            {
                // TODO: Implement me
            } else if (type == StateField.CR)
            {
                crf = BitConverter.ToUInt32(data, 0);
            } else if (type == StateField.XER)
            {
                // TODO: Implement me
            }
            else if (type == StateField.LR)
            {
                lr = BitConverter.ToUInt32(data, 0);
            } else if (type == StateField.CTR)
            {
                ctr = BitConverter.ToUInt32(data, 0);
            } else if (type == StateField.FPSCR)
            {
                // TODO: Implement me
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public class DebugPauseInfo
    {
        public DebugModuleInfo[] modules;
        public uint userModuleIdx;
        public DebugThreadInfo[] threads;
        public DebugSymbolInfo[] symbols;
    }

    public enum StateField : uint
    {
        Invalid = 0,
        GPR,
        GPR0 = GPR,
        GPR31 = GPR + 31,
        FPR,
        FPR0 = FPR,
        FPR31 = FPR + 31,
        GQR,
        GQR0 = GQR,
        GQR7 = GQR + 7,
        CR,
        XER,
        LR,
        CTR,
        FPSCR,
        ReserveAddress,
        Max,
    }

    public struct DebugTraceEntryField
    {
        public StateField type;
        public byte[] data;
    }

    public class DebugTraceEntry
    {
        public uint cia;
        public DebugTraceEntryField[] fields;
    }

    public class ConnectedEventArgs : EventArgs
    {

    }

    public class DisconnectedEventArgs : EventArgs
    {

    }

    public class BpHitEventArgs : EventArgs
    {
        public uint coreId;

        public DebugPauseInfo PauseInfo;
    }

    public class CoreSteppedEventArgs : EventArgs
    {
        public uint coreId;

        public DebugPauseInfo PauseInfo;
    }

    public class PausedEventArgs : EventArgs
    {
        public DebugPauseInfo PauseInfo;
    }

    public class GetTraceEventArgs : EventArgs
    {
        public uint ThreadId;

        public DebugTraceEntry[] Info;
    }

    class NetHandler
    {
        public enum PacketCmd : ushort
        {
            BpHit = 2,
            Pause = 3,
            Resume = 4,
            AddBreakpoint = 5,
            RemoveBreakpoint = 6,
            ReadMem = 7,
            ReadMemRes = 8,
            Disasm = 9,
            DisasmRes = 10,
            StepCore = 11,
            CoreStepped = 12,
            Paused = 13,
            GetTrace = 14,
            GetTraceRes = 15,
            StepCoreOver = 16,
        }

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
        public static bool currentlyPaused = false;

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

        private static string readString(BinaryReader rdr)
        {
            ulong strLen = rdr.ReadUInt64();
            return new string(rdr.ReadChars((int)strLen));
        }

        private static DebugSymbolInfo readDebugSymbolInfo(BinaryReader rdr)
        {
            var info = new DebugSymbolInfo();

            info.moduleIdx = rdr.ReadUInt32();
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

            return info;
        }

        private static DebugThreadInfo readDebugThreadInfo(BinaryReader rdr)
        {
            var info = new DebugThreadInfo();

            info.name = readString(rdr);
            info.id = rdr.ReadUInt32();

            info.curCoreId = rdr.ReadInt32();
            info.attribs = rdr.ReadUInt32();
            info.state = rdr.ReadUInt32();

            info.entryPoint = rdr.ReadUInt32();
            info.stackStart = rdr.ReadUInt32();
            info.stackEnd = rdr.ReadUInt32();

            info.cia = rdr.ReadUInt32();

            info.gpr = new uint[32];
            for (int i = 0; i < 32; ++i)
            {
                info.gpr[i] = rdr.ReadUInt32();
            }
            info.lr = rdr.ReadUInt32();
            info.ctr = rdr.ReadUInt32();
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

            ulong numSymbols = rdr.ReadUInt64();
            info.symbols = new DebugSymbolInfo[numSymbols];
            for (ulong i = 0; i < numSymbols; ++i)
            {
                info.symbols[i] = readDebugSymbolInfo(rdr);
            }

            return info;
        }

        public static DebugTraceEntry readDebugTraceEntry(BinaryReader rdr)
        {
            DebugTraceEntry info = new DebugTraceEntry();

            info.cia = rdr.ReadUInt32();

            ulong numFields = rdr.ReadUInt64();
            info.fields = new DebugTraceEntryField[numFields];
            for (ulong i = 0; i < numFields; ++i)
            {
                info.fields[i].type = (StateField)rdr.ReadUInt32();
                info.fields[i].data = rdr.ReadBytes(16);
            }

            return info;
        }

        public static void HandlePacket(byte[] data)
        {
            Debug.WriteLine("HandlePacket");

            BinaryReader rdr = new BinaryReader(new MemoryStream(data));

            var size = rdr.ReadUInt32();
            var cmd = (PacketCmd)rdr.ReadUInt16();
            var reqId = rdr.ReadUInt16();

            if (cmd == PacketCmd.BpHit)
            {
                ResetDataCache();
                currentlyPaused = true;

                var e = new BpHitEventArgs();
                e.coreId = rdr.ReadUInt32();
                e.PauseInfo = readDebugPauseInfo(rdr);
                BpHitEvent.Invoke(null, e);
            }
            else if (cmd == PacketCmd.CoreStepped)
            {
                ResetDataCache();
                currentlyPaused = true;

                var e = new CoreSteppedEventArgs();
                e.coreId = rdr.ReadUInt32();
                e.PauseInfo = readDebugPauseInfo(rdr);
                CoreSteppedEvent.Invoke(null, e);
            }
            else if (cmd == PacketCmd.Paused)
            {
                ResetDataCache();
                currentlyPaused = true;

                var e = new PausedEventArgs();
                e.PauseInfo = readDebugPauseInfo(rdr);
                PausedEvent.Invoke(null, e);
            }
            else if (cmd == PacketCmd.ReadMemRes)
            {
                var address = rdr.ReadUInt32();
                var numBytes = rdr.ReadUInt64();
                var bytes = rdr.ReadBytes((int)numBytes);

                cacheMutex.WaitOne();

                var pageIdx = address / GetMemoryPageSize();
                var page = memDict[pageIdx];
                if (page != null)
                {
                    page.data = bytes;
                    var waiters = page.waiters;
                    page.waiters = new List<DoneNotif>();
                    waiters.ForEach((DoneNotif i) => { i(); });
                }

                cacheMutex.ReleaseMutex();
            }
            else if (cmd == PacketCmd.DisasmRes)
            {
                var address = rdr.ReadUInt32();
                var numInstrs = rdr.ReadUInt64();
                var instrs = new string[numInstrs];
                for (var i = 0; i < (int)numInstrs; ++i)
                {
                    instrs[i] = readString(rdr);
                }

                cacheMutex.WaitOne();

                var pageIdx = address / GetInstrPageSize();
                var page = instrDict[pageIdx];
                if (page != null)
                {
                    page.data = instrs;
                    var waiters = page.waiters;
                    page.waiters = new List<DoneNotif>();
                    waiters.ForEach((DoneNotif i) => { i(); });
                }

                cacheMutex.ReleaseMutex();
            }
            else if (cmd == PacketCmd.GetTraceRes)
            {
                var e = new GetTraceEventArgs();

                e.ThreadId = rdr.ReadUInt32();

                var numTraces = rdr.ReadUInt64();
                e.Info = new DebugTraceEntry[numTraces];
                for (ulong i = 0; i < numTraces; ++i)
                {
                    e.Info[i] = readDebugTraceEntry(rdr);
                }
                
                GetTraceEvent.Invoke(null, e);
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
        public static void SendPacket(PacketCmd cmd, ushort reqId, PacketBuilder fn)
        {
            var ms = new MemoryStream();
            var wrt = new BinaryWriter(ms);

            wrt.Write((uint)ms.Length);
            wrt.Write((ushort)cmd);
            wrt.Write(reqId);

            if (fn != null)
            {
                fn(wrt);
            }

            wrt.Seek(0, SeekOrigin.Begin);
            wrt.Write((uint)ms.Length);

            Send(currentState.workSocket, ms.ToArray());
        }

        public static void SendAddBreakpoint(uint address)
        {
            SendPacket(PacketCmd.AddBreakpoint, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(address);
            });
        }

        public static void SendRemoveBreakpoint(uint address)
        {
            SendPacket(PacketCmd.RemoveBreakpoint, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(address);
            });
        }

        public static void SendPause()
        {
            SendPacket(PacketCmd.Pause, 0, null);
        }

        public static void SendResume()
        {
            SendPacket(PacketCmd.Resume, 0, null);
            currentlyPaused = false;
        }
    
        public static void SendStepCore(uint coreId)
        {
            SendPacket(PacketCmd.StepCore, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(coreId);
            });
            currentlyPaused = false;
        }

        public static void SendStepCoreOver(uint coreId)
        {
            SendPacket(PacketCmd.StepCoreOver, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(coreId);
            });
            currentlyPaused = false;
        }

        public static void SendGetTrace(uint threadId)
        {
            SendPacket(PacketCmd.GetTrace, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(threadId);
            });
        }

        public delegate void DoneNotif();

        private static void sendReadMem(uint address, uint size)
        {
            Debug.WriteLine("ReadMem {0:x08} {1}", address, size);
            SendPacket(PacketCmd.ReadMem, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(address);
                wrt.Write(size);
            });
        }

        private static void sendDisasm(uint address, uint numInstrs)
        {
            Debug.WriteLine("Disasm {0:x08} {1}", address, numInstrs);
            SendPacket(PacketCmd.Disasm, 0, (BinaryWriter wrt) =>
            {
                wrt.Write(address);
                wrt.Write(numInstrs);
            });
        }

        public static uint GetMemoryPageSize()
        {
            return 1024;
        }

        public static byte[] GetMemoryPage(uint pageIdx, DoneNotif notifFn)
        {
            if (!currentlyPaused)
            {
                // Don't allow reading while the game is running...
                return null;
            }

            cacheMutex.WaitOne();
            MemoryPage entry;
            if (!memDict.TryGetValue(pageIdx, out entry))
            {
                entry = null;
            }

            if (entry == null)
            {
                var newPage = new MemoryPage();
                newPage.pauseIdx = 0;
                newPage.data = null;
                newPage.waiters = new List<DoneNotif>();
                memDict.Add(pageIdx, newPage);
                entry = newPage;                
            }

            if (entry.pauseIdx < pauseIndex)
            {
                uint PAGE_SIZE = GetMemoryPageSize();
                sendReadMem(pageIdx * PAGE_SIZE, PAGE_SIZE);
                entry.pauseIdx = pauseIndex;

                if (notifFn != null)
                {
                    entry.waiters.Add(notifFn);
                }
            }
            
            cacheMutex.ReleaseMutex();
            return entry.data;
        }

        public static uint GetInstrPageSize()
        {
            return 256;
        }

        public static string[] GetInstrPage(uint pageIdx, DoneNotif notifFn)
        {
            if (!currentlyPaused)
            {
                // Don't allow reading while the game is running...
                return null;
            }

            cacheMutex.WaitOne();
            InstrPage entry;
            if (!instrDict.TryGetValue(pageIdx, out entry))
            {
                entry = null;
            }

            if (entry == null)
            {
                var newPage = new InstrPage();
                newPage.pauseIdx = 0;
                newPage.data = null;
                newPage.waiters = new List<DoneNotif>();
                instrDict.Add(pageIdx, newPage);
                entry = newPage;
            }

            if (entry.pauseIdx < pauseIndex)
            {
                uint PAGE_SIZE = GetInstrPageSize();
                sendDisasm(pageIdx * PAGE_SIZE, PAGE_SIZE / 4);
                entry.pauseIdx = pauseIndex;

                if (notifFn != null)
                {
                    entry.waiters.Add(notifFn);
                }
            }

            cacheMutex.ReleaseMutex();
            return entry.data;
        }

        private static void ResetDataCache()
        {
            cacheMutex.WaitOne();
            pauseIndex++;
            cacheMutex.ReleaseMutex();
        }

        private static Mutex cacheMutex = new Mutex();
        private static uint pauseIndex = 1;

        private class MemoryPage
        {
            public uint pauseIdx;
            public byte[] data;
            public List<DoneNotif> waiters;
        };
        private static Dictionary<uint, MemoryPage> memDict = new Dictionary<uint, MemoryPage>();

        private class InstrPage
        {
            public uint pauseIdx;
            public string[] data;
            public List<DoneNotif> waiters;
        }
        private static Dictionary<uint, InstrPage> instrDict = new Dictionary<uint, InstrPage>();

        public static EmuMemoryReader GetMemoryReader(uint address, DoneNotif notifFn)
        {
            return new EmuMemoryReader(address, notifFn);
        }

        public static EmuInstrReader GetInstrReader(uint address, DoneNotif notifFn)
        {
            return new EmuInstrReader(address, notifFn);
        }

        public class EmuMemoryReader
        {
            public EmuMemoryReader(uint address, NetHandler.DoneNotif notifFn)
            {
                notifier = notifFn;
                pageSize = NetHandler.GetMemoryPageSize();
                currentAddress = address;
                retrievePage(true);
            }

            private void retrievePage(bool force = false)
            {
                uint newPageIdx = currentAddress / pageSize;
                if (!force && newPageIdx == currentPageIdx)
                {
                    return;
                }

                currentPage = NetHandler.GetMemoryPage(newPageIdx, notifier);
                currentPageIdx = newPageIdx;
            }

            public bool GetUint8(out byte value)
            {
                if (currentPage == null)
                {
                    value = 0;
                    currentAddress += 1;
                    retrievePage();
                    return false;
                }

                int pageOffset = (int)(currentAddress - (currentPageIdx * pageSize));
                Debug.Assert(pageOffset + 1 <= pageSize);

                value = currentPage[pageOffset + 0];
                currentAddress += 1;
                retrievePage();

                return true;
            }

            public bool GetUInt32(out uint value)
            {
                if (currentPage == null)
                {
                    value = 0;
                    currentAddress += 4;
                    retrievePage();
                    return false;
                }

                int pageOffset = (int)(currentAddress - (currentPageIdx * pageSize));
                Debug.Assert(pageOffset + 4 <= pageSize);

                value =
                    (uint)currentPage[pageOffset + 0] << 24 |
                    (uint)currentPage[pageOffset + 1] << 16 |
                    (uint)currentPage[pageOffset + 2] << 8 |
                    (uint)currentPage[pageOffset + 3];
                currentAddress += 4;
                retrievePage();

                return true;
            }

            NetHandler.DoneNotif notifier;
            uint pageSize;
            uint currentPageIdx;
            byte[] currentPage;
            protected uint currentAddress;
        }

        public class EmuInstrReader
        {
            public EmuInstrReader(uint address, NetHandler.DoneNotif notifFn)
            {
                notifier = notifFn;
                pageSize = NetHandler.GetInstrPageSize();
                currentAddress = address;
                retrievePage(true);
            }

            private void retrievePage(bool force = false)
            {
                uint newPageIdx = currentAddress / pageSize;
                if (!force && newPageIdx == currentPageIdx)
                {
                    return;
                }

                currentPage = NetHandler.GetInstrPage(newPageIdx, notifier);
                currentPageIdx = newPageIdx;
            }

            public bool GetInstr(out string value)
            {
                if (currentPage == null)
                {
                    value = "";
                    currentAddress += 4;
                    retrievePage();
                    return false;
                }

                int pageOffset = (int)(currentAddress - (currentPageIdx * pageSize));
                Debug.Assert(pageOffset + 4 <= pageSize);

                value = currentPage[pageOffset / 4];
                currentAddress += 4;
                retrievePage();

                return true;
            }

            NetHandler.DoneNotif notifier;
            uint pageSize;
            uint currentPageIdx;
            string[] currentPage;
            protected uint currentAddress;
        }

        public static event EventHandler<ConnectedEventArgs> ConnectedEvent;
        public static event EventHandler<DisconnectedEventArgs> DisconnectedEvent;
        public static event EventHandler<BpHitEventArgs> BpHitEvent;
        public static event EventHandler<CoreSteppedEventArgs> CoreSteppedEvent;
        public static event EventHandler<PausedEventArgs> PausedEvent;
        public static event EventHandler<GetTraceEventArgs> GetTraceEvent;
    }
}
