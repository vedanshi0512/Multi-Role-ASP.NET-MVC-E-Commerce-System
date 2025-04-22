using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StationeryEcommerceApp.Models
{
    public class CartItem
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Nullable<decimal> Price { get; set; }

        public int Quantity { get; set; }
        public string ImageUrl { get; set; } // Image URL for cart display
        public int MerchantID { get; set; }
    }
}