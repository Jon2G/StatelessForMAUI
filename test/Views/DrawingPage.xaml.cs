﻿using CommunityToolkit.Maui.Views;
using StatelessForMAUI.Attributes;

namespace SampleApp.Views;
[StatelessNavigation(GoBackTarget: typeof(MainPage))]
public partial class DrawingPage : ContentPage
{
    public DrawingPage()
    {
        InitializeComponent();
        BindingContext = new DrawingViewModel();
    }

    private async void SaveClicked(object sender, EventArgs e)
    {
        var drawingLines = (BindingContext as DrawingViewModel)?.Lines.ToList();

        if (drawingLines is null || drawingLines.Count < 1)
        {
            return;
        }

        var points = drawingLines.SelectMany(x => x.Points).ToList();

        var stream = await DrawingView.GetImageStream(
            drawingLines,
            new Size(points.Max(x => x.X) - points.Min(x => x.X), points.Max(x => x.Y) - points.Min(x => x.Y)),
            Colors.Gray);

        GeneratedImage.Source = ImageSource.FromStream(() => stream);
    }
}
