﻿using Models;

namespace Xanh_Dau.DTO;

public class AdminOrderDTO
{
    public Customer Customer { get; set; }
    public List<Order> ListOrder { get; set; } = new();
    public List<Customer> ListCustomers { get; set; } = new();
}