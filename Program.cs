using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Data.SqlClient;
using System.Text;
using System.Threading;

namespace dbtest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Use ConfigBuilder for settings with env var overrides
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            // Get number of loops for the test program
            bool parseLoops = Int32.TryParse(config["iLoops"], out int iLoops);
            if (parseLoops && iLoops > 1)
            {
                Console.WriteLine("Loops: {0}", iLoops);
            }
            else
            {
                Console.WriteLine("Invalid loop value, exiting...");
                Environment.Exit(1);
            }

            // Get the CLI Override 
            bool parseCliOverride = Int32.TryParse(config["iCliOverride"], out int iCliOverride);
            if (parseCliOverride && (iCliOverride == 0 || iCliOverride == 1))
            {
                Console.WriteLine("CLI Override: {0}", iCliOverride);
            }
            else
            {
                Console.WriteLine("Invalid loop value, exiting...");
                Environment.Exit(1);
            }

            // Get the read only mode setting
            Boolean.TryParse(config["isReadOnly"], out bool isReadOnly);

            // Get the sleep interval
            Int32.TryParse(config["iMsPause"], out int iMsPause);

            // Get the failover mode setting
            Boolean.TryParse(config["isFailover"], out bool isFailover);

            // Set db connection values
            String dbDatabase = config["db:database"];
            String dbUserId = config["db:userId"];
            String dbPassword = config["db:password"];

            String dbServer;
            if (!isFailover)
            {
                dbServer = config["db:primaryServer"];
            }
            else
            {
                dbServer = config["db:secondaryServer"];
            }

            Console.WriteLine(config.ToString());

            // Start the SQL exercise
            try
            {
                // Set up SQL connection string
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                Console.WriteLine("\nBuilding SQL connection with:");
                Console.WriteLine("Server: {0}", dbServer);
                Console.WriteLine("Database: {0}", dbDatabase);
                Console.WriteLine("User: {0}", dbUserId);
                // Console.WriteLine("Password: {0}\n", dbPassword);

                builder.DataSource = dbServer;
                builder.UserID = dbUserId;
                builder.Password = dbPassword;
                builder.InitialCatalog = dbDatabase;

                // Console.WriteLine("String: {0}, ", builder.ConnectionString);

                // Set up the SQL connection
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    Console.WriteLine("\nQuery Log:");
                    Console.WriteLine("=========================================\n");

                    connection.Open();

                    // Perform database read/writes
                    for (int i = 0; i < iLoops; i++)
                    {
                        // Write to the database
                        if (!isReadOnly)
                        {
                            String strTimeNow = DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss.fffffffK");

                            StringBuilder sbWrite = new StringBuilder();
                            sbWrite.Append("INSERT INTO [Demo] ([DemoName], [DemoContent]) ");
                            sbWrite.Append("VALUES (@demoName, @demoContent);");
                            String sqlWrite = sbWrite.ToString();

                            using (SqlCommand command = new SqlCommand(sqlWrite, connection))
                            {
                                Console.WriteLine("Writing {0} to the database...", strTimeNow);
                                command.Parameters.AddWithValue("demoName", strTimeNow);
                                command.Parameters.AddWithValue("demoContent", strTimeNow);
                                int response = command.ExecuteNonQuery();
                            }
                        }

                        // Read from the database
                        StringBuilder sbRead = new StringBuilder();
                        sbRead.Append("SELECT TOP 1 [DemoId], [DemoName], [DemoContent] ");
                        sbRead.Append("FROM [Demo] ORDER BY [DemoId] DESC;");
                        String sqlRead = sbRead.ToString();

                        using (SqlCommand command = new SqlCommand(sqlRead, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Console.WriteLine("Reading last entry from database...");
                                    Console.WriteLine("{0} {1} {2}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2
                                        ));
                                }
                            }
                        }
                        // Sleep for one second
                        Thread.Sleep(iMsPause);
                    }
                    connection.Close();

                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            
            if (iCliOverride == 1)
            {
                Console.WriteLine("\nDone. Press Enter.");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("\nDone.");
            }
            
        }
    }
}
