using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportDecoratorPractice
{
    public class ReportRecord
    {
        public DateTime Date { get; set; }
        public double Amount { get; set; }
        public string UserId { get; set; }
        public string Extra { get; set; } 

        public override string ToString() =>
            $"{Date:yyyy-MM-dd} | User:{UserId} | Amount:{Amount} | {Extra}";
    }

    public interface IReport
    {
        string Generate(); 
        List<ReportRecord> GetRecords(); 
    }

    public class SalesReport : IReport
    {
        protected List<ReportRecord> _records;

        public SalesReport()
        {
            _records = new List<ReportRecord>();
            var rnd = new Random();
            for (int i = 0; i < 12; i++)
            {
                _records.Add(new ReportRecord
                {
                    Date = DateTime.Now.Date.AddDays(-rnd.Next(0, 90)),
                    Amount = Math.Round(100 + rnd.NextDouble() * 900, 2),
                    UserId = "U" + rnd.Next(1, 6),
                    Extra = "Product#" + rnd.Next(1, 10)
                });
            }
        }

        public virtual List<ReportRecord> GetRecords() => _records;

        public virtual string Generate()
        {
            var sb = new StringBuilder();
            sb.AppendLine(" Sales Report ");
            foreach (var r in _records.OrderBy(r => r.Date))
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    public class UserReport : IReport
    {
        protected List<ReportRecord> _records;

        public UserReport()
        {
            _records = new List<ReportRecord>();
            var rnd = new Random();
            var roles = new[] { "buyer", "vip", "guest" };
            for (int i = 0; i < 10; i++)
            {
                _records.Add(new ReportRecord
                {
                    Date = DateTime.Now.Date.AddDays(-rnd.Next(0, 100)),
                    Amount = Math.Round(rnd.NextDouble() * 1000, 2),
                    UserId = "User" + (i + 1),
                    Extra = $"role:{roles[rnd.Next(0, roles.Length)]}"
                });
            }
        }

        public virtual List<ReportRecord> GetRecords() => _records;

        public virtual string Generate()
        {
            var sb = new StringBuilder();
            sb.AppendLine(" User Report ");
            foreach (var r in _records.OrderBy(r => r.UserId))
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    public abstract class ReportDecorator : IReport
    {
        protected IReport _inner;
        public ReportDecorator(IReport inner) => _inner = inner;
        public abstract List<ReportRecord> GetRecords();
        public virtual string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine(" Decorated Report ");
            foreach (var r in recs)
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    public class DateFilterDecorator : ReportDecorator
    {
        private DateTime _from, _to;
        public DateFilterDecorator(IReport inner, DateTime from, DateTime to) : base(inner)
        {
            _from = from.Date;
            _to = to.Date;
        }

        public override List<ReportRecord> GetRecords()
        {
            return _inner.GetRecords()
                         .Where(r => r.Date.Date >= _from && r.Date.Date <= _to)
                         .ToList();
        }

        public override string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine($" Date Filter: {_from:yyyy-MM-dd} to {_to:yyyy-MM-dd} ");
            foreach (var r in recs.OrderBy(r => r.Date))
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    public enum SortCriteria { DateAsc, DateDesc, AmountAsc, AmountDesc, User }
    public class SortingDecorator : ReportDecorator
    {
        private SortCriteria _criteria;
        public SortingDecorator(IReport inner, SortCriteria criteria) : base(inner)
        {
            _criteria = criteria;
        }

        public override List<ReportRecord> GetRecords()
        {
            var recs = _inner.GetRecords();
            return _criteria switch
            {
                SortCriteria.DateAsc => recs.OrderBy(r => r.Date).ToList(),
                SortCriteria.DateDesc => recs.OrderByDescending(r => r.Date).ToList(),
                SortCriteria.AmountAsc => recs.OrderBy(r => r.Amount).ToList(),
                SortCriteria.AmountDesc => recs.OrderByDescending(r => r.Amount).ToList(),
                SortCriteria.User => recs.OrderBy(r => r.UserId).ToList(),
                _ => recs
            };
        }

        public override string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine($" Sorted by {_criteria} ");
            foreach (var r in recs)
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    public class CsvExportDecorator : ReportDecorator
    {
        public CsvExportDecorator(IReport inner) : base(inner) { }

        public override List<ReportRecord> GetRecords() => _inner.GetRecords();

        public override string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine("Date,User,Amount,Extra");
            foreach (var r in recs)
                sb.AppendLine($"{r.Date:yyyy-MM-dd},{r.UserId},{r.Amount},{r.Extra}");
            return sb.ToString();
        }
    }

    public class PdfExportDecorator : ReportDecorator
    {
        public PdfExportDecorator(IReport inner) : base(inner) { }

        public override List<ReportRecord> GetRecords() => _inner.GetRecords();

        public override string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine("<<PDF REPORT>>");
            sb.AppendLine($"Generated at: {DateTime.Now}");
            foreach (var r in recs)
                sb.AppendLine(r.ToString());
            sb.AppendLine("<<END PDF>>");
            return sb.ToString();
        }
    }

    public class SumFilterDecorator : ReportDecorator
    {
        private double _minAmount;
        public SumFilterDecorator(IReport inner, double minAmount) : base(inner)
        {
            _minAmount = minAmount;
        }

        public override List<ReportRecord> GetRecords()
        {
            return _inner.GetRecords().Where(r => r.Amount >= _minAmount).ToList();
        }

        public override string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine($" Filter: Amount >= {_minAmount} ");
            foreach (var r in recs)
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    public class UserAttributeFilterDecorator : ReportDecorator
    {
        private string _attribute;
        public UserAttributeFilterDecorator(IReport inner, string attribute) : base(inner)
        {
            _attribute = attribute;
        }

        public override List<ReportRecord> GetRecords()
        {
            return _inner.GetRecords().Where(r => r.Extra != null && r.Extra.Contains(_attribute)).ToList();
        }

        public override string Generate()
        {
            var recs = GetRecords();
            var sb = new StringBuilder();
            sb.AppendLine($"=== User Attribute Filter: {_attribute} ===");
            foreach (var r in recs)
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine(" Система отчётности ");
            Console.WriteLine("Выберите тип отчёта:");
            Console.WriteLine("1 - SalesReport");
            Console.WriteLine("2 - UserReport");
            Console.Write("Ваш выбор: ");
            int rptChoice = int.Parse(Console.ReadLine() ?? "1");

            IReport report = rptChoice == 2 ? new UserReport() as IReport : new SalesReport() as IReport;

            bool configuring = true;
            while (configuring)
            {
                Console.WriteLine("\nДобавить декоратор:");
                Console.WriteLine("1 - Фильтр по датам");
                Console.WriteLine("2 - Сортировка");
                Console.WriteLine("3 - Экспорт в CSV");
                Console.WriteLine("4 - Экспорт в PDF (симуляция)");
                Console.WriteLine("5 - Фильтр по сумме (минимум)");
                Console.WriteLine("6 - Фильтр по пользовательскому атрибуту (например role:vip)");
                Console.WriteLine("0 - Готово / сгенерировать");
                Console.Write("Ваш выбор: ");
                int d = int.Parse(Console.ReadLine() ?? "0");
                switch (d)
                {
                    case 1:
                        Console.Write("Дата с (yyyy-MM-dd): ");
                        var from = DateTime.Parse(Console.ReadLine() ?? DateTime.Now.ToString());
                        Console.Write("Дата по (yyyy-MM-dd): ");
                        var to = DateTime.Parse(Console.ReadLine() ?? DateTime.Now.ToString());
                        report = new DateFilterDecorator(report, from, to);
                        Console.WriteLine("Добавлен DateFilterDecorator.");
                        break;
                    case 2:
                        Console.WriteLine("Критерии сортировки: 1-DateAsc 2-DateDesc 3-AmountAsc 4-AmountDesc 5-User");
                        Console.Write("Выбор: ");
                        int sc = int.Parse(Console.ReadLine() ?? "1");
                        var crit = sc switch
                        {
                            1 => SortCriteria.DateAsc,
                            2 => SortCriteria.DateDesc,
                            3 => SortCriteria.AmountAsc,
                            4 => SortCriteria.AmountDesc,
                            5 => SortCriteria.User,
                            _ => SortCriteria.DateAsc
                        };
                        report = new SortingDecorator(report, crit);
                        Console.WriteLine("Добавлен SortingDecorator.");
                        break;
                    case 3:
                        report = new CsvExportDecorator(report);
                        Console.WriteLine("Добавлен CsvExportDecorator.");
                        break;
                    case 4:
                        report = new PdfExportDecorator(report);
                        Console.WriteLine("Добавлен PdfExportDecorator.");
                        break;
                    case 5:
                        Console.Write("Минимальная сумма: ");
                        double min = double.Parse(Console.ReadLine() ?? "0");
                        report = new SumFilterDecorator(report, min);
                        Console.WriteLine("Добавлен SumFilterDecorator.");
                        break;
                    case 6:
                        Console.Write("Атрибут (например role:vip): ");
                        var attr = Console.ReadLine() ?? "";
                        report = new UserAttributeFilterDecorator(report, attr);
                        Console.WriteLine("Добавлен UserAttributeFilterDecorator.");
                        break;
                    case 0:
                        configuring = false;
                        break;
                    default:
                        Console.WriteLine("Неверный выбор.");
                        break;
                }
            }

            Console.WriteLine("\n Генерация отчёта \n");
            string output;
            try
            {
                output = report.Generate();
            }
            catch (Exception ex)
            {
                output = "Ошибка при генерации: " + ex.Message;
            }

            Console.WriteLine(output);
            Console.WriteLine("\n Конец отчёта ");
        }
    }
}
