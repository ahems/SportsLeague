using System;
using System.Collections.Generic;

namespace Contoso.App.SportsLeague.Functions.Models
{
    public class Category    {
        public string CategoryId { get; set; } 
        public string DataType { get { return "Category"; } } 
        public string Description { get; set; } 
        public string Name { get; set; } 
        public string id { get; set; } 
}

    public class Product    {
        public string CategoryId { get; set; } 
        public string DataType { get { return "Product"; } }
        public string Description { get; set; } 
        public string ImagePath { get; set; } 
        public string Name { get; set; } 
        public string ProductId { get; set; } 
        public string ThumbnailPath { get; set; } 
        public double UnitPrice { get; set; } 
        public string id { get; set; } 
    }
    
        public class OrderDetail    {
        public int LineItemNumber { get; set; } 
        public string ProductId { get; set; } 
        public int Quantity { get; set; } 
        public double UnitPrice { get; set; } 
    }

    public class Order    {
        public string Address { get; set; } 
        public string City { get; set; } 
        public string Country { get; set; } 
        public string DataType { get { return "Order"; } }
        public string Email { get; set; } 
        public string FirstName { get; set; } 
        public bool HasBeenShipped { get; set; } 
        public string LastName { get; set; } 
        public DateTime OrderDate { get; set; } 
        public ICollection<OrderDetail> OrderDetails { get; set; } 
        public string PaymentTransdactionId { get; set; } 
        public string Phone { get; set; } 
        public string PostalCode { get; set; } 
        public string ReceiptUrl { get; set; } 
        public string SMSOptIn { get; set; } 
        public string SMSStatus { get; set; } 
        public string State { get; set; } 
        public double Total { get; set; } 
        public string id { get; set; } 
    }

       public class CartItem    {
        public string CartItemId { get; set; } 
        public string ProductId { get; set; } 
        public int Quantity { get; set; } 
    }

    public class Cart    {
        public List<CartItem> CartItems { get; set; } 
        public string DataType { get { return "Cart"; } }
        public DateTime DateCreated { get; set; } 
        public string id { get; set; } 
    }
}