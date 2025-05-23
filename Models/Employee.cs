﻿namespace SGtest.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public int? DepartmentId { get; set; }
    public int? JobTitleId { get; set; }
    
    public Department Department { get; set; }
    public JobTitle JobTitle { get; set; }
    public Department ManagedDepartment { get; set; }
}