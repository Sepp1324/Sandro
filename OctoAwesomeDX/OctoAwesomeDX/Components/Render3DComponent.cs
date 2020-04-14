﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OctoAwesome.Model;
using OctoAwesome.Model.Blocks;
using OctoAwesomeDX.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Components
{
    internal sealed class Render3DComponent : DrawableGameComponent
    {
        public static Index3 VIEWRANGE = new Index3(2, 2, 1);

        private WorldComponent world;
        private EgoCameraComponent camera;

        private ChunkRenderer[, ,] chunkRenderer;

        private BasicEffect selectionEffect;

        private Texture2D blockTextures;

        private VertexPositionColor[] selectionLines;
        private short[] selectionIndizes;

        private Index3 chunkOffset = new Index3(-1, -1, -1);

        public Render3DComponent(Game game, WorldComponent world, EgoCameraComponent camera)
            : base(game)
        {
            this.world = world;
            this.camera = camera;
        }

        protected override void LoadContent()
        {
            Bitmap grassTex = GrassBlock.Texture;
            Bitmap sandTex = SandBlock.Texture;

            Bitmap blocks = new Bitmap(128, 128);
            using (Graphics g = Graphics.FromImage(blocks))
            {
                g.DrawImage(grassTex, new PointF(0, 0));
                g.DrawImage(sandTex, new PointF(64, 0));
            }

            using (MemoryStream stream = new MemoryStream())
            {
                blocks.Save(stream, ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                blockTextures = Texture2D.FromStream(GraphicsDevice, stream);
            }

            IPlanet planet = world.World.GetPlanet(0);
            chunkRenderer = new ChunkRenderer[
                (VIEWRANGE.X * 2) + 1,
                (VIEWRANGE.Y * 2) + 1,
                (VIEWRANGE.Z * 2) + 1];

            for (int x = 0; x < chunkRenderer.GetLength(0); x++)
            {
                for (int y = 0; y < chunkRenderer.GetLength(1); y++)
                {
                    for (int z = 0; z < chunkRenderer.GetLength(2); z++)
                    {
                        chunkRenderer[x, y, z] = new ChunkRenderer(GraphicsDevice, camera.Projection, blockTextures)
                        {
                            RelativeIndex = new Index3(x - VIEWRANGE.X, y - VIEWRANGE.Y, z - VIEWRANGE.Z)
                        };
                    }
                }
            }

            FillChunkRenderer();

            selectionLines = new[] 
            {
                new VertexPositionColor(new Vector3(-0.001f, +1.001f, +1.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, +1.001f, +1.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, -0.001f, +1.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, -0.001f, +1.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, +1.001f, -0.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, +1.001f, -0.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, -0.001f, -0.001f), Microsoft.Xna.Framework.Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, -0.001f, -0.001f), Microsoft.Xna.Framework.Color.Black * 0.5f)
            };

            selectionIndizes = new short[] {
                0, 1, 0, 2, 1, 3, 2, 3,
                4, 5, 4, 6, 5, 7, 6, 7,
                0, 4, 1, 5, 2, 6, 3, 7
            };

            selectionEffect = new BasicEffect(GraphicsDevice);
            selectionEffect.VertexColorEnabled = true;

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            FillChunkRenderer();

            for (int x = 0; x < chunkRenderer.GetLength(0); x++)
            {
                for (int y = 0; y < chunkRenderer.GetLength(1); y++)
                {
                    for (int z = 0; z < chunkRenderer.GetLength(2); z++)
                    {
                        chunkRenderer[x, y, z].Update();
                    }
                }
            }

            int cellX = world.World.Player.Position.LocalBlockIndex.X;
            int cellY = world.World.Player.Position.LocalBlockIndex.Y;
            int cellZ = world.World.Player.Position.LocalBlockIndex.Z;

            int range = 8;
            Vector3? selected = null;
            IPlanet planet = world.World.GetPlanet(world.World.Player.Position.Planet);
            float? bestDistance = null;

            for (int z = cellZ - range; z < cellZ + range; z++)
            {
                for (int y = cellY - range; y < cellY + range; y++)
                {
                    for (int x = cellX - range; x < cellX + range; x++)
                    {
                        Index3 pos = new Index3(
                            x + (chunkOffset.X * Chunk.CHUNKSIZE_X),
                            y + (chunkOffset.Y * Chunk.CHUNKSIZE_Y),
                            z + (chunkOffset.Z * Chunk.CHUNKSIZE_Z));

                        IBlock block = planet.GetBlock(pos);

                        if (block == null) continue;

                        BoundingBox[] boxes = block.GetCollisionBoxes();

                        foreach (var box in boxes)
                        {
                            BoundingBox transformedBox = new BoundingBox(box.Min + new Vector3(x, y, z), box.Max + new Vector3(x, y, z));

                            float? distance = camera.PickRay.Intersects(transformedBox);

                            if (distance.HasValue)
                            {
                                if (!bestDistance.HasValue || bestDistance.Value > distance)
                                {
                                    bestDistance = distance.Value;
                                    selected = new Vector3(
                                        (world.World.Player.Position.ChunkIndex.X * Chunk.CHUNKSIZE_X) + x,
                                        (world.World.Player.Position.ChunkIndex.Y * Chunk.CHUNKSIZE_Y) + y,
                                        (world.World.Player.Position.ChunkIndex.Z * Chunk.CHUNKSIZE_Z) + z);
                                }
                            }
                        }
                    }
                }
            }
            world.SelectedBox = selected;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            for (int x = 0; x < chunkRenderer.GetLength(0); x++)
            {
                for (int y = 0; y < chunkRenderer.GetLength(1); y++)
                {
                    for (int z = 0; z < chunkRenderer.GetLength(2); z++)
                    {
                        chunkRenderer[x, y, z].Draw(camera.View);
                    }
                }
            }

            if (world.SelectedBox.HasValue)
            {
                Vector3 selectedBoxPosition = new Vector3(
                    world.SelectedBox.Value.X - (chunkOffset.X * Chunk.CHUNKSIZE_X),
                    world.SelectedBox.Value.Y - (chunkOffset.Y * Chunk.CHUNKSIZE_Y),
                    world.SelectedBox.Value.Z - (chunkOffset.Z * Chunk.CHUNKSIZE_Z));

                selectionEffect.World = Matrix.CreateTranslation(selectedBoxPosition);
                selectionEffect.View = camera.View;
                selectionEffect.Projection = camera.Projection;

                foreach (var pass in selectionEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, selectionLines, 0, 8, selectionIndizes, 0, 12);
                }
            }
        }

        private void FillChunkRenderer()
        {
            Index3 centerChunk = world.World.Player.Position.ChunkIndex;

            if (centerChunk == chunkOffset) return;

            if (centerChunk.X > chunkOffset.X)
            {
                //Scrolling nach rechts
            }

            for (int x = -VIEWRANGE.X; x <= VIEWRANGE.X; x++)
            {
                for (int y = -VIEWRANGE.Y; y <= VIEWRANGE.Y; y++)
                {
                    for (int z = -VIEWRANGE.Z; z <= VIEWRANGE.Z; z++)
                    {
                        IPlanet planet = world.World.GetPlanet(0);
                        Index3 chunkIndex = new Index3(centerChunk.X + x, centerChunk.Y + y, centerChunk.Z + z);

                        if (chunkIndex.X < 0) chunkIndex.X += planet.Size.X;
                        if (chunkIndex.Y < 0) chunkIndex.Y += planet.Size.Y;

                        chunkIndex.X %= planet.Size.X;
                        chunkIndex.Y %= planet.Size.Y;

                        IChunk chunk = world.World.GetPlanet(0).GetChunk(chunkIndex);
                        chunkRenderer[
                            x + VIEWRANGE.X,
                            y + VIEWRANGE.Y,
                            z + VIEWRANGE.Z].SetChunk(chunk);
                    }
                }
            }

            chunkOffset = centerChunk;
        }
    }
}
