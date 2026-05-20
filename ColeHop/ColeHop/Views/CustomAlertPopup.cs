using ColeHop.Services.Alert;

namespace ColeHop.Views
{
    public class CustomAlertPopup : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        private readonly TaskCompletionSource _dismissedTcs = new();

        private static string GetIconGlyph(AlertIcon alertIcon) => alertIcon switch
        {
            AlertIcon.Error => "\ue000",
            AlertIcon.Warning => "\ue002",
            AlertIcon.Success => "\ue86c",
            AlertIcon.Info => "\ue88e",
            AlertIcon.AccessDenied => "\ue899",
            AlertIcon.NotAuthorized => "\ue5cd",
            AlertIcon.Timeout => "\ue425",
            AlertIcon.PickupAuthorized => "\ue876",
            _ => "\ue88e"
        };

        private enum IconCategory { Info, Success, Warning, Error }

        private static IconCategory GetIconCategory(AlertIcon alertIcon) => alertIcon switch
        {
            AlertIcon.Error or AlertIcon.AccessDenied or AlertIcon.NotAuthorized => IconCategory.Error,
            AlertIcon.Success or AlertIcon.PickupAuthorized => IconCategory.Success,
            AlertIcon.Warning or AlertIcon.Timeout => IconCategory.Warning,
            _ => IconCategory.Info
        };

        public CustomAlertPopup(string title, string message, string cancelText, string? acceptText, AlertIcon alertIcon = AlertIcon.Info)
        {
            BackgroundColor = Color.FromArgb("#80000000");
            Shell.SetNavBarIsVisible(this, false);
            Shell.SetTabBarIsVisible(this, false);

            var isDark = Application.Current!.RequestedTheme == AppTheme.Dark;

            var icon = GetIconGlyph(alertIcon);
            var category = GetIconCategory(alertIcon);

            var iconColor = GetThemeColor(isDark, "PrimaryDark", "Primary");
            var iconBgColor = GetThemeColor(isDark, "InfoBgDark", "InfoBgLight");

            if (category == IconCategory.Error)
            {
                iconColor = (Color)Application.Current.Resources["Error"];
                iconBgColor = GetThemeColor(isDark, "ErrorBgDark", "ErrorBgLight");
            }
            else if (category == IconCategory.Success)
            {
                iconColor = (Color)Application.Current.Resources["Success"];
                iconBgColor = GetThemeColor(isDark, "SuccessBgDark", "SuccessBgLight");
            }
            else if (category == IconCategory.Warning)
            {
                iconColor = (Color)Application.Current.Resources["Accent"];
                iconBgColor = GetThemeColor(isDark, "WarningBgDark", "WarningBgLight");
            }

            var iconLabel = new Label
            {
                Text = icon,
                FontFamily = "MaterialSymbolsRounded",
                FontSize = 28,
                TextColor = iconColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var iconBorder = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = iconBgColor,
                HeightRequest = 56,
                WidthRequest = 56,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 28 },
                HorizontalOptions = LayoutOptions.Center,
                Content = iconLabel
            };

            var titleLabel = new Label
            {
                Text = title,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var messageLabel = new Label
            {
                Text = message,
                FontSize = 15,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var cancelButton = new Button
            {
                Text = cancelText,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 16,
                HeightRequest = 48,
                BorderWidth = 0
            };
            cancelButton.Clicked += OnCancelClicked;

            Grid buttonGrid;

            if (string.IsNullOrEmpty(acceptText))
            {
                cancelButton.BackgroundColor = GetThemeColor(isDark, "PrimaryDark", "Primary");
                cancelButton.TextColor = isDark ? GetThemeColor(true, "PrimaryDarkText", "PrimaryDarkText") : Colors.White;

                buttonGrid = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star) },
                    Margin = new Thickness(0, 8, 0, 0)
                };
                buttonGrid.Add(cancelButton, 0);
            }
            else
            {
                cancelButton.BackgroundColor = GetThemeColor(isDark, "SurfaceDimDark", "Surface");
                cancelButton.TextColor = GetThemeColor(isDark, "PrimaryDark", "Primary");

                var acceptButton = new Button
                {
                    Text = acceptText,
                    BackgroundColor = GetThemeColor(isDark, "PrimaryDark", "Primary"),
                    TextColor = isDark ? GetThemeColor(true, "PrimaryDarkText", "PrimaryDarkText") : Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 16,
                    HeightRequest = 48,
                    BorderWidth = 0
                };
                acceptButton.Clicked += OnAcceptClicked;

                buttonGrid = new Grid
                {
                    ColumnSpacing = 12,
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                    Margin = new Thickness(0, 8, 0, 0)
                };
                buttonGrid.Add(cancelButton, 0);
                buttonGrid.Add(acceptButton, 1);
            }

            var stack = new VerticalStackLayout
            {
                Spacing = 16,
                Children = { iconBorder, titleLabel, messageLabel, buttonGrid }
            };

            var cardBackground = GetThemeColor(isDark, "CardBackgroundDark", "CardBackgroundLight");

            var card = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = cardBackground,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
                Padding = 28,
                MinimumWidthRequest = 300,
                MaximumWidthRequest = 360,
                Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#60000000")), Offset = new Point(0, 6), Radius = 28, Opacity = 0.5f },
                Content = stack
            };

            Content = new Grid
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Padding = 24,
                Children = { card }
            };
        }

        public async Task WaitForDismissAsync()
        {
            await _tcs.Task;
            await _dismissedTcs.Task;
        }

        public async Task<bool> WaitForConfirmAsync()
        {
            var result = await _tcs.Task;
            await _dismissedTcs.Task;
            return result;
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            try { await Navigation.PopModalAsync(animated: false); } catch (InvalidOperationException) { }
            _dismissedTcs.TrySetResult();
        }

        private async void OnAcceptClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(true);
            try { await Navigation.PopModalAsync(animated: false); } catch (InvalidOperationException) { }
            _dismissedTcs.TrySetResult();
        }

        private static Color GetThemeColor(bool isDark, string darkKey, string lightKey)
        {
            var key = isDark ? darkKey : lightKey;
            if (Application.Current!.Resources.TryGetValue(key, out var value) && value is Color color)
                return color;
            return Colors.Transparent;
        }
    }
}
