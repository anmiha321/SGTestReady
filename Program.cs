using System.Text;
using SGtest.Config;
using SGtest.Services;
using Microsoft.EntityFrameworkCore;
using SGtest.Data;

namespace SGtest
{ 
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                // configurating db conection
                var appSettings = ConfigurationManager.LoadConfiguration();
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseNpgsql(appSettings.Database.GetConnectionString());

                using (var dbContext = new AppDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();

                    if (args.Length == 0)
                    {
                        PrintUsage();
                        return;
                    }

                    string command = args[0].ToLower();

                    if (command == "import")
                    {
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Error: Import command requires file and type arguments");
                            PrintUsage();
                            return;
                        }

                        string filePath = null;
                        string importType = null;
                        
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (args[i].StartsWith("--file="))
                            {
                                filePath = args[i].Substring("--file=".Length);
                            }
                            else if (args[i].StartsWith("--type="))
                            {
                                importType = args[i].Substring("--type=".Length).ToLower();
                            }
                        }

                        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(importType))
                        {
                            Console.WriteLine("Error: Missing required arguments for import command");
                            PrintUsage();
                            return;
                        }

                        ImportData(dbContext, filePath, importType);
                    }
                    else if (command == "print")
                    {
                        int? departmentId = null;
                        
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (args[i].StartsWith("--department="))
                            {
                                string deptIdStr = args[i].Substring("--department=".Length);
                                if (int.TryParse(deptIdStr, out int deptId))
                                {
                                    departmentId = deptId;
                                }
                            }
                        }

                        PrintStructure(dbContext, departmentId);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown command: {command}");
                        PrintUsage();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  SGtest import --file=<file_path> --type=<data_type>");
            Console.WriteLine("  SGtest print [--department=<department_id>]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  import    Import data from a TSV file");
            Console.WriteLine("  print     Print the current structure of data");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --file=<file_path>             Path to the TSV file to import");
            Console.WriteLine("  --type=<data_type>             Type of data to import (department, employee, or jobtitle)");
            Console.WriteLine("  --department=<department_id>   ID of department to print");
        }

        private static void ImportData(AppDbContext dbContext, string filePath, string importType)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }
            
            try
            {
                // Batch import for large files
                var batchProcessor = new BatchProcessor(dbContext);
                
                if (importType == "department")
                    batchProcessor.ImportDepartmentsInBatches(filePath);
                else if (importType == "employee")
                    batchProcessor.ImportEmployeesInBatches(filePath);
                else if (importType == "jobtitle")
                    batchProcessor.ImportJobTitlesInBatches(filePath);
                else
                {
                    Console.WriteLine($"Error: Invalid import type: {importType}");
                    Console.WriteLine("Valid types are: department, employee, jobtitle");
                    return;
                }
                
                Console.WriteLine("Иморт успешен.");
                
                
                Console.WriteLine("\n Структура базы данных:");
                Console.WriteLine("======================");
                var printer = new StructurePrinter(dbContext);
                printer.PrintStructure();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during import: {ex.Message}");
            }
        }

        private static void PrintStructure(AppDbContext dbContext, int? departmentId)
        {
            try
            {
                var printer = new StructurePrinter(dbContext);
                
                if (departmentId.HasValue)
                {
                    Console.WriteLine($"PСтруктура базы данных по DepartmentID = {departmentId}:");
                }
                else
                {
                    Console.WriteLine("Структура базы данных:");
                }
                
                Console.WriteLine("======================");
                printer.PrintStructure(departmentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing structure: {ex.Message}");
            }
        }
    }
}