﻿using OctoAwesome.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Model
{
    internal sealed class Game
    {
        private Input input;

        public Camera Camera { get; private set; }

        public Vector2 PlaygroundSize
        {
            get
            {
                return new Vector2(Map.Columns, Map.Rows);
            }
        }

        public Player Player { get; private set; }

        public Map Map { get; private set; }

        public Game(Input input)
        {
            //Map = Map.Generate(20, 20, CellType.Grass);
            Map = Map.Load(@"C:\Users\sebip\OneDrive\Desktop\testMap.map");
            Player = new Player(input, Map);
            Camera = new Camera(this, input);
        }

        public void Update(TimeSpan frameTime)
        {
            Player.Update(frameTime);

            //Oberflächenbeschaffenheit ermitteln
            int cellX = (int)Player.Position.X;
            int cellY = (int)Player.Position.Y;

            CellType cellType = Map.GetCell(cellX, cellY);

            //Geschwindigkeit modifizieren
            Vector2 velocity = Player.Velocity;

            switch (cellType)
            {
                case CellType.Grass:
                    velocity *= 0.8f;
                    break;
                case CellType.Sand:
                    velocity *= 1f;
                    break;
            }

            Player.Position += (velocity * (float)frameTime.TotalSeconds);

            if (Player.Position.X - Player.Radius < 0)
            {
                Player.Position = new Vector2(Player.Radius, Player.Position.Y);
            }

            if (Player.Position.X + Player.Radius > PlaygroundSize.X)
            {
                Player.Position = new Vector2(PlaygroundSize.X - Player.Radius, Player.Position.Y);
            }

            if (Player.Position.Y - Player.Radius < 0)
            {
                Player.Position = new Vector2(Player.Position.X, Player.Radius);
            }

            if (Player.Position.Y + Player.Radius > PlaygroundSize.Y)
            {
                Player.Position = new Vector2(PlaygroundSize.X, Player.Position.Y - Player.Radius);
            }
            Camera.Update(frameTime);
        }
    }
}
