//
// This file is created by Frenetic LLC.
// This code is Copyright (C) 2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticGameCore;
using FreneticGameGraphics.ClientSystem;
using FreneticGameGraphics.GraphicsHelpers;
using OpenTK;

namespace FreneticGameGraphics.UISystem
{
    /// <summary>
    /// Represents a simple text box on a screen.
    /// </summary>
    public class UILabel : UIElement
    {
        /// <summary>
        /// The text to display on this label.
        /// </summary>
        public string Text;

        /// <summary>
        /// The font to use.
        /// </summary>
        public FontSet TextFont;

        /// <summary>
        /// The maximum width of this label.
        /// <para>Will cause text to wrap.</para>
        /// </summary>
        public Func<int> MaxX = null;

        /// <summary>
        /// The background color for this label.
        /// <para>Set to Vector4.Zero (or any values with W=0) to disable the background color.</para>
        /// </summary>
        public Vector4 BackColor = Vector4.Zero;

        /// <summary>
        /// The base text color for this label.
        /// </summary>
        public string BColor = "^r^7";

        /// <summary>
        /// Constructs a new label.
        /// </summary>
        /// <param name="btext">The text to display on the label.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="anchor">The anchor the label will be relative to.</param>
        /// <param name="xOff">The function to get the X offset.</param>
        /// <param name="yOff">The function to get the Y offset.</param>
        /// <param name="maxx">The function to get the maximum width.</param>
        public UILabel(string btext, FontSet font, UIAnchor anchor, Func<int> xOff, Func<int> yOff, Func<int> maxx = null)
            : base(anchor, () => 0, () => 0, xOff, yOff)
        {
            Text = btext;
            TextFont = font;
            Width = () => (float)TextFont.MeasureFancyLinesOfText(MaxX != null ? TextFont.SplitAppropriately(Text, MaxX()) : Text, BColor).X;
            Height = () => (float)TextFont.MeasureFancyLinesOfText(MaxX != null ? TextFont.SplitAppropriately(Text, MaxX()) : Text, BColor).Y;
            MaxX = maxx;
        }

        /// <summary>
        /// Renders this label on the screen.
        /// </summary>
        /// <param name="view">The UI view.</param>
        /// <param name="delta">The time since the last render.</param>
        /// <param name="xoff">The X offset of this label's parent.</param>
        /// <param name="yoff">The Y offset of this label's parent.</param>
        protected override void Render(ViewUI2D view, double delta, int xoff, int yoff)
        {
            string tex = MaxX != null ? TextFont.SplitAppropriately(Text, MaxX()) : Text;
            float bx = GetX() + xoff;
            float by = GetY() + yoff;
            if (BackColor.W > 0)
            {
                Location meas = TextFont.MeasureFancyLinesOfText(tex);
                view.Rendering.SetColor(BackColor);
                view.Rendering.RenderRectangle(view.UIContext, bx, by, bx + (float)meas.X, by + (float)meas.Y);
                view.Rendering.SetColor(Vector4.One);
            }
            TextFont.DrawColoredText(tex, new Location(bx, by, 0), bcolor: BColor);
        }
    }
}
