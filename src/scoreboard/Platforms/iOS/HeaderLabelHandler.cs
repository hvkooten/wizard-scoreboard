using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace WizardScoreboard.Pages;

// iOS handler for HeaderLabel. MedievalSharp has no real bold weight, so UIKit renders it regular.
// A negative stroke width fills and outlines the glyphs, which synthesizes bold like Android's fake bold.
internal sealed class HeaderLabelHandler : LabelHandler
{
    private const float FakeBoldStrokeWidth = -3f;

    private static readonly PropertyMapper<ILabel, ILabelHandler> HeaderMapper = CreateMapper();

    public HeaderLabelHandler() : base(HeaderMapper)
    {
    }

    private static PropertyMapper<ILabel, ILabelHandler> CreateMapper()
    {
        var mapper = new PropertyMapper<ILabel, ILabelHandler>(Mapper);
        foreach (var key in new[] { nameof(ILabel.Text), nameof(ILabel.Font), nameof(ILabel.TextColor), nameof(ILabel.CharacterSpacing) })
        {
            mapper.AppendToMapping(key, ApplyFakeBold);
        }

        return mapper;
    }

    private static void ApplyFakeBold(ILabelHandler handler, ILabel label)
    {
        var view = handler.PlatformView;
        if (string.IsNullOrEmpty(view.Text))
        {
            return;
        }

        var attributes = new UIStringAttributes
        {
            Font = view.Font,
            ForegroundColor = view.TextColor,
            StrokeColor = view.TextColor,
            StrokeWidth = FakeBoldStrokeWidth,
            KerningAdjustment = (float)label.CharacterSpacing
        };
        view.AttributedText = new NSAttributedString(view.Text, attributes);
    }
}
