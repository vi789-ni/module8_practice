using System;

namespace LogisticsAdapterPractice
{
    public interface IInternalDeliveryService
    {
        bool DeliverOrder(string orderId);
        string GetDeliveryStatus(string orderId);
        double CalculateDeliveryCost(string orderId, string region);
    }

    public class InternalDeliveryService : IInternalDeliveryService
    {
        public bool DeliverOrder(string orderId)
        {
            Console.WriteLine($"[Internal] Starting delivery for order {orderId}...");
            System.Threading.Thread.Sleep(200);
            Console.WriteLine($"[Internal] Order {orderId} delivered by internal logistics.");
            return true;
        }

        public string GetDeliveryStatus(string orderId)
        {
            return $"[Internal] Status({orderId}): Delivered";
        }

        public double CalculateDeliveryCost(string orderId, string region)
        {
            double baseCost = 500;
            if (region.ToLower().Contains("remote")) baseCost += 200;
            return baseCost;
        }
    }

    public class ExternalLogisticsServiceA
    {
        public bool ShipItem(int itemId)
        {
            Console.WriteLine($"[ExternalA] Shipping item {itemId} via ExternalA...");
            return true;
        }
        public string TrackShipment(int shipmentId) => $"[ExternalA] Shipment {shipmentId} in transit";
        public double GetRate(int itemId) => 700 + (itemId % 5) * 50;
    }

    public class ExternalLogisticsServiceB
    {
        public bool SendPackage(string packageInfo)
        {
            Console.WriteLine($"[ExternalB] Sending package: {packageInfo}");
            return true;
        }
        public string CheckPackageStatus(string trackingCode) => $"[ExternalB] Package {trackingCode} - delivered";
        public double PriceForPackage(string region) => region.ToLower().Contains("near") ? 400 : 800;
    }

    public class ExternalLogisticsServiceC
    {
        public bool Dispatch(string orderRef)
        {
            Console.WriteLine($"[ExternalC] Dispatching order {orderRef}...");
            return true;
        }
        public string Status(string refCode) => $"[ExternalC] Ref:{refCode} - out for delivery";
        public double Rate(string region, int weightKg) => 300 + weightKg * 150;
    }

    public class LogisticsAdapterA : IInternalDeliveryService
    {
        private ExternalLogisticsServiceA _svc;
        public LogisticsAdapterA(ExternalLogisticsServiceA svc) => _svc = svc;

        public bool DeliverOrder(string orderId)
        {
            try
            {
                Console.WriteLine("[AdapterA] Logging: converting orderId -> itemId for ExternalA");
                int itemId = Math.Abs(orderId.GetHashCode()) % 1000;
                var ok = _svc.ShipItem(itemId);
                Console.WriteLine("[AdapterA] Delivery result: " + ok);
                return ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AdapterA] Error: " + ex.Message);
                return false;
            }
        }

        public string GetDeliveryStatus(string orderId)
        {
            int shipmentId = Math.Abs(orderId.GetHashCode()) % 1000;
            return _svc.TrackShipment(shipmentId);
        }

        public double CalculateDeliveryCost(string orderId, string region)
        {
            int itemId = Math.Abs(orderId.GetHashCode()) % 1000;
            return _svc.GetRate(itemId);
        }
    }

    public class LogisticsAdapterB : IInternalDeliveryService
    {
        private ExternalLogisticsServiceB _svc;
        public LogisticsAdapterB(ExternalLogisticsServiceB svc) => _svc = svc;

        public bool DeliverOrder(string orderId)
        {
            try
            {
                string packageInfo = $"order:{orderId};timestamp:{DateTime.Now.Ticks}";
                return _svc.SendPackage(packageInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AdapterB] Error: " + ex.Message);
                return false;
            }
        }

        public string GetDeliveryStatus(string orderId)
        {
            string tracking = $"TR-{Math.Abs(orderId.GetHashCode()) % 10000}";
            return _svc.CheckPackageStatus(tracking);
        }

        public double CalculateDeliveryCost(string orderId, string region)
        {
            return _svc.PriceForPackage(region);
        }
    }

    public class LogisticsAdapterC : IInternalDeliveryService
    {
        private ExternalLogisticsServiceC _svc;
        public LogisticsAdapterC(ExternalLogisticsServiceC svc) => _svc = svc;

        public bool DeliverOrder(string orderId)
        {
            try
            {
                return _svc.Dispatch(orderId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AdapterC] Error: " + ex.Message);
                return false;
            }
        }

        public string GetDeliveryStatus(string orderId)
        {
            return _svc.Status(orderId);
        }

        public double CalculateDeliveryCost(string orderId, string region)
        {
            int weight = (Math.Abs(orderId.GetHashCode()) % 10) + 1;
            return _svc.Rate(region, weight);
        }
    }

    public static class DeliveryServiceFactory
    {
        public static IInternalDeliveryService Create(string type, string region = "near")
        {
            switch (type.ToLower())
            {
                case "internal":
                    return new InternalDeliveryService();
                case "externalA":
                    return new LogisticsAdapterA(new ExternalLogisticsServiceA());
                case "externalB":
                    return new LogisticsAdapterB(new ExternalLogisticsServiceB());
                case "externalC":
                    return new LogisticsAdapterC(new ExternalLogisticsServiceC());
                default:
                    throw new ArgumentException("Unknown delivery service type");
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine(" Система доставки ");

            Console.WriteLine("Доступные службы: internal, externalA, externalB, externalC");
            Console.Write("Выберите службу (internal/externalA/externalB/externalC): ");
            string svcType = Console.ReadLine() ?? "internal";
            Console.Write("Регион (near/remote): ");
            string region = Console.ReadLine() ?? "near";

            IInternalDeliveryService service;
            try
            {
                service = DeliveryServiceFactory.Create(svcType, region);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка создания службы: " + ex.Message);
                return;
            }

            Console.Write("Введите id заказа: ");
            string orderId = Console.ReadLine() ?? "ORD123";

            try
            {
                double cost = service.CalculateDeliveryCost(orderId, region);
                Console.WriteLine($"Ориентировочная стоимость доставки: {cost} тг");

                Console.WriteLine("Выполнить доставку сейчас? (y/n): ");
                if ((Console.ReadLine() ?? "n").ToLower() == "y")
                {
                    bool ok = service.DeliverOrder(orderId);
                    Console.WriteLine($"Результат доставки: {ok}");
                }

                Console.WriteLine("\nПолучить статус доставки? (y/n): ");
                if ((Console.ReadLine() ?? "n").ToLower() == "y")
                {
                    Console.WriteLine(service.GetDeliveryStatus(orderId));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            Console.WriteLine("\n Конец сценария доставки ");
        }
    }
}
