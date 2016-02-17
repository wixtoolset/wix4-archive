* @firegiantco via @barnson: ThmUtil cleanups.
  * ThemeControlExists should take a `const THEME*`.
  * ThemeSetTextControlEx exposed clunky `fInvalidateControl` and `fInvalidateParent` arguments in an attempt to force controls to redraw. Replace with `fUpdate` more generically and, hidden in the code, hide and show the control, which we found to be the cleanest way of getting transparent text redrawn over a graphical background.
