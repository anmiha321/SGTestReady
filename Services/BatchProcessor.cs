using System.Text.RegularExpressions;
using SGtest.Data;
using SGtest.Models;
using Microsoft.EntityFrameworkCore;

namespace SGtest.Services
{
    public class BatchProcessor
    {
        private readonly AppDbContext _dbContext;
        private const int BatchSize = 1000;
        
        public BatchProcessor(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public void ProcessFileInBatches(string filePath, Action<List<string>> processBatch)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"File {filePath} not found");
                return;
            }
            
            try
            {
                using (var streamReader = new StreamReader(filePath))
                {
                    streamReader.ReadLine();

                    var currentBatch = new List<string>();

                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            currentBatch.Add(line);
                        }
                        
                        if (currentBatch.Count >= BatchSize || streamReader.EndOfStream)
                        {
                            if (currentBatch.Count > 0)
                            {
                                try
                                {
                                    processBatch(currentBatch);
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine($"Error processing batch: {ex.Message}");
                                }

                                currentBatch.Clear();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during file processing: {ex.Message}");
            }
        }
        
        public void ImportJobTitlesInBatches(string filePath)
        {
            Action<List<string>> processBatch = batch =>
            {
                var jobTitlesToAdd = new List<JobTitle>();
                var existingTitles = _dbContext.JobTitles.Select(j => j.Name.ToLower()).ToHashSet();

                foreach (var line in batch)
                {
                    var name = line.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (!existingTitles.Contains(name.ToLower()))
                    {
                        jobTitlesToAdd.Add(new JobTitle { Name = name });
                        existingTitles.Add(name.ToLower());
                    }
                }

                if (jobTitlesToAdd.Count > 0)
                {
                    _dbContext.JobTitles.AddRange(jobTitlesToAdd);
                    _dbContext.SaveChanges();
                }
            };
            
            ProcessFileInBatches(filePath, processBatch);
        }
        
        public void ImportDepartmentsInBatches(string filePath)
        {
            var existingDepartments = _dbContext.Departments
                .Select(d => new { d.Id, d.Name, d.ParentId })
                .ToList();
                
            // Create a dictionary that can look up departments by both full key (name+parentId) and Name to determine uniqueness of the records
            var departmentDictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var dept in existingDepartments)
            {
                string fullKey = $"{dept.Name.ToLower()}_{dept.ParentId}";
                departmentDictionary[fullKey] = dept.Id;
                
                string nameKey = dept.Name.ToLower();
                departmentDictionary[nameKey] = dept.Id;
            }
            
            Action<List<string>> processBatch = batch =>
            {
                var newDepartments = new List<Department>();
                var updatedDepartments = new List<Department>();

                foreach (var line in batch)
                {
                    try
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length < 3)
                            continue;

                        string name = parts[0].Trim();
                        string parentName = parts[1].Trim();
                        string phone = parts.Length > 3 ? Regex.Replace(parts[3].Trim(), @"[^\d]", "") : null;

                        int? parentId = null;
                        if (!string.IsNullOrWhiteSpace(parentName) &&
                            departmentDictionary.TryGetValue(parentName.ToLower(), out int pId))
                        {
                            parentId = pId;
                        }

                        int? managerId = null;

                        string fullKey = $"{name.ToLower()}_{parentId}";
                        string nameKey = name.ToLower();

                        if (departmentDictionary.TryGetValue(fullKey, out int existingIdByFullKey))
                        {
                            // update department with the same parent key and name
                            var dept = new Department
                            {
                                Id = existingIdByFullKey,
                                Name = name,
                                ParentId = parentId,
                                ManagerId = managerId,
                                Phone = phone
                            };

                            updatedDepartments.Add(dept);
                        }
                        else if (departmentDictionary.TryGetValue(nameKey, out int existingIdByName))
                        {
                            // update department with the existed name and change his parent id
                            var dept = new Department
                            {
                                Id = existingIdByName,
                                Name = name,
                                ParentId = parentId,
                                ManagerId = managerId,
                                Phone = phone
                            };

                            updatedDepartments.Add(dept);

                            // Update the dictionary to a new relationship key+name
                            departmentDictionary[fullKey] = existingIdByName;
                        }
                        else
                        {
                            var dept = new Department
                            {
                                Name = name,
                                ParentId = parentId,
                                ManagerId = managerId,
                                Phone = phone
                            };

                            newDepartments.Add(dept);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error parsing department line: {ex.Message}");
                    }
                }

                if (newDepartments.Count > 0)
                {
                    _dbContext.Departments.AddRange(newDepartments);
                    _dbContext.SaveChanges();

                    // Update ductionary with a new departments
                    foreach (var dept in newDepartments)
                    {
                        string fullKey = $"{dept.Name.ToLower()}_{dept.ParentId}";
                        string nameKey = dept.Name.ToLower();

                        departmentDictionary[fullKey] = dept.Id;
                        departmentDictionary[nameKey] = dept.Id;

                        existingDepartments.Add(new { dept.Id, dept.Name, dept.ParentId });
                    }
                }

                if (updatedDepartments.Count > 0)
                {
                    foreach (var dept in updatedDepartments)
                    {
                        _dbContext.Departments.Attach(dept);
                        _dbContext.Entry(dept).Property(d => d.ParentId).IsModified = true;
                        _dbContext.Entry(dept).Property(d => d.Phone).IsModified = true;
                    }

                    _dbContext.SaveChanges();
                }
            };
            
            ProcessFileInBatches(filePath, processBatch);
            
            _dbContext.ChangeTracker.Clear();
            
            ProcessFileInBatches(filePath, processBatch);
            UpdateDepartmentManagers();
        }
        
        private void UpdateDepartmentManagers()
        {
            var departments = _dbContext.Departments
                .Include(d => d.Manager)
                .ThenInclude(m => m.JobTitle)
                .ToList();
                
            var employees = _dbContext.Employees
                .Include(e => e.JobTitle)
                .ToList();
                
            foreach (var dept in departments)
            {
                var manager = employees.FirstOrDefault(e => e.DepartmentId == dept.Id && 
                    e.JobTitleId.HasValue);
                    
                if (manager != null)
                {
                    dept.ManagerId = manager.Id;
                    dept.Manager = manager;
                }
            }
            
            _dbContext.SaveChanges();
        }
        
        public void ImportEmployeesInBatches(string filePath)
        {
            var jobTitles = _dbContext.JobTitles.ToDictionary(
                j => j.Name.ToLower(),
                j => j.Id
            );
            
            var departments = _dbContext.Departments.ToDictionary(
                d => d.Name.ToLower(),
                d => d.Id
            );
            
            var existingEmployees = _dbContext.Employees
                .Select(e => new { e.Id, e.FullName })
                .ToDictionary(
                    e => e.FullName.ToLower(),
                    e => e.Id
                );

            Action<List<string>> processBatch = batch =>
            {
                var newEmployees = new List<Employee>();
                var updatedEmployees = new List<Employee>();

                foreach (var line in batch)
                {
                    try
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length < 5)
                            continue;

                        string departmentName = parts[0].Trim();
                        string fullName = parts[1].Trim();
                        string login = parts[2].Trim();
                        string password = parts[3].Trim();
                        string jobTitleName = parts[4].Trim();

                        if (string.IsNullOrWhiteSpace(fullName))
                            continue;

                        int? departmentId = null;
                        if (!string.IsNullOrWhiteSpace(departmentName) &&
                            departments.TryGetValue(departmentName.ToLower(), out int deptId))
                        {
                            departmentId = deptId;
                        }

                        int? jobTitleId = null;
                        if (!string.IsNullOrWhiteSpace(jobTitleName) &&
                            jobTitles.TryGetValue(jobTitleName.ToLower(), out int titleId))
                        {
                            jobTitleId = titleId;
                        }

                        if (existingEmployees.TryGetValue(fullName.ToLower(), out int existingId))
                        {
                            var employee = new Employee
                            {
                                Id = existingId,
                                FullName = fullName,
                                Login = login,
                                Password = password,
                                DepartmentId = departmentId,
                                JobTitleId = jobTitleId
                            };

                            updatedEmployees.Add(employee);
                        }
                        else
                        {
                            var employee = new Employee
                            {
                                FullName = fullName,
                                Login = login,
                                Password = password,
                                DepartmentId = departmentId,
                                JobTitleId = jobTitleId
                            };

                            newEmployees.Add(employee);
                            existingEmployees[fullName.ToLower()] = -1; // Placeholder until saved
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error parsing employee line: {ex.Message}");
                    }
                }

                if (newEmployees.Count > 0)
                {
                    _dbContext.Employees.AddRange(newEmployees);
                    _dbContext.SaveChanges();

                    // Update dictionary with a new ids
                    foreach (var emp in newEmployees)
                    {
                        existingEmployees[emp.FullName.ToLower()] = emp.Id;
                    }
                }

                if (updatedEmployees.Count > 0)
                {
                    foreach (var emp in updatedEmployees)
                    {
                        _dbContext.Employees.Attach(emp);
                        _dbContext.Entry(emp).Property(e => e.Login).IsModified = true;
                        _dbContext.Entry(emp).Property(e => e.Password).IsModified = true;
                        _dbContext.Entry(emp).Property(e => e.DepartmentId).IsModified = true;
                        _dbContext.Entry(emp).Property(e => e.JobTitleId).IsModified = true;
                    }

                    _dbContext.SaveChanges();
                }
            };
            
            ProcessFileInBatches(filePath, processBatch);
            UpdateDepartmentManagers();
        }
    }
}