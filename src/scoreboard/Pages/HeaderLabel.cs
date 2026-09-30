namespace WizardScoreboard.Pages;

// A dedicated Label subclass for the Shell header titles. The global "bold all text" preference
// is registered as an implicit Style targeting typeof(Label); implicit styles only match the
// exact TargetType (ApplyToDerivedTypes is false by default), so this derived type is never
// affected by it. That keeps the header rendering identical regardless of the setting.
internal sealed class HeaderLabel : Label
{
}
