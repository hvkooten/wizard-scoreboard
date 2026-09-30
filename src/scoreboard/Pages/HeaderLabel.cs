namespace WizardScoreboard.Pages;

// A dedicated Label subclass for the Shell header titles.
//
// It exists purely so the global "bold all text" implicit Style(typeof(Label)) cannot target the
// header: implicit styles match the exact type only, so a HeaderLabel is never affected by that
// style. The header is always bold. Because the header font (MedievalSharp) has no real bold weight,
// Android drops the synthesized bold on re-layout; HeaderLabelHandler (Platforms/Android) enforces it.
internal sealed class HeaderLabel : Label
{
}
