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
        private WorldComponent world;
        private EgoCameraComponent camera;

        private BasicEffect effect;
        private BasicEffect selectionEffect;

        private Texture2D blockTextures;

        private VertexBuffer vb;
        private IndexBuffer ib;

        private int vertexCount;
        private int indexCount;

        private VertexPositionColor[] selectionLines;
        private short[] selectionIndizes;

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

            RegenerateVertexBuffer();

            effect = new BasicEffect(GraphicsDevice);

            effect.World = Matrix.Identity;
            effect.Projection = camera.Projection;
            effect.TextureEnabled = true;

            effect.EnableDefaultLighting();

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
            if (world.Dirty)
            {
                RegenerateVertexBuffer();
                world.Dirty = false;
            }

            int cellX = world.World.Player.Position.Block.X;
            int cellY = world.World.Player.Position.Block.Y;
            int cellZ = world.World.Player.Position.Block.Z;

            int range = 8;
            Vector3? selected = null;
            float? bestDistance = null;

            for (int z = cellZ - range; z < cellZ + range; z++)
            {
                for (int y = cellY - range; y < cellY + range; y++)
                {
                    for (int x = cellX - range; x < cellX + range; x++)
                    {
                        if (x < 0 || x >= Chunk.CHUNKSIZE_X || y < 0 || y >= Chunk.CHUNKSIZE_Y || z < 0 || z >= Chunk.CHUNKSIZE_Z) continue;

                        IBlock block = world.World.Chunk.Blocks[x, y, z];

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
                                    selected = new Vector3(x, y, z);
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

            //RasterizerState r = new RasterizerState();
            //r.CullMode = CullMode.None;
            //GraphicsDevice.RasterizerState = r;

            effect.World = Matrix.Identity;
            effect.View = camera.View;
            effect.Texture = blockTextures;

            GraphicsDevice.SetVertexBuffer(vb);
            GraphicsDevice.Indices = ib;


            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, indexCount / 3);
            }

            if (world.SelectedBox.HasValue)
            {
                selectionEffect.World = Matrix.CreateTranslation(world.SelectedBox.Value);
                selectionEffect.View = camera.View;
                selectionEffect.Projection = camera.Projection;

                foreach (var pass in selectionEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, selectionLines, 0, 8, selectionIndizes, 0, 12);
                }
            }
        }

        private void RegenerateVertexBuffer()
        {
            if (vb != null)
            {
                vb.Dispose();
                vb = null;
            }

            if (ib != null)
            {
                ib.Dispose();
                ib = null;
            }

            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> index = new List<int>();

            for (int z = 0; z < Chunk.CHUNKSIZE_Z; z++)
            {
                for (int y = 0; y < Chunk.CHUNKSIZE_Y; y++)
                {
                    for (int x = 0; x < Chunk.CHUNKSIZE_X; x++)
                    {
                        if (world.World.Chunk.Blocks[x, y, z] == null) continue;

                        //Textur-Kooridinate "berechnen" :D
                        Vector2 textureOffset = new Vector2();
                        Vector2 textureSize = new Vector2(0.49f, 0.49f);

                        if (world.World.Chunk.Blocks[x, y, z] is GrassBlock)
                        {
                            textureOffset = new Vector2(0.005f, 0.005f);
                        }
                        else if (world.World.Chunk.Blocks[x, y, z] is SandBlock)
                        {
                            textureOffset = new Vector2(0.505f, 0.005f);
                        }

                        //Oben
                        if (y == Chunk.CHUNKSIZE_Y - 1 || world.World.Chunk.Blocks[x, y + 1, z] == null)
                        {
                            int localOffset = vertices.Count;

                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 1, z + 0), Vector3.Up, textureOffset));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 1, z + 0), Vector3.Up, new Vector2(textureOffset.X + textureSize.X, textureOffset.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 1, z + 1), Vector3.Up, new Vector2(textureOffset.X, textureOffset.Y + textureSize.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 1, z + 1), Vector3.Up, textureOffset + textureSize));
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 1);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 2);
                        }

                        //Links
                        if (x == 0 || world.World.Chunk.Blocks[x - 1, y, z] == null)
                        {
                            int localOffset = vertices.Count;

                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 1, z + 0), Vector3.Left, textureOffset));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 1, z + 1), Vector3.Left, new Vector2(textureOffset.X + textureSize.X, textureOffset.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 0, z + 0), Vector3.Left, new Vector2(textureOffset.X, textureOffset.Y + textureSize.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 0, z + 1), Vector3.Left, textureOffset + textureSize));
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 1);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 2);
                        }

                        //Vorne
                        if (z == Chunk.CHUNKSIZE_Z - 1 || world.World.Chunk.Blocks[x, y, z + 1] == null)
                        {
                            int localOffset = vertices.Count;

                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 1, z + 1), Vector3.Forward, textureOffset));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 1, z + 1), Vector3.Forward, new Vector2(textureOffset.X + textureSize.X, textureOffset.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 0, z + 1), Vector3.Forward, new Vector2(textureOffset.X, textureOffset.Y + textureSize.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 0, z + 1), Vector3.Forward, textureOffset + textureSize));
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 1);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 2);
                        }

                        //Rechts
                        if (x == Chunk.CHUNKSIZE_X - 1 || world.World.Chunk.Blocks[x + 1, y, z] == null)
                        {
                            int localOffset = vertices.Count;

                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 1, z + 1), Vector3.Right, textureOffset));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 1, z + 0), Vector3.Right, new Vector2(textureOffset.X + textureSize.X, textureOffset.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 0, z + 1), Vector3.Right, new Vector2(textureOffset.X, textureOffset.Y + textureSize.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 0, z + 0), Vector3.Right, textureOffset + textureSize));
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 1);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 2);
                        }

                        //Hinten
                        if (z == 0 || world.World.Chunk.Blocks[x, y, z - 1] == null)
                        {
                            int localOffset = vertices.Count;

                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 1, z + 0), Vector3.Backward, textureOffset));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 1, z + 0), Vector3.Backward, new Vector2(textureOffset.X + textureSize.X, textureOffset.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 0, z + 0), Vector3.Backward, new Vector2(textureOffset.X, textureOffset.Y + textureSize.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 0, z + 0), Vector3.Backward, textureOffset + textureSize));
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 1);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 2);
                        }

                        //Unten
                        if (y == 0 || world.World.Chunk.Blocks[x, y - 1, z] == null)
                        {
                            int localOffset = vertices.Count;

                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 0, z + 1), Vector3.Down, textureOffset));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 0, z + 1), Vector3.Down, new Vector2(textureOffset.X + textureSize.X, textureOffset.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 0, y + 0, z + 0), Vector3.Down, new Vector2(textureOffset.X, textureOffset.Y + textureSize.Y)));
                            vertices.Add(new VertexPositionNormalTexture(new Vector3(x + 1, y + 0, z + 0), Vector3.Down, textureOffset + textureSize));
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 1);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 0);
                            index.Add(localOffset + 3);
                            index.Add(localOffset + 2);
                        }
                    }
                }
            }

            vertexCount = vertices.Count;
            indexCount = index.Count;

            vb = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertexCount, BufferUsage.WriteOnly);
            vb.SetData<VertexPositionNormalTexture>(vertices.ToArray());

            ib = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, indexCount, BufferUsage.WriteOnly);
            ib.SetData<int>(index.ToArray());
        }
    }
}
