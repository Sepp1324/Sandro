﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OctoAwesomeDX.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OctoAwesome.Components
{
    internal sealed class HudComponent : DrawableGameComponent
    {
        private WorldComponent world;

        private SpriteBatch batch;
        private SpriteFont font;

        private Texture2D pix;

        private float[] frameBuffer;

        private int bufferSize = 10;
        private int bufferIndex = 0;

        //private int frameCount = 0;
        //private double milliSeconds = 0;
        //private double lastValue = 0;

        public HudComponent(Game game, WorldComponent world): base(game)
        {
            this.world = world;

            frameBuffer = new float[bufferSize];
        }

        public override void Initialize()
        {
            batch = new SpriteBatch(Game.GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            font = Game.Content.Load<SpriteFont>("hud");
            pix = Game.Content.Load<Texture2D>("Textures/pix");
            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            //frameCount++;
            //milliSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            //if (frameCount == 10)
            //{
            //    lastValue = milliSeconds / frameCount;
            //    frameCount = 0;
            //    milliSeconds = 0;
            //}

            frameBuffer[bufferIndex++] = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bufferIndex %= bufferSize;

            batch.Begin();

            batch.DrawString(font, "Development Version", new Vector2(5, 5), Color.White);

            //string pos = "pos: " + world.World.Player.Position.ToString();
            //string pos = "pos: " +
            //    world.World.Player.Position.X.ToString("0.00") + "/" +
            //    world.World.Player.Position.Y.ToString("0.00") + "/" +
            //    world.World.Player.Position.Z.ToString("0.00");
            string pos = "pos: [" +
                world.World.Player.Position.BlockPosition.X.ToString("0") + "/" +
                world.World.Player.Position.BlockPosition.Y.ToString("0.00") + "/" +
                world.World.Player.Position.BlockPosition.Z.ToString("0.00") + "] (" +
                world.World.Player.Position.GlobalPosition.X.ToString("0.00") + "/" +
                world.World.Player.Position.GlobalPosition.Y.ToString("0.00") + "/" +
                world.World.Player.Position.GlobalPosition.Z.ToString("0.00") + ")";
            var size = font.MeasureString(pos);
            batch.DrawString(font, pos, new Vector2(GraphicsDevice.Viewport.Width - size.X - 5, 5), Color.White);

            float deg = (world.World.Player.Angle / MathHelper.TwoPi) * 360;

            string rot = "rot: " + (((world.World.Player.Angle / MathHelper.TwoPi) * 360) % 360).ToString("0.00") + " / " + ((world.World.Player.Tilt / MathHelper.TwoPi) * 360).ToString("0.00");
            size = font.MeasureString(rot); 
            batch.DrawString(font, rot, new Vector2(GraphicsDevice.Viewport.Width - size.X - 5, 25), Color.White);

            string fps = "fps: " + (1f / (frameBuffer.Sum() / bufferSize)).ToString("0.00");
            size = font.MeasureString(fps);
            batch.DrawString(font, fps, new Vector2(GraphicsDevice.Viewport.Width - size.X - 5, 45), Color.White);

            //string fps = "fps: " + (1f / lastValue).ToString("0.00");
            //size = font.MeasureString(fps);
            //batch.DrawString(font, fps, new Vector2(GraphicsDevice.Viewport.Width - size.X - 5, 45), Color.White);

            int centerX = GraphicsDevice.Viewport.Width / 2;
            int centerY = GraphicsDevice.Viewport.Height / 2;

            batch.Draw(pix, new Rectangle(centerX - 1, centerY - 15, 2, 30), Color.White * 0.5f);
            batch.Draw(pix, new Rectangle(centerX - 15, centerY - 1, 30, 2), Color.White * 0.5f);

            batch.End();
        }
    }
}