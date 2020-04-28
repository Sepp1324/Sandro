﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OctoAwesome.Basics
{
    public class PickaxeDefinition : IItemDefinition
    {
        public string Name
        {
            get
            {
                return "Pickaxe";
            }
        }

        public Bitmap Icon => throw new NotImplementedException();

        public PhysicalProperties GetProperties(IItem item)
        {
            return new PhysicalProperties()
            {
                Density = 1f,
                FractureToughness = 1f,
                Granularity = 1f,
                Hardness = 1f
            };
        }
    }
}