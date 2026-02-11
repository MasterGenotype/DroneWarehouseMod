// UI/ZoneSelectionOverlay.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewModdingAPI;
using StardewValley.Menus;
using DroneWarehouseMod.Game;

namespace DroneWarehouseMod.UI
{
    /// <summary>
    /// Overlay that displays touch-friendly buttons during zone selection mode.
    /// Essential for Android/mobile users who cannot use keyboard shortcuts.
    /// </summary>
    internal sealed class ZoneSelectionOverlay
    {
        private readonly ITranslationHelper _i18n;
        private readonly DroneManager _mgr;

        // Button rectangles (recalculated each draw to handle resolution changes)
        private Rectangle _btnSize;
        private Rectangle _btnUndo;
        private Rectangle _btnStart;
        private Rectangle _btnCancel;

        private const int BtnWidth = 90;
        private const int BtnHeight = 48;
        private const int BtnPadding = 12;
        private const int PanelPadding = 16;

        public ZoneSelectionOverlay(IModHelper helper, DroneManager mgr)
        {
            _i18n = helper.Translation;
            _mgr = mgr;
        }

        /// <summary>
        /// Returns the action if the given screen position clicks a button, or null otherwise.
        /// </summary>
        public SelectionAction? GetClickedAction(int x, int y)
        {
            if (_btnSize.Contains(x, y)) return SelectionAction.CycleSize;
            if (_btnUndo.Contains(x, y)) return SelectionAction.Undo;
            if (_btnStart.Contains(x, y)) return SelectionAction.Start;
            if (_btnCancel.Contains(x, y)) return SelectionAction.Cancel;
            return null;
        }

        public void Draw(SpriteBatch b)
        {
            if (_mgr == null || !_mgr.IsSelectionActive) return;
            if (Game1.currentLocation is not Farm) return;

            var vp = Game1.uiViewport;

            // Calculate button positions at bottom center of screen
            int totalWidth = (BtnWidth * 4) + (BtnPadding * 3);
            int panelWidth = totalWidth + PanelPadding * 2;
            int panelHeight = BtnHeight + PanelPadding * 2 + 28; // Extra for hint text

            int panelX = (vp.Width - panelWidth) / 2;
            int panelY = 32; // Top of screen, same as FarmerQueuesOverlay

            var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);

            // Draw panel background
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                panel.X, panel.Y, panel.Width, panel.Height, Color.White, 1f, false
            );

            // Calculate button positions
            int btnY = panel.Y + PanelPadding;
            int btnX = panel.X + PanelPadding;

            _btnSize = new Rectangle(btnX, btnY, BtnWidth, BtnHeight);
            btnX += BtnWidth + BtnPadding;
            _btnUndo = new Rectangle(btnX, btnY, BtnWidth, BtnHeight);
            btnX += BtnWidth + BtnPadding;
            _btnStart = new Rectangle(btnX, btnY, BtnWidth, BtnHeight);
            btnX += BtnWidth + BtnPadding;
            _btnCancel = new Rectangle(btnX, btnY, BtnWidth, BtnHeight);

            // Draw buttons (convert Translation to string)
            DrawButton(b, _btnSize, _i18n.Get("selection.btn.size").ToString() + $"\n{_mgr.CurrentSelectionSizeText()}", Color.CornflowerBlue);
            DrawButton(b, _btnUndo, _i18n.Get("selection.btn.undo").ToString(), Color.Orange);
            DrawButton(b, _btnStart, _i18n.Get("selection.btn.start").ToString(), Color.LimeGreen);
            DrawButton(b, _btnCancel, _i18n.Get("selection.btn.cancel").ToString(), Color.IndianRed);

            // Draw hint text
            string hint = _i18n.Get("selection.hint.tap").ToString();
            var hintSize = Game1.smallFont.MeasureString(hint);
            Utility.drawTextWithShadow(
                b, hint, Game1.smallFont,
                new Vector2(panel.X + (panel.Width - hintSize.X) / 2, panel.Bottom - hintSize.Y - 8),
                Color.White * 0.9f
            );
        }

        private static void DrawButton(SpriteBatch b, Rectangle rect, string text, Color bgColor)
        {
            // Button background
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                rect.X, rect.Y, rect.Width, rect.Height, bgColor * 0.8f, 1f, false
            );

            // Button text (centered)
            var lines = text.Split('\n');
            float totalHeight = 0;
            foreach (var line in lines)
                totalHeight += Game1.smallFont.MeasureString(line).Y;

            float currentY = rect.Y + (rect.Height - totalHeight) / 2;
            foreach (var line in lines)
            {
                var lineSize = Game1.smallFont.MeasureString(line);
                Utility.drawTextWithShadow(
                    b, line, Game1.smallFont,
                    new Vector2(rect.X + (rect.Width - lineSize.X) / 2, currentY),
                    Color.White
                );
                currentY += lineSize.Y;
            }
        }
    }

    /// <summary>
    /// Actions that can be triggered by the zone selection overlay buttons.
    /// </summary>
    internal enum SelectionAction
    {
        CycleSize,
        Undo,
        Start,
        Cancel
    }
}
