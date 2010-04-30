﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Mammoth.Engine.Networking;

namespace Mammoth.Engine
{
    class ModelDatabase : DrawableGameComponent, IModelDBService
    {
        private static int nextID = 0;

        private Dictionary<int, BaseObject> _objects;

        public ModelDatabase(Game game) : base(game)
        {
            this.Game.Services.AddService(typeof(IModelDBService), this);
            _objects = new Dictionary<int, BaseObject>();
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var obj in _objects.Values)
                obj.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (var obj in _objects.Values)
                obj.Draw(gameTime);
        }

        #region IModelDBService Members

        public bool hasObject(int objectID)
        {
            return _objects.ContainsKey(objectID);
        }

        public BaseObject getObject(int objectID)
        {
            return _objects[objectID];
        }

        public void registerObject(BaseObject newObject)
        {
            _objects.Add(newObject.ID, newObject);
        }

        public bool removeObject(int objectID)
        {
            return _objects.Remove(objectID);
        }

        public int getNextOpenID()
        {
            IClientNetworking net = (IClientNetworking)this.Game.Services.GetService(typeof(INetworkingService));
            return net.ClientID << 25 | nextID++;
        }

        #endregion
    }
}
