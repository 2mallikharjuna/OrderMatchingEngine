using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TraderProgram
{
    //enumerator list for operation types
    public enum OperationType
    {
        OPERATION_TYPE_NONE,
        OPERATION_TYPE_BUY,
        OPERATION_TYPE_SELL,
        OPERATION_TYPE_MODIFY,
        OPERATION_TYPE_MODIFY_BUY,
        OPERATION_TYPE_MODIFY_SELL,
        OPERATION_TYPE_CANCEL,
        OPERATION_TYPE_PRINT
    }
    //enumerator list for Order types
    public enum OrderType
    {
        None = 0,
        IOC = 1,
        GFD = 2
    }
    public class TradeTrasaction
    {
        public string OrderId { get; private set; }
        public UInt64 OrderPrice { get; set; }
        public Double OrderQuantity { get; set; }
        public OrderType OrderType { get; private set; }
        public OperationType OperationType { get; private set; }
        public TradeTrasaction(OperationType operationType)
        {
            OrderType = OrderType.None;
            OperationType = operationType;
            OrderPrice = 0;

        }
        public TradeTrasaction(OperationType operationType, OrderType orderType, UInt64 price, UInt64 quantity, string orderId)
        {
            OperationType = operationType;
            OrderType = orderType;
            OrderId = orderId;
            OrderPrice = price;
            OrderQuantity = quantity;            
        }
        public override string ToString()
        {
            return string.Format("{0} {1} {2}", OrderType, OrderId, OperationType);
        }
    }
    /// <summary>
    /// The 'IOrderTransaction' interface
    /// </summary>
    public interface IOrderTransaction
    {
        OperationType Id { get; }
        void ExecuteTrasaction(Dictionary<string, TradeTrasaction> BuyTradeItems, Dictionary<string, TradeTrasaction> SellTradeItems, TradeTrasaction tradeItem);
    }

    /// <summary>
    /// A concrete command
    /// </summary>
    public class BuyTransaction : IOrderTransaction
    {
        public OperationType Id { get { return OperationType.OPERATION_TYPE_BUY; } }
        
        private void PrintTrade(TradeTrasaction Item1, TradeTrasaction Item2, double Quanity)
        {
            Console.WriteLine("TRADE {0} {1} {2} {3} {4} {5}", Item1.OrderId, Item1.OrderPrice, Quanity, Item2.OrderId, Item2.OrderPrice, Quanity);
        }
        public void ExecuteTrasaction(Dictionary<string, TradeTrasaction> BuyTradeItems, Dictionary<string, TradeTrasaction> SellTradeItems, TradeTrasaction tradeItem)
        {
            try
            {
                foreach (var sellItem in SellTradeItems.Values.Reverse().ToList())
                {
                    if(tradeItem.OrderPrice >= sellItem.OrderPrice)
                    {
                        var buyOrderQty = tradeItem.OrderQuantity;
                        var sellOrderQty = sellItem.OrderQuantity;

                        if(buyOrderQty == sellOrderQty)
                        {
                            PrintTrade(sellItem, tradeItem, buyOrderQty);                            
                            SellTradeItems.Remove(sellItem.OrderId);
                            return;
                        }
                        else if(buyOrderQty < sellOrderQty)
                        {
                            PrintTrade(sellItem, tradeItem, buyOrderQty);
                            SellTradeItems[sellItem.OrderId].OrderQuantity = sellOrderQty - buyOrderQty;
                            return;
                        }
                        else 
                        {
                            PrintTrade(sellItem, tradeItem, sellOrderQty);
                            tradeItem.OrderQuantity = buyOrderQty - sellOrderQty;
                            SellTradeItems.Remove(sellItem.OrderId);
                            continue;
                        }
                    }
                }
                if (tradeItem.OrderType == OrderType.GFD)
                {
                    BuyTradeItems.Add(tradeItem.OrderId, tradeItem);
                }
            }
            catch
            {
                Console.WriteLine("Buy Error");
            }
        }
    }

    /// <summary>
    /// A concrete command
    /// </summary>
    public class SellTransaction : IOrderTransaction
    {
        public OperationType Id { get { return OperationType.OPERATION_TYPE_SELL; } }
        private void PrintTrade(TradeTrasaction Item1, TradeTrasaction Item2, double Quanity)
        {
            Console.WriteLine("TRADE {0} {1} {2} {3} {4} {5}", Item1.OrderId, Item1.OrderPrice, Quanity, Item2.OrderId, Item2.OrderPrice, Quanity);
        }
        private void UpdateTradeItem(TradeTrasaction tradeItem, UInt64 price, double quantity)
        {
            tradeItem.OrderPrice = price;
            tradeItem.OrderQuantity -= quantity;
            if (tradeItem.OrderQuantity <= 0)
                tradeItem = null;
        }
        public void ExecuteTrasaction(Dictionary<string, TradeTrasaction> BuyTradeItems, Dictionary<string, TradeTrasaction> SellTradeItems, TradeTrasaction order)
        {
            try
            {
                foreach (var buyItem in BuyTradeItems.Values.Reverse().ToList())
                {
                    if (order.OrderPrice <= buyItem.OrderPrice)
                    {
                        var OrderQty = order.OrderQuantity;
                        var buyOrderQty = buyItem.OrderQuantity;

                        if (OrderQty == buyOrderQty)
                        {
                            PrintTrade(buyItem, order, OrderQty);
                            BuyTradeItems.Remove(buyItem.OrderId);
                            return;
                        }
                        else if (OrderQty < buyOrderQty)
                        {
                            PrintTrade(buyItem, order, OrderQty);
                            BuyTradeItems[buyItem.OrderId].OrderQuantity = buyOrderQty - OrderQty;
                            return;
                        }
                        else
                        {
                            PrintTrade(buyItem, order, buyOrderQty);
                            order.OrderQuantity = OrderQty - buyOrderQty;
                            BuyTradeItems.Remove(buyItem.OrderId);
                            continue;
                        }
                    }
                }
                if (order.OrderType == OrderType.GFD)
                {
                    SellTradeItems.Add(order.OrderId, order);
                }
            }
            catch
            {
                Console.WriteLine("Sell Error");
            }
        }
    }

    /// <summary>
    /// A concrete command
    /// </summary>
    public class CancelTransaction : IOrderTransaction
    {
        public OperationType Id { get { return OperationType.OPERATION_TYPE_CANCEL; } }
        public void ExecuteTrasaction(Dictionary<string, TradeTrasaction> BuyTradeItems, Dictionary<string, TradeTrasaction> SellTradeItems, TradeTrasaction tradeItem)
        {            
            if (BuyTradeItems.ContainsKey(tradeItem.OrderId))
            {
                BuyTradeItems.Remove(tradeItem.OrderId);                
            }
            else if(SellTradeItems.ContainsKey(tradeItem.OrderId))
            {
                SellTradeItems.Remove(tradeItem.OrderId);
            }
        }
    }

    public class PrintTransaction : IOrderTransaction
    {
        public OperationType Id { get { return OperationType.OPERATION_TYPE_PRINT; } }
        private IEnumerable<KeyValuePair<UInt64, double>> GetPritableElements(List<TradeTrasaction> OrderedItems)
        {
            var OrderTable = new Dictionary<UInt64, double>();

            foreach (var item in OrderedItems)
            {
                try
                {
                    OrderTable.Add(item.OrderPrice, item.OrderQuantity);
                }
                catch
                {
                    OrderTable[item.OrderPrice] += item.OrderQuantity;
                }
            }
            return OrderTable;
        }
        
        public void ExecuteTrasaction(Dictionary<string, TradeTrasaction> BuyTradeItems, Dictionary<string, TradeTrasaction> SellTradeItems, TradeTrasaction tradeItem)
        {

            Console.WriteLine("SELL:");
            foreach (var pritableElement in GetPritableElements(SellTradeItems.Values.OrderByDescending(x => x.OrderPrice).ToList()))
            {
                if (pritableElement.Value != 0)
                    Console.WriteLine(pritableElement.Key + " " + pritableElement.Value);
            }
            Console.WriteLine("BUY:");
            foreach (var pritableElement in GetPritableElements(BuyTradeItems.Values.OrderByDescending(x => x.OrderPrice).ToList()))
            {
                if (pritableElement.Value != 0)
                    Console.WriteLine(pritableElement.Key + " " + pritableElement.Value);
            }
        }
    }

    public static class OrderCommandFactory
    {
        public static IOrderTransaction Get(OperationType operationType)
        {
            return OrderCommandMapper[operationType]();
        }
        public static IOrderTransaction CreateInstance<T>(string TransDetails) where T : class, IOrderTransaction
        {
            return (T)Activator.CreateInstance(typeof(T), TransDetails);
        }
        
        //Modify is combination of cancel and buy/sell, so command to devided to execute.
        private static Dictionary<OperationType, Func<IOrderTransaction>> OrderCommandMapper =
            new Dictionary<OperationType, Func<IOrderTransaction>> {
                        { OperationType.OPERATION_TYPE_BUY, ()=>new BuyTransaction() },
                        { OperationType.OPERATION_TYPE_SELL, ()=>new SellTransaction() },
                        { OperationType.OPERATION_TYPE_MODIFY, ()=>new CancelTransaction() },
                        { OperationType.OPERATION_TYPE_CANCEL, ()=>new CancelTransaction() },
                        { OperationType.OPERATION_TYPE_MODIFY_BUY, ()=>new BuyTransaction() },
                        { OperationType.OPERATION_TYPE_MODIFY_SELL, ()=>new SellTransaction() },
                        { OperationType.OPERATION_TYPE_PRINT, ()=>new PrintTransaction()   }
            };
    }
        
    public class TradedOrderEngine
    {
        
        internal static Dictionary<string, TradeTrasaction> m_sellBookList;
        internal static Dictionary<string, TradeTrasaction> m_buybookList;
        
        internal Dictionary<string, TradeTrasaction> BuyTradeTransactions
        {
            get { return m_buybookList; }
        }
        internal Dictionary<string, TradeTrasaction> SellTradeTransactions
        {
            get { return m_sellBookList; }
        }
        public TradedOrderEngine()
        {
            
            m_buybookList = new Dictionary<string, TradeTrasaction>();
            m_sellBookList = new Dictionary<string, TradeTrasaction>();
        }

        public void ExecuteCommand(IOrderTransaction command, TradeTrasaction tradeItem)
        {
            if (command != null)
            {
                command.ExecuteTrasaction(BuyTradeTransactions, SellTradeTransactions, tradeItem);
                
                //We meet this condition only when OPERATION_TYPE_MODIFY_BUY/OPERATION_TYPE_MODIFY_SELL
                if (tradeItem != null && tradeItem.OperationType != command.Id)
                    OrderCommandFactory.Get(tradeItem.OperationType).ExecuteTrasaction(BuyTradeTransactions, SellTradeTransactions, tradeItem);
            }
        }        
    }

    /// <summary>
    /// The Invoker class
    /// </summary>
    public class Patron
    {
        private IOrderTransaction _orderCommand;
        private TradeTrasaction _tradeItem;
        private TradedOrderEngine _order;

        public Patron()
        {
            _order = new TradedOrderEngine();
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase: true);            
        }
        public static T ParseOperationTypeEnum<T>(string value)
        {
            return ParseEnum<T>(string.Format("OPERATION_TYPE_{0}",value));
        }        
        
        private bool IsInt(string sVal)
        {
            foreach (char c in sVal)
            {
                int iN = (int)c;
                if ((iN > 57) || (iN < 48))
                    return false;
            }
            return true;
        }
        public OperationType CreateTradeItem(string inputCommandLine)
        { 
            string[] stdInputArgumentsArray = inputCommandLine.Split(' ');

            if (stdInputArgumentsArray.Length == 0 || stdInputArgumentsArray.Length > 5)
            {                
                return OperationType.OPERATION_TYPE_NONE;
            }

            OperationType operType = ParseOperationTypeEnum<OperationType>(stdInputArgumentsArray[0]);

            if (operType == OperationType.OPERATION_TYPE_BUY || operType == OperationType.OPERATION_TYPE_SELL)
            {
                if ((stdInputArgumentsArray.Length < 5) || !IsInt(stdInputArgumentsArray[2]) || !IsInt(stdInputArgumentsArray[3]))
                   return OperationType.OPERATION_TYPE_NONE;

                _tradeItem = new TradeTrasaction(operType, ParseEnum<OrderType>(stdInputArgumentsArray[1]),
                                                           Convert.ToUInt64(stdInputArgumentsArray[2]),
                                                           Convert.ToUInt64(stdInputArgumentsArray[3]),
                                                           stdInputArgumentsArray[4]);
                return _tradeItem.OperationType;

            }
            else if (operType == OperationType.OPERATION_TYPE_MODIFY)
            {
                if ((stdInputArgumentsArray.Length < 5) || !IsInt(stdInputArgumentsArray[3]) || !IsInt(stdInputArgumentsArray[4]))
                    return OperationType.OPERATION_TYPE_NONE; 

                OperationType ModoperType = ParseOperationTypeEnum<OperationType>(stdInputArgumentsArray[2]);
                
                if ((ModoperType == OperationType.OPERATION_TYPE_BUY) || (ModoperType == OperationType.OPERATION_TYPE_SELL))
                {
                    ModoperType += 3; //set to modify buy/sell
                    _tradeItem = new TradeTrasaction(ModoperType, OrderType.GFD,
                                                           Convert.ToUInt64(stdInputArgumentsArray[3]),
                                                           Convert.ToUInt64(stdInputArgumentsArray[4]),
                                                           stdInputArgumentsArray[1]);
                    return operType; //return the orginal operation
                }
                return OperationType.OPERATION_TYPE_NONE;

            }
            else if (operType == OperationType.OPERATION_TYPE_CANCEL)
            {
                if (stdInputArgumentsArray.Length != 2)
                    return OperationType.OPERATION_TYPE_NONE; 
                _tradeItem = new TradeTrasaction(OperationType.OPERATION_TYPE_CANCEL, OrderType.None, 0, 0, stdInputArgumentsArray[1]);
                return _tradeItem.OperationType;

            }
            else if (operType == OperationType.OPERATION_TYPE_PRINT)
            {
                if (stdInputArgumentsArray.Length != 1)
                    return OperationType.OPERATION_TYPE_NONE; 
                _tradeItem = new TradeTrasaction(OperationType.OPERATION_TYPE_PRINT);
                return _tradeItem.OperationType;
            }
            return OperationType.OPERATION_TYPE_NONE; ;
        }
        
        public void ExecuteCommand(OperationType operationCommand)
        {
            if (operationCommand != OperationType.OPERATION_TYPE_NONE)
            {
                _orderCommand = OrderCommandFactory.Get(operationCommand);
                _order.ExecuteCommand(_orderCommand, _tradeItem);
            }
        }        
    }    

    class Solution
    {    
        static void Main(string[] args)
        {
            List<string> InputCommandArray = new List<string> { "BUY GFD 1000 -40 order1", "BUY IOC -1000 10 order2", "SELL IOC 900 20 order4", "PRINT" };
            
            string[] stdInputArgumentsArray = new string[] { };
            
            IList<IOrderTransaction> _transactions = new List<IOrderTransaction>();
            Patron patron = new Patron();
            
            foreach (var inputLine in InputCommandArray)
            {
                patron.ExecuteCommand(patron.CreateTradeItem(inputLine));
            }
            Console.ReadKey();
        }
    }
}
