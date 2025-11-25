using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class CustomerService : ICustomerService
{
    private readonly List<Customer> _customers = new()
    {
        new Customer { CMND = "123456", Ho = "Nguyen Van", Ten = "A", DiaChi = "123 Le Loi", SDT = "0901234567", Phai = "Nam", MaCN = "BENTHANH" },
        new Customer { CMND = "234567", Ho = "Tran Thi", Ten = "B", DiaChi = "456 Nguyen Hue", SDT = "0902345678", Phai = "Nu", MaCN = "BENTHANH" },
        new Customer { CMND = "345678", Ho = "Le Van", Ten = "C", DiaChi = "789 Tran Hung Dao", SDT = "0903456789", Phai = "Nam", MaCN = "TANDINH" },
        new Customer { CMND = "456789", Ho = "Pham Thi", Ten = "D", DiaChi = "321 Hai Ba Trung", SDT = "0904567890", Phai = "Nu", MaCN = "TANDINH" },
        new Customer { CMND = "c123456", Ho = "Khach", Ten = "Hang", DiaChi = "999 Customer St", SDT = "0905678901", Phai = "Nam", MaCN = "BENTHANH" }
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
        existing.DiaChi = customer.DiaChi;
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

        _customers.Remove(customer);
        return Task.FromResult(true);
    }
}
