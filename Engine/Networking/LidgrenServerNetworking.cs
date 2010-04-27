﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Lidgren.Network.Xna;
using Lidgren.Network;

namespace Mammoth.Engine.Networking
{
    public class LidgrenServerNetworking : LidgrenNetworking, IServerNetworking
    {
        private NetServer _server;
        private Dictionary<int, NetConnection> _connections;
        private Queue<DataGram> _toSend;
        private Dictionary<int, Queue<InputStateUpdate>> _inputStates;

        private List<byte[]> _data;

        public LidgrenServerNetworking(Game game)
            : base(game)
        {
            _toSend = new Queue<DataGram>();
            _connections = new Dictionary<int, NetConnection>();
            _inputStates = new Dictionary<int, Queue<InputStateUpdate>>();

            _data = new List<byte[]>();
            createSession();
        }

        #region IServerNetworking Members

        public void sendThing(IEncodable toSend, int target)
        {
            _toSend.Enqueue(new DataGram(toSend.Encode(), target));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //Console.WriteLine("Updating server");

            NetBuffer buffer;

            while (_toSend.Count != 0)
            {
                DataGram message = _toSend.Dequeue();
                buffer = _server.CreateBuffer();
                buffer.Write(message.Data);
                _server.SendMessage(buffer, _connections[message.Recipient], NetChannel.Unreliable);
            }

            buffer = _server.CreateBuffer();
            NetMessageType type;
            NetConnection sender;
            while (_server.ReadMessage(buffer, out type, out sender))
            {
                switch (type)
                {
                    case NetMessageType.DebugMessage:
                        Console.WriteLine(buffer.ReadString());
                        break;
                    case NetMessageType.ConnectionApproval:
                        int id = buffer.ReadVariableInt32();
                        Console.WriteLine("Approval; id is " + id);
                        sender.Approve();
                        sender.Tag = id;
                        _connections.Add(id, sender);
                        break;
                    case NetMessageType.StatusChanged:
                        string statusMessage = buffer.ReadString();
                        NetConnectionStatus newStatus = (NetConnectionStatus)buffer.ReadByte();
                        Console.WriteLine("New status for " + sender + ": " + newStatus + " (" + statusMessage + ")");
                        break;
                    case NetMessageType.Data:
                        // A client sent this data!
                        Console.WriteLine("Data received from " + sender);
                        switch ((ClientToServerMessageType)buffer.ReadVariableInt32())
                        {
                            case ClientToServerMessageType.InputState:
                                int senderID = (int)sender.Tag;
                                double elapsedTime = buffer.ReadDouble();
                                uint inputBitmask = buffer.ReadVariableUInt32();
                                if (_inputStates[senderID] == null)
                                    _inputStates[senderID] = new Queue<InputStateUpdate>();
                                _inputStates[senderID].Enqueue(new InputStateUpdate(inputBitmask, elapsedTime));
                                break;
                        }
                        //byte[] data = buffer.ReadBytes(buffer.LengthBytes);
                        //Console.WriteLine(data);
                        //_data.Add(data);
                        break;
                }
            }
        }

        public List<byte[]> getData()
        {
            return _data;
        }

        public void createSession()
        {
            NetConfiguration config = new NetConfiguration("Mammoth");
            config.MaxConnections = 32;
            config.Port = PORT;

            _server = new NetServer(config);
            _server.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);
            _server.Start();
        }

        public void endGame()
        {
            _server.Shutdown("Game ended.");
        }

        #endregion

        private class DataGram
        {
            public byte[] Data { get; set;  }
            public int Recipient { get; set;  }

            public DataGram(byte[] data, int recipient)
            {
                Data = data;
                Recipient = recipient;
            }
        }
    }

    public class InputStateUpdate
    {
        public uint _bitmask;
        public double _elapsedTime;

        public InputStateUpdate(uint bitmask, double elapsedTime)
        {
            _bitmask = bitmask;
            _elapsedTime = elapsedTime;
        }

        public uint Bitmask
        {
            get
            {
                return _bitmask;
            }
        }

        public double ElapsedTime
        {
            get
            {
                return _elapsedTime;
            }
        }
    }
}
