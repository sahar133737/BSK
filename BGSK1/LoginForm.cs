using System;
using System.Drawing;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class LoginForm : Form
    {
        private readonly TextBox _txtEmail;
        private readonly TextBox _txtPassword;
        private readonly Button _btnLogin;
        private readonly Label _lblError;

        public LoginForm()
        {
            Text = "Вход в систему";
            Width = 520;
            Height = 340;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ThemeHelper.DarkBg;
            Font = new Font("Segoe UI", 10f);

            var card = new Panel
            {
                Width = 420,
                Height = 250,
                Left = 45,
                Top = 34,
                BackColor = Color.White
            };

            var lblTitle = new Label
            {
                Left = 24,
                Top = 18,
                Width = 360,
                Text = "BGSK1 Database Control",
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
                ForeColor = ThemeHelper.Text
            };

            var lblEmail = new Label { Left = 24, Top = 64, Width = 120, Text = "Email:", ForeColor = ThemeHelper.MutedText };
            _txtEmail = new TextBox { Left = 24, Top = 86, Width = 370, Text = "admin@bgsk.local", BorderStyle = BorderStyle.FixedSingle };

            var lblPassword = new Label { Left = 24, Top = 118, Width = 120, Text = "Пароль:", ForeColor = ThemeHelper.MutedText };
            _txtPassword = new TextBox { Left = 24, Top = 140, Width = 370, PasswordChar = '*', Text = "Admin123!", BorderStyle = BorderStyle.FixedSingle };

            _btnLogin = new Button
            {
                Left = 24,
                Top = 176,
                Width = 370,
                Height = 36,
                Text = "Войти",
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.Primary,
                ForeColor = Color.White
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += BtnLogin_Click;

            _lblError = new Label { Left = 24, Top = 218, Width = 380, Height = 26, ForeColor = Color.Firebrick };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblEmail);
            card.Controls.Add(_txtEmail);
            card.Controls.Add(lblPassword);
            card.Controls.Add(_txtPassword);
            card.Controls.Add(_btnLogin);
            card.Controls.Add(_lblError);

            Controls.Add(card);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var email = _txtEmail.Text.Trim();
            var password = _txtPassword.Text;
            var ip = "127.0.0.1";

            if (!UserService.IsValidEmail(email))
            {
                _lblError.Text = "Введите корректный email.";
                return;
            }

            if (AuthService.TryLogin(email, password, ip, out var error))
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _lblError.Text = error;
        }
    }
}
