using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RawAccelModern
{
    internal static class AppDialog
    {
        internal static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            return Show(Application.Current == null ? null : Application.Current.MainWindow, message, title, buttons, image);
        }

        internal static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            MainWindow mainWindow = owner as MainWindow;
            if (mainWindow == null && Application.Current != null) mainWindow = Application.Current.MainWindow as MainWindow;
            bool portuguese = mainWindow != null && mainWindow.IsPortugueseLanguage;
            MessageBoxResult result = MessageBoxResult.None;

            Brush surface = ResourceBrush(owner, "SurfaceBrush", "#0B1627");
            Brush card = ResourceBrush(owner, "CardBrush", "#0E1A2C");
            Brush border = ResourceBrush(owner, "BorderBrush", "#22334D");
            Brush text = ResourceBrush(owner, "TextBrush", "#E7EDF6");
            Brush muted = ResourceBrush(owner, "MutedBrush", "#93A2B8");
            Brush accent = ResourceBrush(owner, "AccentBrush", "#1594FB");
            Brush control = ResourceBrush(owner, "ControlBrush", "#111D31");

            string iconText = "i";
            Color iconColor = Color.FromRgb(21, 148, 251);
            if (image == MessageBoxImage.Question) { iconText = "?"; iconColor = Color.FromRgb(21, 148, 251); }
            else if (image == MessageBoxImage.Warning) { iconText = "!"; iconColor = Color.FromRgb(255, 188, 82); }
            else if (image == MessageBoxImage.Error) { iconText = "×"; iconColor = Color.FromRgb(255, 91, 106); }
            else if (image == MessageBoxImage.Information) { iconText = "i"; iconColor = Color.FromRgb(66, 214, 255); }

            Window dialog = new Window
            {
                Title = String.IsNullOrWhiteSpace(title) ? "Raw Accel Reimagined" : title,
                Width = 620,
                SizeToContent = SizeToContent.Height,
                MaxHeight = 680,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                Owner = owner,
                FontFamily = new FontFamily("Segoe UI")
            };

            Border shell = new Border
            {
                Background = surface,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(18),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.48, Color = Colors.Black }
            };
            dialog.Content = shell;

            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.Child = layout;

            Border accentBar = new Border { Background = new SolidColorBrush(iconColor), CornerRadius = new CornerRadius(16, 16, 0, 0) };
            layout.Children.Add(accentBar);

            Grid header = new Grid { Margin = new Thickness(26, 20, 20, 14) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            Grid.SetRow(header, 1);
            layout.Children.Add(header);

            Border iconCircle = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(21),
                Background = new SolidColorBrush(Color.FromArgb(35, iconColor.R, iconColor.G, iconColor.B)),
                BorderBrush = new SolidColorBrush(iconColor),
                BorderThickness = new Thickness(1)
            };
            iconCircle.Child = new TextBlock
            {
                Text = iconText,
                Foreground = new SolidColorBrush(iconColor),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(iconCircle);

            TextBlock titleBlock = new TextBlock
            {
                Text = dialog.Title,
                Foreground = text,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(titleBlock, 1);
            header.Children.Add(titleBlock);

            Button closeButton = CreateButton("×", control, muted, border, false);
            closeButton.Width = 34;
            closeButton.Height = 32;
            closeButton.Padding = new Thickness(0);
            closeButton.Click += delegate { dialog.Close(); };
            Grid.SetColumn(closeButton, 2);
            header.Children.Add(closeButton);

            Border messageCard = new Border
            {
                Background = card,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 15, 18, 15),
                Margin = new Thickness(26, 0, 26, 0),
                MinHeight = 86,
                MaxHeight = 390
            };
            Grid.SetRow(messageCard, 2);
            layout.Children.Add(messageCard);
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            messageCard.Child = scroll;
            scroll.Content = new TextBlock
            {
                Text = message ?? String.Empty,
                Foreground = text,
                FontSize = 14,
                LineHeight = 22,
                TextWrapping = TextWrapping.Wrap
            };

            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(26, 18, 26, 24)
            };
            Grid.SetRow(actions, 3);
            layout.Children.Add(actions);

            if (buttons == MessageBoxButton.OK)
                actions.Children.Add(ResultButton("OK", MessageBoxResult.OK, true, dialog, delegate(MessageBoxResult value) { result = value; }, control, text, border, accent));
            else if (buttons == MessageBoxButton.OKCancel)
            {
                actions.Children.Add(ResultButton(portuguese ? "Cancelar" : "Cancel", MessageBoxResult.Cancel, false, dialog, delegate(MessageBoxResult value) { result = value; }, control, text, border, accent));
                actions.Children.Add(ResultButton("OK", MessageBoxResult.OK, true, dialog, delegate(MessageBoxResult value) { result = value; }, control, text, border, accent));
            }
            else if (buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel)
            {
                if (buttons == MessageBoxButton.YesNoCancel)
                    actions.Children.Add(ResultButton(portuguese ? "Cancelar" : "Cancel", MessageBoxResult.Cancel, false, dialog, delegate(MessageBoxResult value) { result = value; }, control, text, border, accent));
                actions.Children.Add(ResultButton(portuguese ? "Não" : "No", MessageBoxResult.No, false, dialog, delegate(MessageBoxResult value) { result = value; }, control, text, border, accent));
                actions.Children.Add(ResultButton(portuguese ? "Sim" : "Yes", MessageBoxResult.Yes, true, dialog, delegate(MessageBoxResult value) { result = value; }, control, text, border, accent));
            }

            dialog.PreviewKeyDown += delegate(object sender, KeyEventArgs args)
            {
                if (args.Key == Key.Escape) { args.Handled = true; dialog.Close(); }
            };
            header.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs args)
            {
                if (args.LeftButton == MouseButtonState.Pressed) dialog.DragMove();
            };
            dialog.ShowDialog();
            return result;
        }

        private static Button ResultButton(string label, MessageBoxResult value, bool primary, Window dialog,
            Action<MessageBoxResult> setResult, Brush control, Brush text, Brush border, Brush accent)
        {
            Button button = CreateButton(label, primary ? accent : control, primary ? Brushes.White : text, primary ? accent : border, primary);
            button.MinWidth = 112;
            button.Height = 42;
            button.Margin = new Thickness(10, 0, 0, 0);
            button.IsDefault = primary;
            button.Click += delegate
            {
                setResult(value);
                dialog.Close();
            };
            return button;
        }

        private static Button CreateButton(string label, Brush background, Brush foreground, Brush border, bool bold)
        {
            Button button = new Button
            {
                Content = label,
                Background = background,
                Foreground = foreground,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 8, 14, 8),
                FontSize = 13,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Cursor = Cursors.Hand
            };
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory chrome = new FrameworkElementFactory(typeof(Border));
            chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
            chrome.AppendChild(content);
            template.VisualTree = chrome;
            button.Template = template;
            button.MouseEnter += delegate { button.Opacity = 0.86; };
            button.MouseLeave += delegate { button.Opacity = 1.0; };
            return button;
        }

        private static Brush ResourceBrush(Window owner, string key, string fallback)
        {
            object resource = owner == null ? null : owner.TryFindResource(key);
            Brush brush = resource as Brush;
            return brush ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));
        }
    }
}
