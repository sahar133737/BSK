using System.Collections.Generic;
using System.Windows.Forms;

namespace BGSK1.UI
{
    internal static class ModuleHelpProvider
    {
        private const string VkContact = "vk.com/id695275682";

        private static readonly Dictionary<string, string> HelpTexts = new Dictionary<string, string>
        {
            { "equipment", "Модуль \"Техника\": учет оборудования, его статусов, локаций и ответственных. Используйте для добавления и актуализации карточек техники." },
            { "requests", "Модуль \"Заявки\": регистрация неисправностей, назначение исполнителя, контроль статусов и расхода запчастей по каждой заявке." },
            { "maintenance", "Модуль \"Плановое ТО\": ведение графиков обслуживания, сроков следующего ТО и ответственных лиц." },
            { "parts", "Модуль \"Склад запчастей\": учет остатков, минимальных запасов, списания и пополнения номенклатуры." },
            { "reports", "Модуль \"Отчеты\": формирование аналитических отчетов по технике, заявкам, ТО и запасам с экспортом и печатью." },
            { "backup", "Модуль \"Резервные копии\": создание и восстановление резервных копий базы данных приложения." },
            { "users", "Модуль \"Пользователи\": создание учетных записей, назначение ролей, управление активностью и сброс паролей." },
            { "admin", "Модуль \"Администрирование прав\": настройка доступа ролей к разделам системы." },
            { "mainmenu", "Главное меню: быстрый вход в модули и сводка по ключевым показателям системы (заявки, ТО, запчасти)." }
        };

        public static void ShowHelp(string moduleKey, IWin32Window owner)
        {
            string text;
            if (!HelpTexts.TryGetValue(moduleKey, out text))
            {
                text = "Справка для этого модуля пока не заполнена.";
            }

            MessageBox.Show(
                owner,
                text + "\n\nДля связи: " + VkContact,
                "Справка по модулю",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public static void BindF11(Form form, string moduleKey)
        {
            if (form == null)
            {
                return;
            }

            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.F11)
                {
                    return;
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
                ShowHelp(moduleKey, form);
            };
        }
    }
}
