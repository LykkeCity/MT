using Common.Log;
using System;
using System.Threading.Tasks;

namespace MarginTrading.SqlMigration
{
    class Program
    {
        static MigrationService _service;
        static ILog _log;
        static void Main(string[] args)
        {
            _log = new LogToConsole();

            Log("Initializing Migration Service...");
            _service = new MigrationService(_log);

            ShowHelp();
            Log("Awaiting command...");

            string input;
            do
            {   
                input = Console.ReadLine();
                switch (input)
                {
                    case "help":
                        ShowHelp();
                        break;
                    case "1":
                        try
                        {
                            Log("Migration table MarginTradingAccountTransactionsReports...");
                            long records = 0;
                            Task.Run(async () => records =  await _service.Migrate_MarginTradingAccountTransactionsReports())
                                .Wait();
                            Log($"Migration finished. Migrated {records} records");
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                        break;
                    case "2":
                        try
                        {
                            Log("Migration table AccountMarginEventsReports...");
                            long records = 0;
                            Task.Run(async () => records = await _service.Migrate_AccountMarginEventsReports())
                                .Wait();
                            Log($"Migration finished. Migrated {records} records");
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                        break;
                    case "3":
                        try
                        {
                            Log("Migration table ClientAccountsReports...");
                            long records = 0;
                            Task.Run(async () => records = await _service.Migrate_ClientAccountsReports())
                                .Wait();
                            Log($"Migration finished. Migrated {records} records");
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                        break;
                    case "4":
                        try
                        {
                            Log("Migration table ClientAccountsStatusReports...");
                            long records = 0;
                            Task.Run(async () => records = await _service.Migrate_ClientAccountsStatusReports())
                                .Wait();
                            Log($"Migration finished. Migrated {records} records");
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                        break;
                        
                    default:
                        break;
                }
            } while (input != "exit");

        }

        private static void ShowHelp()
        {
            Console.WriteLine("*****help*****");
            Console.WriteLine("Commands:");
            Console.WriteLine("\thelp = show this help");
            Console.WriteLine("\texit = stop application");
            Console.WriteLine("\t1 = Migrate <<MarginTradingAccountTransactionsReports>> table");
            Console.WriteLine("\t2 = Migrate <<AccountMarginEventsReports>> table");
            Console.WriteLine("\t3 = Migrate <<ClientAccountsReports>> table");
            Console.WriteLine("\t4 = Migrate <<ClientAccountsStatusReports>> table");

            Console.WriteLine("**************");
        }

        public static void Log(string text, params string[] pars)
        {
            Console.WriteLine("{0}|INFO|{1}", DateTime.UtcNow.ToString("HH:mm:ss.fff"), string.Format(text, pars));
        }
        public static void Log(Exception ex)
        {
            Console.WriteLine("{0}|ERROR|{1}", DateTime.UtcNow.ToString("HH:mm:ss.fff"), ex.Message + "\n" + ex.StackTrace);
        }
    }
}
