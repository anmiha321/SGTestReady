namespace SGtest.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public int? ManagerId { get; set; }
    public string Phone { get; set; }
    
    public Department Parent { get; set; }
    public Employee Manager { get; set; }
    public ICollection<Department> Children { get; set; } = new List<Department>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}