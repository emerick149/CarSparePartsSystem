using System.Collections.Generic;
using System.Linq;

namespace CarSparePartsSystem.Models
{
    public class CartItem
    {
        public int PartId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int MaxStock { get; set; }
    }

    public static class CartManager
    {
        public static List<CartItem> Cart { get; set; } = new List<CartItem>();

        public static void AddToCart(CartItem item)
        {
            var existing = Cart.Find(c => c.PartId == item.PartId);
            if (existing != null)
            {
                if (existing.Quantity + item.Quantity <= existing.MaxStock)
                    existing.Quantity += item.Quantity;
            }
            else
            {
                Cart.Add(item);
            }
        }

        public static decimal GetTotal() => Cart.Sum(item => item.Price * item.Quantity);
        public static void Clear() => Cart.Clear();
    }
}