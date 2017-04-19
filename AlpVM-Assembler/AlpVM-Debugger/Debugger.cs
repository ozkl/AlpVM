using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlpVM_Debugger
{
    public class Debugger
    {
        Socket mClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        Machine mMachine = new Machine();

        bool mConnected = false;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct VmMessageHeader
        {
            public enum EnMessageType : int
            {
                MachineState,
                Memory,
                Breakpoints
            }

            public EnMessageType MessageType;

            public UInt32 MessageSize;

            public UInt32 Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct DebugCommand
        {
            public enum EnCommand : int
            {
                NoCommand,
                Pause,
                Resume,
                Step,
                SetBreakpoint,
                UnsetBreakpoint,
                GetBreakpoints,
                ClearBreakpoints,
                GetMemory
            };

            public EnCommand Command;
            public UInt32 Data;
        }

        public class StateObject
        {
            // Size of receive buffer.
            public const int BufferSize = 8192;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
        }

        public Action MachineStateUpdated;
        public Action<UInt32, UInt32> MemoryUpdated;
        public Action ConnectionStateChanged;

        public Machine Machine
        {
            get
            {
                return mMachine;
            }
        }

        public bool Connected
        {
            set
            {
                bool old = mConnected;

                mConnected = value;

                if (null != ConnectionStateChanged && old != mConnected)
                {
                    ConnectionStateChanged();
                }
            }

            get
            {
                return mConnected;
            }
        }

        public Debugger()
        {
            
        }

        public void Connect()
        {
            mClient.BeginConnect("localhost", 5566, new AsyncCallback(ConnectCallback), mClient);
        }

        void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Connected = client.Connected;

                if (client.Connected)
                {
                    Receive();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void Send(byte[] data)
        {
            if (Connected)
            {
                // Begin sending the data to the remote device.
                mClient.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), null);
            }
        }

        void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Complete sending the data to the remote device.
                int bytesSent = mClient.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void Receive()
        {
            try
            {
                StateObject state = new StateObject();

                // Begin receiving the data from the remote device.
                mClient.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;

                // Read data from the remote device.
                int bytesRead = mClient.EndReceive(ar);

                if (bytesRead > 0)
                {
                    try
                    {
                        OnNewMessage(state.buffer);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    Receive();
                }
                else
                {
                    //close connection
                    mClient.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                //close connection
                mClient.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), null);
            }
        }

        void DisconnectCallback(IAsyncResult ar)
        {
            mClient.EndDisconnect(ar);

            Connected = mClient.Connected;
        }

        void OnNewMessage(byte[] data)
        {
            object oMessageHeader = new VmMessageHeader();
            Utils.ByteArrayToStructure(data, 0, ref oMessageHeader);
            VmMessageHeader messageHeader = (VmMessageHeader)oMessageHeader;

            int headerSize = Marshal.SizeOf(oMessageHeader);

            switch (messageHeader.MessageType)
            {
                case VmMessageHeader.EnMessageType.MachineState:
                    if (messageHeader.MessageSize == Marshal.SizeOf(mMachine.MachineState))
                    {
                        object oMachineState = (object)new MachineState();
                        Utils.ByteArrayToStructure(data, headerSize, ref oMachineState);
                        mMachine.MachineState = (MachineState)oMachineState;

                        GetMemory(mMachine.MachineState.mExecutionPointer);//TODO: move elsewhere

                        if (null != MachineStateUpdated)
                        {
                            MachineStateUpdated();
                        }
                    }
                    break;
                case VmMessageHeader.EnMessageType.Memory:
                    {
                        Array.Copy(data, headerSize, mMachine.MachineMemory, messageHeader.Data, messageHeader.MessageSize);
                        if (null != MemoryUpdated)
                        {
                            MemoryUpdated(messageHeader.Data, messageHeader.MessageSize);
                        }
                    }
                    break;
            }
            
        }

        public void Step()
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.Step;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void Resume()
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.Resume;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void Pause()
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.Pause;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void SetBreakpoint(UInt32 address)
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.SetBreakpoint;
                command.Data = address;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void UnsetBreakpoint(UInt32 address)
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.UnsetBreakpoint;
                command.Data = address;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void GetBreakpoints()
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.GetBreakpoints;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void ClearBreakpoints()
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.ClearBreakpoints;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }

        public void GetMemory(UInt32 address)
        {
            if (mClient.Connected)
            {
                DebugCommand command = new DebugCommand();
                command.Command = DebugCommand.EnCommand.GetMemory;
                command.Data = address;

                byte[] bytes = Utils.StructureToByteArray(command);

                Send(bytes);
            }
        }
    }
}
