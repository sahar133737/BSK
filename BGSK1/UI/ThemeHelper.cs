using System.Drawing;
using System.Windows.Forms;

namespace BGSK1.UI
{
    internal static class ThemeHelper
    {
        /// <summary>Высота подписи поля; фиксированная, чтобы текст не наезжал на TextBox.</summary>
        public const int FormFieldLabelHeight = 20;

        /// <summary>Подпись над полем ввода: одна строка, без перекрытия контрола снизу.</summary>
        public static Label FormFieldLabel(string text, int left, int top, int width, Color? foreColor = null)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = FormFieldLabelHeight,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = foreColor ?? MutedText
            };
        }

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
            form.Load += (s, e) => ApplyMinimalistTheme(form);
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

        public static void ApplyMinimalistTheme(Control root)
        {
            if (root is Form form)
            {
                form.BackColor = Surface;
                form.ForeColor = Text;
            }

            foreach (Control child in root.Controls)
            {
                if (child is DataGridView grid)
                {
                    StyleGrid(grid);
                }
                else if (child is GroupBox group)
                {
                    group.ForeColor = Text;
                    group.BackColor = Color.White;
                    group.Padding = new Padding(10);
                }
                else if (child is Panel panel)
                {
                    if (panel.Dock != DockStyle.Left && panel.Dock != DockStyle.Top)
                    {
                        panel.BackColor = Surface;
                    }
                }
                else if (child is Label label)
                {
                    label.ForeColor = IsDarkBackground(label.Parent?.BackColor ?? Surface) ? Color.White : Text;
                }
                else if (child is TextBox textBox)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = Color.White;
                    textBox.ForeColor = Text;
                }
                else if (child is ComboBox comboBox)
                {
                    comboBox.BackColor = Color.White;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                }
                else if (child is DateTimePicker datePicker)
                {
                    datePicker.CalendarForeColor = Text;
                    datePicker.CalendarMonthBackground = Color.White;
                }
                else if (child is NumericUpDown numeric)
                {
                    numeric.BackColor = Color.White;
                    numeric.ForeColor = Text;
                }
                else if (child is CheckBox checkBox)
                {
                    checkBox.ForeColor = IsDarkBackground(checkBox.Parent?.BackColor ?? Surface) ? Color.White : Text;
                }
                else if (child is Button button)
                {
                    if (button.BackColor == SystemColors.Control || button.BackColor == default(Color))
                    {
                        StyleButton(button, Primary);
                    }
                }

                if (child.HasChildren)
                {
                    ApplyMinimalistTheme(child);
                }
            }
        }

        private static bool IsDarkBackground(Color color)
        {
            var brightness = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
            return brightness < 140;
        }
    }
}
