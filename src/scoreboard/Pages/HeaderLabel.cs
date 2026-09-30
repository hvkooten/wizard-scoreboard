namespace WizardScoreboard.Pages;

// A dedicated Label subclass for the Shell header titles.
//
// It exists purely so the global "bold all text" implicit Style(typeof(Label)) cannot target the
// header: implicit styles match the exact type only, so a HeaderLabel is never affected by that
// style. The header intentionally does NOT use FontAttributes.Bold, because the header font
// (MedievalSharp) has no real bold weight and the synthesized ("faux") bold is re-rasterized
// inconsistently whenever the title view re-measures (e.g. when the setting is toggled), causing a
// visible weight flicker. The header stays visually distinct through its size, color and glow
// instead, which do not depend on font-weight synthesis.
internal sealed class HeaderLabel : Label
{
}
