using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

public class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = new()
    {
        new Customer 
        { 
            CMND = "0012345678", 
            Ho = "Nguyen Van", 
            Ten = "A", 
            NgaySinh = new DateTime(1990, 1, 15), 
            DiaChi = "123 Le Loi", 
            NgayCap = new DateTime(2008, 1, 20), 
            SDT = "0901234567", 
            Phai = "Nam", 
            MaCN = "BENTHANH" 
        },
        new Customer 
        { 
            CMND = "0023456789", 
            Ho = "Tran Thi", 
            Ten = "B", 
            NgaySinh = new DateTime(1992, 5, 10), 
            DiaChi = "456 Nguyen Hue", 
            NgayCap = new DateTime(2010, 5, 15), 
            SDT = "0902345678", 
            Phai = "Nữ", 
            MaCN = "BENTHANH" 
        },
        new Customer 
        { 
            CMND = "0034567890", 
            Ho = "Le Van", 
            Ten = "C", 
            NgaySinh = new DateTime(1985, 8, 25), 
            DiaChi = "789 Tran Hung Dao", 
            NgayCap = new DateTime(2003, 9, 1), 
            SDT = "0903456789", 
            Phai = "Nam", 
            MaCN = "TANDINH" 
        },
        new Customer 
        { 
            CMND = "0045678901", 
            Ho = "Pham Thi", 
            Ten = "D", 
            NgaySinh = new DateTime(1995, 3, 12), 
            DiaChi = "321 Hai Ba Trung", 
            NgayCap = new DateTime(2013, 3, 20), 
            SDT = "0904567890", 
            Phai = "Nữ", 
            MaCN = "TANDINH" 
        },
        new Customer 
        { 
            CMND = "0056789012", 
            Ho = "Khach", 
            Ten = "Hang", 
            NgaySinh = new DateTime(1988, 7, 8), 
            DiaChi = "999 Customer St", 
            NgayCap = new DateTime(2006, 7, 15), 
            SDT = "0905678901", 
            Phai = "Nam", 
            MaCN = "BENTHANH" 
        }
    };

    public Task<List<Customer>> GetCustomersByBranchAsync(string branchCode)
    {
        var customers = _customers.Where(c => c.MaCN == branchCode).ToList();
        return Task.FromResult(customers);
    }

    public Task<List<Customer>> GetAllCustomersAsync()
    {
        return Task.FromResult(_customers.ToList());
    }

    public Task<Customer?> GetCustomerByCMNDAsync(string cmnd)
    {
        var customer = _customers.FirstOrDefault(c => c.CMND == cmnd);
        return Task.FromResult(customer);
    }

    public Task<bool> AddCustomerAsync(Customer customer)
    {
        if (_customers.Any(c => c.CMND == customer.CMND))
            return Task.FromResult(false);

        _customers.Add(customer);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateCustomerAsync(Customer customer)
    {
        var existing = _customers.FirstOrDefault(c => c.CMND == customer.CMND);
        if (existing == null)
            return Task.FromResult(false);

        existing.Ho = customer.Ho;
        existing.Ten = customer.Ten;
        existing.NgaySinh = customer.NgaySinh;
        existing.DiaChi = customer.DiaChi;
        existing.NgayCap = customer.NgayCap;
        existing.SDT = customer.SDT;
        existing.Phai = customer.Phai;
        existing.MaCN = customer.MaCN;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteCustomerAsync(string cmnd)
    {
        var customer = _customers.FirstOrDefault(c => c.CMND == cmnd);
        if (customer == null)
            return Task.FromResult(false);

        customer.TrangThaiXoa = 1;
        return Task.FromResult(true);
    }

    public Task<bool> RestoreCustomerAsync(string cmnd)
    {
        var customer = _customers.FirstOrDefault(c => c.CMND == cmnd);
        if (customer == null)
            return Task.FromResult(false);

        customer.TrangThaiXoa = 0;
        return Task.FromResult(true);
    }
}
