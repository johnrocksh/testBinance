
using WebSocketSharp;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Example
{

    public class Menu
    {
        ListenerThread lt = new ListenerThread();
        string Pairs = "";

        public void ShowMenu()
        {
            Console.WriteLine($"\n");
            Console.WriteLine($"------EXCHANGE-------\n");          
            Console.WriteLine($"1. SUBSCRIBE to TRADES \n");  
            Console.WriteLine($"---------------------\n");
            Console.WriteLine("Enter your choice:");
        }
        public bool MenuHandler(int choise)
        {
            switch (choise)
            {
                case 1:
                    {                      
                        Console.WriteLine("Please enter pair in format: usdttry,ethbtc,ethusdt");                             
                        Pairs = Console.ReadLine();
                        TradesList.ListOfSubscribedPairs = Pairs.Split(',').ToList();
                        Console.WriteLine($"Your pare is : {Pairs}");
                    
                            OnSubscribeMenu();                                           
                        break;
                    }                  
            }
            return true;
        }
        public int GetChoice(int choice)
        {
            try
            {
                choice = Convert.ToInt32(Console.ReadLine());
            }
            catch (Exception ex) { Console.WriteLine(ex); }

            return choice;
        }
        
        public void OnSubscribeMenu()
        {
            
            Debug.WriteLine($"START MAIN");
     
            List<Trade> ListOfResultTrades = new();
  
            //INIT of Trades Counter Dictionary            
            Debug.WriteLine($"INIT DICTIONARY");
            foreach (var item in TradesList.ListOfSubscribedPairs)
            {
                TradesList.DictionaryOfCounts.Add(item, 0);
            }
          
            //---------ADDING---------------- 
            foreach (var pair in TradesList.ListOfSubscribedPairs)  //for every pair start new thread and add it to Thread List
            {
                lt.RunWorker(pair);
            }
            lt.ClearingWorker();                     
        }
    }


    [DataContract]
    public class Trade
    {
        [DataMember(Name = "e")]
        public string EventType { get; set; } = "";
       
        [DataMember(Name = "E")]
        public float DispatchTime { get; set; }

       
        [DataMember(Name = "s")]
        public string Pair { get; set; } = "";

        [DataMember(Name = "t")]
        public float EventID { get; set; }

      
        [DataMember(Name = "p")]
        public string Price { get; set; }
       
        [DataMember(Name = "q")]
        public string Value { get; set; } = "";

       
        [DataMember(Name = "b")]
        public float Currency { get; set; }
       
       [DataMember(Name = "a")]
        public float BuyerID { get; set; }

      
        [DataMember(Name = "T")]
        public float SellerID { get; set; }
      
      
        [DataMember(Name = "m")]
        public bool BuyerMaker { get; set; }
       
        [DataMember(Name = "M")]
        public bool NotRelevant { get; set; }
    }   
    static class TradesList {

        static public int T = 5_000;   
        static  public  int N = 20;    
        static public object LockerOfResultList = new object();
        static public ObservableCollection<Trade> ListOfTrades { get; set; } =  new ObservableCollection<Trade>();
        static  public List<Trade> ListOfResultTrades = new();     
        static public List<string> ListOfSubscribedPairs = new List<string>();
        static public Dictionary<string, int> DictionaryOfCounts = new Dictionary<string, int>();       
    }

    class ListenerThread {
       
        public Thread thread;
        public bool Working = false;
        public ListenerThread(){}
    
        public void ClearingWorker()
        {
            //---------CLEANER----------------
            int clearingCount = 0;
            var CleanerTh = new Thread(() =>
            {
                clearingCount++;              
             
                while (true)
                {
                    Thread.Sleep(TradesList.T);
                    Console.WriteLine($"START CLEANING #{clearingCount++}...");

                    //перед началом очитски кленапим список результатов и дикшинари
                    TradesList.ListOfResultTrades.Clear();
                    foreach (var item in TradesList.ListOfSubscribedPairs)
                    {
                        TradesList.DictionaryOfCounts[item] = 0;
                    }
                   
                    lock (TradesList.LockerOfResultList)
                    {
                        foreach (var trade in TradesList.ListOfTrades.Reverse())
                        {
                            if (TradesList.DictionaryOfCounts[trade.Pair.ToLower()] < TradesList.N)
                            {
                                TradesList.ListOfResultTrades.Add((Trade)trade);
                                TradesList.DictionaryOfCounts[trade.Pair.ToLower()] += 1;
                            }
                        }
                        //PRINT RESULT
                        var idx = 0;
                        foreach (var item in TradesList.ListOfResultTrades)
                        {
                            Console.WriteLine($"#{idx++} - {item.EventType}" +
                                $"{item.EventID}" +
                                $"{item.Price}" +
                                $"{item.SellerID}" +
                                $"{item.BuyerMaker}");
                        }
                    }
                }
            });
            CleanerTh.Start();
           
        }

        public void  RunWorker(string pair)   //start thread
        {
            Debug.WriteLine($"START RUN FOR {pair}");

                thread = new Thread(() => {
                Working = true;    

                string SocketUrl = $"wss://stream.binance.com:9443/ws/{pair}@trade";
                WebSocket ws = new WebSocket(SocketUrl);
                Console.WriteLine($"Go {pair}");
                ws.OnOpen += (sender, j) =>
                    Console.WriteLine($"OnOpen {pair}");
                ws.OnMessage += (sender, j) =>
                {
                 
                    Debug.WriteLine(j.Data);
                    var trade = JsonConvert.DeserializeObject<Trade>(j.Data);                
                  
                   lock (TradesList.LockerOfResultList)
                    {
                        if (trade.BuyerMaker == true)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        Console.WriteLine($"Pair={trade.Pair} , " +
                                         $"Currency={trade.Currency} ," +
                                         $"EventID={trade.EventID} ," +                                     
                                         $"BuyerID={trade.BuyerID}");

                        TradesList.ListOfTrades.Add((Trade)trade);
                    }
                  
                    TradesList.DictionaryOfCounts[trade.Pair.ToLower()]  = TradesList.DictionaryOfCounts[trade.Pair.ToLower()]++;                                                        
                };
                ws.Connect();
                while (Working)
                {   //thread working while Working is true;
                }
            });
            thread.Start();              
        }
    }

    class Program
    {
        static void Main()
        {
            Menu menu = new();
            int choice = 0;          

            menu.ShowMenu();
            choice = menu.GetChoice(choice);      
            menu.MenuHandler(choice);
    }
    }
}