﻿using OctoAwesome.Basics.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OctoAwesome.Basics
{
    public sealed class GroundBlockDefinition : IBlockDefinition
    {
        public string Name
        {
            get { return "Ground"; }
        }

        public IEnumerable<Bitmap> Textures
        {
            get
            {
                return new[] { Resources.ground_bottom };
            }
        }

        public IBlock GetInstance(OrientationFlags orientation)
        {
            return new GroundBlock();
        }

        public Type GetBlockType()
        {
            return typeof(GroundBlock);
        }

        public int GetTopTextureIndex(IBlock block)
        {
            return 0;
        }

        public int GetBottomTextureIndex(IBlock block)
        {
            return 0;
        }

        public int GetNorthTextureIndex(IBlock block)
        {
            return 0;
        }

        public int GetSouthTextureIndex(IBlock block)
        {
            return 0;
        }

        public int GetWestTextureIndex(IBlock block)
        {
            return 0;
        }

        public int GetEastTextureIndex(IBlock block)
        {
            return 0;
        }

        public int GetTopTextureRotation(IBlock block)
        {
            return 0;
        }

        public int GetBottomTextureRotation(IBlock block)
        {
            return 0;
        }

        public int GetNorthTextureRotation(IBlock block)
        {
            return 0;
        }

        public int GetSouthTextureRotation(IBlock block)
        {
            return 0;
        }

        public int GetWestTextureRotation(IBlock block)
        {
            return 0;
        }

        public int GetEastTextureRotation(IBlock block)
        {
            return 0;
        }
    }
}