using System.Collections.Generic;
using System.Windows.Forms;

namespace BGSK1.UI
{
    internal static class GridHeaderMap
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Maps = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "equipment",
                new Dictionary<string, string>
                {
                    { "InventoryNumber", "Инвентарный номер" }, { "Name", "Наименование" }, { "TypeName", "Тип" },
                    { "LocationName", "Локация" }, { "ResponsiblePerson", "Ответственный" }, { "StatusName", "Статус" },
                    { "PurchaseDate", "Дата покупки" }, { "WarrantyUntil", "Гарантия до" }
                }
            },
            {
                "requests",
                new Dictionary<string, string>
                {
                    { "RequestNumber", "Номер заявки" }, { "CreatedAt", "Дата" }, { "InventoryNumber", "Инв. номер" },
                    { "EquipmentName", "Оборудование" }, { "ProblemDescription", "Неисправность" }, { "PriorityName", "Приоритет" },
                    { "StatusName", "Статус" }, { "AssignedTo", "Исполнитель" }, { "CompletedAt", "Завершено" }
                }
            },
            {
                "requestParts",
                new Dictionary<string, string>
                {
                    { "PartName", "Запчасть" }, { "PartNumber", "Артикул" }, { "QuantityUsed", "Количество" }
                }
            },
            {
                "maintenance",
                new Dictionary<string, string>
                {
                    { "InventoryNumber", "Инв. номер" }, { "EquipmentName", "Оборудование" }, { "MaintenanceType", "Вид ТО" },
                    { "PeriodDays", "Период (дн.)" }, { "NextDate", "Следующее ТО" }, { "ResponsiblePerson", "Ответственный" }
                }
            },
            {
                "parts",
                new Dictionary<string, string>
                {
                    { "PartName", "Запчасть" }, { "PartNumber", "Артикул" }, { "QuantityInStock", "Остаток" },
                    { "MinQuantity", "Минимум" }, { "LastUpdated", "Обновлено" }
                }
            },
            {
                "backups",
                new Dictionary<string, string>
                {
                    { "FileName", "Имя файла" }, { "FilePath", "Путь к файлу" }, { "SizeBytes", "Размер (байт)" },
                    { "CreationDate", "Дата создания" }, { "Comment", "Комментарий" }, { "IsAuto", "Автоматический" }
                }
            },
            {
                "reportData",
                new Dictionary<string, string>
                {
                    { "InventoryNumber", "Инвентарный номер" }, { "EquipmentName", "Оборудование" }, { "TypeName", "Тип" },
                    { "LocationName", "Локация" }, { "ResponsiblePerson", "Ответственный" }, { "StatusName", "Статус" },
                    { "RequestsTotal", "Всего заявок" }, { "RequestsOpen", "Открытых заявок" }, { "LastRequestDate", "Последняя заявка" },
                    { "LastMaintenanceDate", "Последнее ТО" }, { "NextMaintenanceDate", "Следующее ТО" }, { "OverduePlansCount", "Просрочено планов" },
                    { "PriorityName", "Приоритет" }, { "AssignedTo", "Исполнитель" }, { "RequestsClosed", "Закрыто заявок" },
                    { "AvgResolveHours", "Среднее время, ч" }, { "MaxResolveHours", "Макс. время, ч" },
                    { "MaintenanceType", "Вид ТО" }, { "PeriodDays", "Период (дн.)" }, { "NextDate", "Плановая дата ТО" },
                    { "LastPerformedAt", "Последнее выполнение" }, { "OverdueDays", "Дней просрочки" }, { "ComplianceStatus", "Статус контроля" },
                    { "PartName", "Запчасть" }, { "PartNumber", "Артикул" },
                    { "QuantityInStock", "Остаток на складе" }, { "MinQuantity", "Мин. остаток" }, { "UsedInPeriod", "Расход за период" },
                    { "ForecastNeed", "Потребность (прогноз)" }, { "RecommendedOrderQty", "Рекомендовано к заказу" }, { "StockRisk", "Риск запаса" }
                }
            },
            {
                "reportHistory",
                new Dictionary<string, string>
                {
                    { "ReportName", "Название отчета" }, { "ReportType", "Формат" }, { "CreatedAt", "Создан" },
                    { "CreatedBy", "Пользователь" }, { "FilePath", "Путь к файлу" }
                }
            },
            {
                "users",
                new Dictionary<string, string>
                {
                    { "Email", "Логин" }, { "FullName", "ФИО" }, { "RoleName", "Роль" },
                    { "RegistrationDate", "Дата регистрации" }
                }
            },
            {
                "dashboardRecentRequests",
                new Dictionary<string, string>
                {
                    { "Id", "Код" },
                    { "RequestNumber", "Номер заявки" }, { "CreatedAt", "Дата создания" },
                    { "StatusName", "Статус" }, { "PriorityName", "Приоритет" }, { "AssignedTo", "Исполнитель" }
                }
            }
        };

        public static void Apply(DataGridView grid, string mapKey, params string[] hiddenColumns)
        {
            if (grid == null || !Maps.ContainsKey(mapKey))
            {
                return;
            }

            foreach (var hidden in hiddenColumns)
            {
                if (grid.Columns.Contains(hidden))
                {
                    grid.Columns[hidden].Visible = false;
                }
            }

            var map = Maps[mapKey];
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (map.ContainsKey(column.Name))
                {
                    column.HeaderText = map[column.Name];
                }
                else if (column.Visible)
                {
                    column.Visible = false;
                }
            }
        }
    }
}
