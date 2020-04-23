﻿using Microsoft.Xna.Framework;

namespace OctoAwesome.Runtime
{
    public sealed class World
    {
        private UpdateDomain[] updateDomains;

        public ActorHost Player { get { return updateDomains[0].ActorHosts[0]; } }

        public World()
        {
            updateDomains = new UpdateDomain[1];
            updateDomains[0] = new UpdateDomain();
        }

        public void Update(GameTime frameTime)
        {
            foreach (var updateDomain in updateDomains)
            {
                updateDomain.Update(frameTime);
            }
        }

        public void Save()
        {
            foreach (var updateDomain in updateDomains)
            {
                updateDomain.Save();
            }
        }
    }
}