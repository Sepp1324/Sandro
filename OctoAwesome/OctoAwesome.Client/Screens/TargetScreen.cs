﻿using engenious;
using engenious.Graphics;
using engenious.UI;
using engenious.UI.Controls;
using OctoAwesome.Client.Components;
using System;

namespace OctoAwesome.Client.Screens
{
    internal sealed class TargetScreen : Screen
    {
        private readonly AssetComponent _assets;

        public TargetScreen(ScreenComponent manager, Action<int, int> tp, int x, int y) : base(manager)
        {
            _assets = manager.Game.Assets;

            IsOverlay = true;
            Background = new BorderBrush(Color.Black * 0.5f);
            Title = Languages.OctoClient.SelectTarget;

            var panelBackground = _assets.LoadTexture(typeof(ScreenComponent), "panel");
            var panel = new Panel(manager)
            {
                Background = NineTileBrush.FromSingleTexture(panelBackground, 30, 30),
                Padding = Border.All(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Controls.Add(panel);

            var stackPanel = new StackPanel(manager);
            panel.Controls.Add(stackPanel);

            var headLine = new Label(manager)
            {
                Text = Title,
                Font = Skin.Current.HeadlineFont,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            stackPanel.Controls.Add(headLine);

            var vStack = new StackPanel(manager) {Orientation = Orientation.Vertical};
            stackPanel.Controls.Add(vStack);

            var xStack = new StackPanel(manager) {Orientation = Orientation.Horizontal};
            vStack.Controls.Add(xStack);

            var xLabel = new Label(manager) {Text = "X:"};
            xStack.Controls.Add(xLabel);

            var xText = new Textbox(manager)
            {
                Background = new BorderBrush(Color.Gray),
                Width = 150,
                Margin = new Border(2, 10, 2, 10),
                Text = x.ToString()
            };
            xStack.Controls.Add(xText);

            var yStack = new StackPanel(manager) {Orientation = Orientation.Horizontal};
            vStack.Controls.Add(yStack);

            var yLabel = new Label(manager) {Text = "Y:"};
            yStack.Controls.Add(yLabel);

            var yText = new Textbox(manager)
            {
                Background = new BorderBrush(Color.Gray),
                Width = 150,
                Margin = new Border(2, 10, 2, 10),
                Text = y.ToString()
            };
            yStack.Controls.Add(yText);

            Button closeButton = new TextButton(manager, Languages.OctoClient.Teleport)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            closeButton.LeftMouseClick += (s, e) =>
            {
                if (tp != null)
                    tp(int.Parse(xText.Text), int.Parse(yText.Text));
                else
                    manager.NavigateBack();
            };
            stackPanel.Controls.Add(closeButton);

            KeyDown += (s, e) =>
            {
                if (e.Key == engenious.Input.Keys.Escape)
                    manager.NavigateBack();
                e.Handled = true;
            };
        }
    }
}
