using System;
using System.Drawing;
using System.Windows.Forms;
using BGSK1.Services;

namespace BGSK1.UI
{
    internal static class LookupUiHelper
    {
        /// <summary>Кнопка «+» для добавления значения в справочник.</summary>
        public static Button CreateAddLookupButton(int left, int top, string toolTip, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = "+",
                Left = left,
                Top = top,
                Width = 28,
                Height = 26,
                TabStop = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            ThemeHelper.StyleButton(btn, ThemeHelper.Secondary);
            btn.Click += onClick;
            if (!string.IsNullOrEmpty(toolTip))
            {
                var tip = new ToolTip();
                tip.SetToolTip(btn, toolTip);
            }

            return btn;
        }

        /// <summary>Диалог ввода и запись в dbo.LookupDictionary. Возвращает добавленное значение (уже обрезанное).</summary>
        public static bool TryPromptAndAddValue(IWin32Window owner, string category, string dialogTitle, out string addedValue)
        {
            addedValue = null;
            using (var f = new Form())
            {
                f.Text = dialogTitle;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.ShowInTaskbar = false;
                f.AutoScaleMode = AutoScaleMode.Font;
                f.Font = new Font("Segoe UI", 10f);
                f.BackColor = ThemeHelper.Surface;
                f.ClientSize = new Size(400, 130);

                var lbl = new Label
                {
                    Left = 14,
                    Top = 14,
                    Width = 370,
                    Height = 40,
                    Text = "Новое значение появится в списке выбора во всех формах этого раздела.",
                    ForeColor = ThemeHelper.MutedText
                };
                var txt = new TextBox { Left = 14, Top = 56, Width = 370 };
                var ok = new Button { Text = "Добавить", Left = 210, Top = 90, Width = 85, Height = 28, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Отмена", Left = 299, Top = 90, Width = 85, Height = 28, DialogResult = DialogResult.Cancel };
                ThemeHelper.StyleButton(ok, ThemeHelper.Primary);
                ThemeHelper.StyleButton(cancel, ThemeHelper.Secondary);
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                if (f.ShowDialog(owner) != DialogResult.OK)
                {
                    return false;
                }

                var v = txt.Text.Trim();
                if (string.IsNullOrEmpty(v))
                {
                    MessageBox.Show(f, "Введите непустое значение.", "Справочник", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                try
                {
                    LookupDictionaryService.AddValue(category, v);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(f, ex.Message, "Не удалось сохранить", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                addedValue = v;
                return true;
            }
        }
    }
}
