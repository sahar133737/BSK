using System.Drawing;
using System.Windows.Forms;

namespace BGSK1.UI
{
    internal static class ThemeHelper
    {
        // Palette aligned with the college website style: deep blue + light surfaces.
        public static readonly Color DarkBg = Color.FromArgb(10, 52, 96);
        public static readonly Color PanelBg = Color.FromArgb(15, 67, 125);
        public static readonly Color Surface = Color.FromArgb(248, 251, 255);
        public static readonly Color Header = Color.FromArgb(226, 238, 250);
        public static readonly Color Text = Color.FromArgb(20, 43, 73);
        public static readonly Color MutedText = Color.FromArgb(85, 106, 133);
        public static readonly Color Primary = Color.FromArgb(0, 90, 169);
        public static readonly Color Secondary = Color.FromArgb(34, 122, 194);
        public static readonly Color Accent = Color.FromArgb(245, 166, 35);
        public static readonly Color Danger = Color.FromArgb(201, 48, 44);
        public static readonly Color Success = Color.FromArgb(44, 133, 89);

        public static void ApplyForm(Form form, string title)
        {
            form.Text = title;
            form.BackColor = Surface;
            form.Font = new Font("Segoe UI", 10f);
            form.StartPosition = FormStartPosition.CenterParent;
        }

        public static void StyleButton(Button button, Color backColor)
        {
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08f);
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.04f);
            button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Header;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(207, 228, 249);
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.BackColor = Color.White;
        }
    }
}
