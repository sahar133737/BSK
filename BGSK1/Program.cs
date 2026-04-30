using System;
using System.Windows.Forms;
using BGSK1.Services;

namespace BGSK1
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                DatabaseBootstrapper.EnsureDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка инициализации БД: {ex.Message}",
                    "Ошибка запуска",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            RolePermissionService.LoadCurrentRolePermissions();
            Application.Run(new MainMenuForm());
        }
    }
}
