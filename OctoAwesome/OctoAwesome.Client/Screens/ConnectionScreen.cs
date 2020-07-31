﻿using System;
using MonoGameUi;
using engenious;
using OctoAwesome.Client.Components;

namespace OctoAwesome.Client.Screens
{
    internal sealed class ConnectionScreen : BaseScreen
    {
        public new ScreenComponent Manager => (ScreenComponent)base.Manager;

        private ISettings _settings;
        private OctoGame _game;

        public ConnectionScreen(ScreenComponent manager) : base(manager)
        {
            _settings = Manager.Game.Settings;
            _game = Manager.Game;
            Padding = new Border(0, 0, 0, 0);

            Title = Languages.OctoClient.CreateUniverse;

            SetDefaultBackground();

            var panel = new StackPanel(manager)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = Border.All(50),
                Background = new BorderBrush(Color.White * 0.5f),
                Padding = Border.All(10)
            };
            Controls.Add(panel);

            var input = new Textbox(manager)
            {
                Text = "localhost",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                //VerticalAlignment = VerticalAlignment.Stretch,
                Background = new BorderBrush(Color.LightGray, LineType.Solid, Color.Black)
            };
            panel.Controls.Add(input);

            var createButton = Button.TextButton(manager, Languages.OctoClient.Connect);
            createButton.HorizontalAlignment = HorizontalAlignment.Center;
            createButton.VerticalAlignment = VerticalAlignment.Center;
            createButton.Visible = true;
            createButton.LeftMouseClick += (s, e) =>
            {
                _game.Settings.Set("server", input.Text);
                ((ContainerResourceManager)_game.ResourceManager).CreateManager(_game.ExtensionLoader, _game.DefinitionManager, _game.Settings, true);

                //manager.NavigateToScreen(new GameScreen(manager));

                PlayMultiplayer(manager);
            };
            panel.Controls.Add(createButton);
        }

        private void PlayMultiplayer(ScreenComponent manager)
        {
            Manager.Player.SetEntity(null);

            Manager.Game.Simulation.LoadGame(Guid.Empty);
            //settings.Set("LastUniverse", levelList.SelectedItem.Id.ToString());

            Player player = Manager.Game.Simulation.LoginPlayer(Guid.Empty);
            Manager.Game.Player.SetEntity(player);

            Manager.NavigateToScreen(new GameScreen(manager));
        }
    }
}
