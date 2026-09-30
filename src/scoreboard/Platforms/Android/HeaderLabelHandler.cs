using Android.Content;
using Android.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace WizardScoreboard.Pages;

// Android handler for HeaderLabel. MedievalSharp has no real bold weight, and Android resets the
// typeface (dropping the synthesized bold) on various re-layouts. The native view below enforces
// bold on every typeface assignment, so the header can never flip back to non-bold.
internal sealed class HeaderLabelHandler : LabelHandler
{
    protected override MauiTextView CreatePlatformView() => new BoldTextView(Context);

    private sealed class BoldTextView(Context context) : MauiTextView(context)
    {
        public override Typeface? Typeface
        {
            get => base.Typeface;
            set
            {
                base.Typeface = Typeface.Create(value, TypefaceStyle.Bold);
                Paint.FakeBoldText = true;
            }
        }
    }
}
