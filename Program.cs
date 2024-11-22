using System;
using System.Collections.Generic;
using System.Linq;

namespace TraderProgram
{
    #region Enums
    /// <summary>
    /// Enumerator for different operation types.
    /// </summary>
    public enum OperationType
    {
        NONE,
        BUY,
        SELL,
        MODIFY,
        MODIFY_BUY,
        MODIFY_SELL,
        CANCEL,
        PRINT
    }

    /// <summary>
    /// Enumerator for order types (IOC or GFD).
    /// </summary>
    public enum OrderType
    {
        None = 0,
        IOC = 1,
        GFD = 2
    }
    #endregion

    #region Models
    /// <summary>
    /// Represents a trade transaction.
    /// </summary>
    public class TradeTransaction
    {
        public string OrderId { get; private set; }
        public ulong OrderPrice { get; set; }
        public double OrderQuantity { get; set; }
        public OrderType OrderType { get; private set; }
        public OperationType OperationType { get; private set; }

        public TradeTransaction(OperationType operationType, OrderType orderType, ulong price, double quantity, string orderId)
        {
            OperationType = operationType;
            OrderType = orderType;
            OrderId = orderId;
            OrderPrice = price;
            OrderQuantity = quantity;
        }

        public override string ToString()
        {
            return $"OrderID: {OrderId}, Price: {OrderPrice}, Quantity: {OrderQuantity}, Type: {OrderType}, Operation: {OperationType}";
        }
    }
    #endregion

    #region Interfaces and Commands
    /// <summary>
    /// Interface for executing trade operations.
    /// </summary>
    public interface IOrderTransaction
    {
        OperationType Id { get; }
        void ExecuteTransaction(SortedDictionary<ulong, List<TradeTransaction>> buyOrders,
                                SortedDictionary<ulong, List<TradeTransaction>> sellOrders,
                                TradeTransaction transaction);
    }

    /// <summary>
    /// Command to execute a BUY operation.
    /// </summary>
    public class BuyTransaction : IOrderTransaction
    {
        public OperationType Id => OperationType.BUY;

        public void ExecuteTransaction(SortedDictionary<ulong, List<TradeTransaction>> buyOrders,
                                       SortedDictionary<ulong, List<TradeTransaction>> sellOrders,
                                       TradeTransaction transaction)
        {
            try
            {
                foreach (var sellPrice in sellOrders.Keys.ToList())
                {
                    if (transaction.OrderPrice >= sellPrice)
                    {
                        var sellOrderList = sellOrders[sellPrice];
                        while (transaction.OrderQuantity > 0 && sellOrderList.Count > 0)
                        {
                            var sellOrder = sellOrderList[0];
                            var tradeQty = Math.Min(transaction.OrderQuantity, sellOrder.OrderQuantity);

                            // Print trade
                            Console.WriteLine($"TRADE {sellOrder.OrderId} {sellOrder.OrderPrice} {tradeQty} {transaction.OrderId} {transaction.OrderPrice} {tradeQty}");

                            // Update quantities
                            transaction.OrderQuantity -= tradeQty;
                            sellOrder.OrderQuantity -= tradeQty;

                            // Remove completed sell order
                            if (sellOrder.OrderQuantity <= 0) sellOrderList.RemoveAt(0);
                        }

                        // Remove price level if empty
                        if (sellOrderList.Count == 0) sellOrders.Remove(sellPrice);

                        if (transaction.OrderQuantity == 0) break;
                    }
                }

                if (transaction.OrderQuantity > 0 && transaction.OrderType == OrderType.GFD)
                {
                    AddOrderToBook(buyOrders, transaction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BuyTransaction: {ex.Message}");
            }
        }

        private void AddOrderToBook(SortedDictionary<ulong, List<TradeTransaction>> orderBook, TradeTransaction transaction)
        {
            if (!orderBook.ContainsKey(transaction.OrderPrice))
            {
                orderBook[transaction.OrderPrice] = new List<TradeTransaction>();
            }
            orderBook[transaction.OrderPrice].Add(transaction);
        }
    }

    /// <summary>
    /// Command to execute a SELL operation.
    /// </summary>
    public class SellTransaction : IOrderTransaction
    {
        public OperationType Id => OperationType.SELL;

        public void ExecuteTransaction(SortedDictionary<ulong, List<TradeTransaction>> buyOrders,
                                       SortedDictionary<ulong, List<TradeTransaction>> sellOrders,
                                       TradeTransaction transaction)
        {
            try
            {
                foreach (var buyPrice in buyOrders.Keys.OrderByDescending(x => x).ToList())
                {
                    if (transaction.OrderPrice <= buyPrice)
                    {
                        var buyOrderList = buyOrders[buyPrice];
                        while (transaction.OrderQuantity > 0 && buyOrderList.Count > 0)
                        {
                            var buyOrder = buyOrderList[0];
                            var tradeQty = Math.Min(transaction.OrderQuantity, buyOrder.OrderQuantity);

                            // Print trade
                            Console.WriteLine($"TRADE {buyOrder.OrderId} {buyOrder.OrderPrice} {tradeQty} {transaction.OrderId} {transaction.OrderPrice} {tradeQty}");

                            // Update quantities
                            transaction.OrderQuantity -= tradeQty;
                            buyOrder.OrderQuantity -= tradeQty;

                            // Remove completed buy order
                            if (buyOrder.OrderQuantity <= 0) buyOrderList.RemoveAt(0);
                        }

                        // Remove price level if empty
                        if (buyOrderList.Count == 0) buyOrders.Remove(buyPrice);

                        if (transaction.OrderQuantity == 0) break;
                    }
                }

                if (transaction.OrderQuantity > 0 && transaction.OrderType == OrderType.GFD)
                {
                    AddOrderToBook(sellOrders, transaction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SellTransaction: {ex.Message}");
            }
        }

        private void AddOrderToBook(SortedDictionary<ulong, List<TradeTransaction>> orderBook, TradeTransaction transaction)
        {
            if (!orderBook.ContainsKey(transaction.OrderPrice))
            {
                orderBook[transaction.OrderPrice] = new List<TradeTransaction>();
            }
            orderBook[transaction.OrderPrice].Add(transaction);
        }
    }
    #endregion

    #region Engine and Driver
    /// <summary>
    /// Engine to manage and execute trade orders.
    /// </summary>
    public class TradeEngine
    {
        private readonly SortedDictionary<ulong, List<TradeTransaction>> buyOrders;
        private readonly SortedDictionary<ulong, List<TradeTransaction>> sellOrders;

        public TradeEngine()
        {
            buyOrders = new SortedDictionary<ulong, List<TradeTransaction>>(Comparer<ulong>.Create((x, y) => y.CompareTo(x))); // Descending
            sellOrders = new SortedDictionary<ulong, List<TradeTransaction>>();
        }

        public void ExecuteCommand(IOrderTransaction command, TradeTransaction transaction)
        {
            command.ExecuteTransaction(buyOrders, sellOrders, transaction);
        }

        public void PrintOrderBook()
        {
            Console.WriteLine("SELL:");
            foreach (var price in sellOrders.Keys)
            {
                var totalQty = sellOrders[price].Sum(o => o.OrderQuantity);
                Console.WriteLine($"{price} {totalQty}");
            }
            Console.WriteLine("BUY:");
            foreach (var price in buyOrders.Keys)
            {
                var totalQty = buyOrders[price].Sum(o => o.OrderQuantity);
                Console.WriteLine($"{price} {totalQty}");
            }
        }
    }

    /// <summary>
    /// Main class to drive the application.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            var engine = new TradeEngine();

            var transactions = new List<TradeTransaction>
            {
                new TradeTransaction(OperationType.BUY, OrderType.GFD, 1000, 40, "order1"),
                new TradeTransaction(OperationType.SELL, OrderType.IOC, 900, 20, "order2"),
                new TradeTransaction(OperationType.BUY, OrderType.IOC, 1000, 30, "order3"),
            };

            foreach (var transaction in transactions)
            {
                var command = transaction.OperationType switch
                {
                    OperationType.BUY => new BuyTransaction(),
                    OperationType.SELL => new SellTransaction(),
                    _ => throw new InvalidOperationException("Unknown operation type")
                };

                engine.ExecuteCommand(command, transaction);
            }

            engine.PrintOrderBook();
        }
    }
    #endregion
