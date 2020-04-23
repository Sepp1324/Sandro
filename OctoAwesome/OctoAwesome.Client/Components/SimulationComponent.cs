﻿using Microsoft.Xna.Framework;
using OctoAwesome.Runtime;

namespace OctoAwesome.Client.Components
{
    internal sealed class SimulationComponent : GameComponent
    {
        public World World { get; private set; }

        public SimulationComponent(Game game) : base(game)
        {

        }

        public override void Initialize()
        {
            World = new World();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            World.Update(gameTime);
        }
    }
}
