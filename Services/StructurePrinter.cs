using SGtest.Data;
using SGtest.Models;
using Microsoft.EntityFrameworkCore;

namespace SGtest.Services
{
    public class StructurePrinter
    {
        private readonly AppDbContext _dbContext;

        public StructurePrinter(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void PrintStructure(int? departmentId = null)
        {
            try
            {
                if (departmentId.HasValue)
                {
                    PrintDepartmentChain(departmentId.Value);
                }
                else
                {
                    // Get main departments (ParentId = null) aka parents for children
                    var rootDepartments = _dbContext.Departments
                        .Include(d => d.Manager)
                            .ThenInclude(m => m.JobTitle)
                        .Include(d => d.Employees)
                            .ThenInclude(e => e.JobTitle)
                        .Where(d => d.ParentId == null)
                        .OrderBy(d => d.Name)
                        .ToList();

                    foreach (var department in rootDepartments)
                    {
                        PrintDepartmentHierarchy(department, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing structure: {ex.Message}");
            }
        }

        private void PrintDepartmentChain(int departmentId)
        {
            try
            {
                // get data for department, manager, and employee
                var department = _dbContext.Departments
                    .Include(d => d.Manager)
                        .ThenInclude(m => m.JobTitle)
                    .Include(d => d.Employees)
                        .ThenInclude(e => e.JobTitle)
                    .FirstOrDefault(d => d.Id == departmentId);

                if (department == null)
                {
                    Console.WriteLine($"Department with ID {departmentId} not found.");
                    return;
                }

                // Build the parent chain (департамент департамента)
                var parentChain = new List<Department>();
                var currentDept = department;
                
                while (currentDept.ParentId.HasValue)
                {
                    var parent = _dbContext.Departments
                        .Include(d => d.Manager)
                            .ThenInclude(m => m.JobTitle)
                        .FirstOrDefault(d => d.Id == currentDept.ParentId);
                    
                    if (parent == null)
                        break;
                    
                    parentChain.Add(parent);
                    currentDept = parent;
                }

                // Print parent chain in reverse order
                int level = 0;
                for (int i = parentChain.Count - 1; i >= 0; i--)
                {
                    PrintDepartment(parentChain[i], level++, false);
                }

                // Print the specified department with employees
                PrintDepartment(department, level, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing department chain: {ex.Message}");
            }
        }

        private void PrintDepartmentHierarchy(Department department, int level, bool includeEmployees = true)
        {
            // Print department info
            PrintDepartment(department, level, includeEmployees);

            // Get and print subdepartments (вложеность)
            var children = _dbContext.Departments
                .Include(d => d.Manager)
                    .ThenInclude(m => m.JobTitle)
                .Include(d => d.Employees)
                    .ThenInclude(e => e.JobTitle)
                .Where(d => d.ParentId == department.Id)
                .OrderBy(d => d.Name)
                .ToList();

            foreach (var child in children)
            {
                PrintDepartmentHierarchy(child, level + 1, includeEmployees);
            }
        }

        private void PrintDepartment(Department department, int level, bool includeEmployees)
        {
            try
            {
                // load data with relationships
                department = _dbContext.Departments
                    .Include(d => d.Manager)
                        .ThenInclude(e => e.JobTitle)
                    .Include(d => d.Employees)
                        .ThenInclude(e => e.JobTitle)
                    .FirstOrDefault(d => d.Id == department.Id);

                if (department == null)
                {
                    Console.WriteLine($"Department with ID could not be loaded.");
                    return;
                }

                // вложеность из = (в зависимости от уровня вложености)
                string indent = new string('=', level + 1);
                Console.WriteLine($"{indent} {department.Name} ID={department.Id}");
                
                if (department.Manager != null)
                {
                    string managerIndent = new string(' ', level);
                    Console.WriteLine($"{managerIndent}* {department.Manager.FullName} ID={department.Manager.Id} ({department.Manager.JobTitle.Name} ID={department.Manager.JobTitleId})");                }
                
                if (includeEmployees && department.Employees != null)
                {
                    var employees = department.Employees
                        .Where(e => e != null && e.Id != department.ManagerId)
                        .OrderBy(e => e.FullName)
                        .ToList();

                    string employeeIndent = new string(' ', level);
                    foreach (var employee in employees)
                    {
                        if (employee.JobTitleId.HasValue)
                        {
                            Console.WriteLine($"{employeeIndent}- {employee.FullName} ID={employee.Id} ({employee.JobTitle.Name} ID={employee.JobTitleId})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing department: {ex.Message}");
            }
        }
    }
}