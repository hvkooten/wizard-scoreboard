using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class HighscorePage : ContentPage
{
    private readonly IHighscoreService highscoreService;
    private readonly IGroupService groupService;

    private readonly CollectionView gridView;

    public HighscorePage(IHighscoreService highscoreService, IGroupService groupService)
    {
        PageTitleHelper.Apply(this, Localization.GetString("Highscore"));

        this.highscoreService = highscoreService;
        this.groupService = groupService;

        gridView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            Header = CreateHeaderRow(),
            ItemTemplate = new DataTemplate(() =>
            {
                var rowGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = 40 },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = 56 },
                        new ColumnDefinition { Width = 62 },
                        new ColumnDefinition { Width = 56 }
                    },
                    ColumnSpacing = 8,
                    Padding = new Thickness(8, 10)
                };

                rowGrid.Add(CreateCellLabel(nameof(HighscoreRow.Rank), TextAlignment.Center), 0, 0);
                rowGrid.Add(CreateCellLabel(nameof(HighscoreRow.Name), TextAlignment.Start), 1, 0);
                rowGrid.Add(CreateCellLabel(nameof(HighscoreRow.Wins), TextAlignment.Center), 2, 0);
                rowGrid.Add(CreateCellLabel(nameof(HighscoreRow.Played), TextAlignment.Center), 3, 0);
                rowGrid.Add(CreateCellLabel(nameof(HighscoreRow.Best), TextAlignment.Center), 4, 0);

                return new Border
                {
                    Stroke = Colors.LightGray,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 6 },
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = rowGrid
                };
            })
        };

        Content = new StackLayout
        {
            Padding = 20,
            Children =
            {
                gridView
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshHighscores();
    }

    public void RefreshHighscores()
    {
        highscoreService.UpdateHighscores(groupService.GetGroups());
        gridView.ItemsSource = highscoreService
            .GetHighscores()
            .Select((p, index) => new HighscoreRow
            {
                Rank = index + 1,
                Name = p.Name,
                Wins = p.Wins,
                Played = p.GamesPlayed,
                Best = p.HighestScore
            })
            .ToList();
    }

    private static View CreateHeaderRow()
    {
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 40 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 56 },
                new ColumnDefinition { Width = 62 },
                new ColumnDefinition { Width = 56 }
            },
            ColumnSpacing = 8,
            Padding = new Thickness(8, 8),
            BackgroundColor = Color.FromArgb("#1a3a5c")
        };

        headerGrid.Add(CreateHeaderLabel("#", TextAlignment.Center), 0, 0);
        headerGrid.Add(CreateHeaderLabel("Name", TextAlignment.Start), 1, 0);
        headerGrid.Add(CreateHeaderLabel("Wins", TextAlignment.Center), 2, 0);
        headerGrid.Add(CreateHeaderLabel("Played", TextAlignment.Center), 3, 0);
        headerGrid.Add(CreateHeaderLabel("Best", TextAlignment.Center), 4, 0);

        return new Border
        {
            Stroke = Colors.Transparent,
            Margin = new Thickness(0, 0, 0, 8),
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Content = headerGrid
        };
    }

    private static Label CreateHeaderLabel(string text, TextAlignment alignment)
    {
        return new Label
        {
            Text = text,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            HorizontalTextAlignment = alignment,
            VerticalTextAlignment = TextAlignment.Center
        };
    }

    private static Label CreateCellLabel(string bindingPath, TextAlignment alignment)
    {
        var label = new Label
        {
            FontSize = 14,
            HorizontalTextAlignment = alignment,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        label.SetBinding(Label.TextProperty, bindingPath);
        return label;
    }

    private sealed class HighscoreRow
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Played { get; set; }
        public int Best { get; set; }
    }
}
