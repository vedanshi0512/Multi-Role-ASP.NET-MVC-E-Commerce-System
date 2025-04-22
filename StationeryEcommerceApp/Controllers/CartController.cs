using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using StationeryEcommerceApp.Models; // Ensure models are imported
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Diagnostics;
using static iTextSharp.text.pdf.AcroFields;
using static System.Collections.Specialized.BitVector32;
using System.Data.Entity.Validation;


public class CartController : Controller
{
    private StationeryEcommerceDBEntities1 db = new StationeryEcommerceDBEntities1();

    // GET: View Cart
    public ActionResult Index()
    {
        var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
        ViewBag.Total = cart.Sum(item => item.Price * item.Quantity); // Calculate total price
        return View(cart);
    }

    // POST: Add to Cart
    [HttpPost]
    
    public ActionResult AddToCart(int id)
    {
        var product = db.Products.Find(id);
        if (product == null) return HttpNotFound();

        var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

        var cartItem = cart.FirstOrDefault(c => c.ProductID == id);
        if (cartItem != null)
        {
            cartItem.Quantity++; // If product already in cart, increase quantity
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductID = product.ProductID,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                Quantity = 1,
                ImageUrl = product.ImageUrl
            });
        }

        Session["Cart"] = cart; // Save cart in session

        // Stay on the same page
        return Redirect(Request.UrlReferrer.ToString());
    }


    // POST: Update Quantity

    //public ActionResult UpdateCart(int id, int quantity)
    //{
    //    var cart = Session["Cart"] as List<CartItem>;
    //    var item = cart.FirstOrDefault(c => c.ProductID == id);
    //    if (cart != null)
    //    {
    //        //var item = cart.FirstOrDefault(c => c.ProductID == id);
    //        if (item != null && quantity > 0)
    //        {
    //            item.Quantity = quantity;
    //        }
    //    }
    //    Session["Cart"] = cart;
    //    return Json(new { success = true, total = item.Quantity * item.Price, cartCount = cart.Sum(c => c.Quantity) });
    //}
    [HttpPost]
    public ActionResult UpdateCart(int id, int quantityChange)
    {
        var cart = Session["Cart"] as List<CartItem>;

        if (cart != null)
        {
            var item = cart.FirstOrDefault(c => c.ProductID == id);
            if (item == null)
            {
                // If item is not in cart, add it with default quantity 1
                item = new CartItem { ProductID = id, Quantity = 1 };
                cart.Add(item);
            }
            else
            {
                //Adjust quantity based on button action
                if (quantityChange == +1)
                {
                    item.Quantity++;
                }
                else if (quantityChange == -1)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0)
                    {
                        cart.Remove(item); // Remove item if quantity is 0
                    }
                }
                ViewBag.QuantityChange = quantityChange;
                // Save updated cart
            }
            Session["Cart"] = cart;
        }

        return RedirectToAction("Index");
    }
    //return RedirectToAction("Index");


    // POST: Remove Item
    public ActionResult RemoveFromCart(int id)
    {
        var cart = Session["Cart"] as List<CartItem>;
        if (cart != null)
        {
            var item = cart.FirstOrDefault(c => c.ProductID == id);
            if (item != null)
            {
                cart.Remove(item);
            }
        }
        Session["Cart"] = cart;
        return RedirectToAction("Index");
    }

    // GET: Clear Cart
    public ActionResult ClearCart()
    {
        Session["Cart"] = null;
        return RedirectToAction("Index");
    }
    public ActionResult Checkout()
    {
        if (Session["UserID"] == null)
        {
            return RedirectToAction("Login","User");
        }

        int userId = (int)Session["UserID"];

        var user = db.Users.Find(userId);
        if (user == null)
        {
            return RedirectToAction("Login","User");
        }

        // Fetch user's saved addresses
        var addresses = db.Addresses.Where(a => a.UserID == userId).ToList();

        // Retrieve cart items from session
        var cart = Session["Cart"] as List<CartItem>;

        ViewBag.User = user;
        ViewBag.Addresses = addresses;
        ViewBag.CartItems = cart;

        return View(addresses);
    }

    [HttpPost]
    public ActionResult ConfirmPayment(int? SelectedAddressID, string FullAddress, string City, string State, string PostalCode, string Country)
    {
        if (Session["Cart"] == null || Session["UserID"] == null)
        {
            return RedirectToAction("Index", "Cart");
        }

        int userId = (int)Session["UserID"];
        var user = db.Users.Find(userId);
        var cart = Session["Cart"] as List<CartItem>;


        if (user == null)
        {
            return RedirectToAction("Login", "User");
        }


        if (cart == null || cart.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }
        var selectedAddress = db.Addresses.FirstOrDefault(a => a.AddressID == SelectedAddressID && a.UserID == userId);
        if (SelectedAddressID != null && SelectedAddressID > 0)
        {
            var existingAddress = db.Addresses.Find(SelectedAddressID);
            if (existingAddress == null)
            {
                return RedirectToAction("Index", "Cart"); // Address not found
            }
        }
        // **CASE 2: New Address - Save it first**
        else if (SelectedAddressID == 0)
        {
            var newAddress = new Address
            {
                UserID = userId,
                FullAddress = FullAddress,
                City = City,
                State = State,
                PostalCode = PostalCode,
                Country = Country
                
            };

            db.Addresses.Add(newAddress);
            db.SaveChanges();

            // Set new address ID for order
            SelectedAddressID = newAddress.AddressID;
        }

        //if (selectedAddress == null)
        //{
        //    return RedirectToAction("Checkout", "Cart"); // Redirect back if address is invalid
        //}
        //int finalAddressID = 0;
        //if (SelectedAddressID == 0 || SelectedAddressID == null)
        //{
        //    var newAddress = new Address
        //    {
        //        UserID = userId,
        //        FullAddress = FullAddress,
        //        City = City,
        //        State = State,
        //        PostalCode = PostalCode,
        //        Country = Country,

        //    };
        //    db.Addresses.Add(newAddress);

        //    // Use new address for order
        //    db.SaveChanges();
        //    finalAddressID = newAddress.AddressID;

        //}
        //else
        //{
        //    finalAddressID = SelectedAddressID.Value;
        //}


        // Create Order
        var order = new Order
        {
            UserID = userId,
            OrderDate = DateTime.Now,
            TotalAmount = cart.Sum(item => item.Price * item.Quantity),
            Status = "Processing",// Default status
            AddressID = SelectedAddressID
        };
        db.Orders.Add(order);
        db.SaveChanges(); // Save to generate OrderID

        // Add items to OrderDetails
        foreach (var item in cart)
        {
            var product = db.Products.Find(item.ProductID);
            if (product == null || product.MerchantID == null)
            {
                return RedirectToAction("Cart", new { error = "Product Merchant ID missing" });
            }
            var orderDetail = new OrderDetail
            {
                OrderID = order.OrderID, // Link to order
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                Price = item.Price,
                MerchantID = product.MerchantID
            };
            db.OrderDetails.Add(orderDetail);
        }
        db.SaveChanges();

       

        // Generate invoice
        string invoicePath = GenerateInvoice(user, order,cart);

        // Clear cart
        Session["Cart"] = null;

        return RedirectToAction("ThankYou", "Cart", new { invoicePath });
    }
    //[HttpPost]
    //public ActionResult ConfirmPayment()
    //{
    //    if (Session["Cart"] == null)
    //    {
    //        return RedirectToAction("Index", "Cart");
    //    }

    //    if (Session["UserID"] == null)
    //    {
    //        Debug.WriteLine("null user");
    //        return RedirectToAction("Login", "User");
    //    }

    //    int userId = (int)Session["UserID"];
    //    var user = db.Users.Find(userId);

    //    if (user == null)
    //    {
    //        Debug.WriteLine("User not found in database");
    //        return RedirectToAction("Login", "User");
    //    }

    //    // Generate invoice
    //    string invoicePath = GenerateInvoice(user);

    //    // Clear cart after purchase
    //    Session["Cart"] = null;

    //    return RedirectToAction("ThankYou", "Cart", new { invoicePath });
    //}

    public ActionResult ThankYou(string invoicePath)
    {
        ViewBag.InvoicePath = invoicePath;
        return View();
    }
    private string GenerateInvoice(User user, Order order, List<CartItem> cartItems)
    {
        string folderPath = Server.MapPath("~/Invoices/");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"Invoice_{user.UserID}_{DateTime.Now.Ticks}.pdf";
        string filePath = Path.Combine(folderPath, fileName);

        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            Document doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, fs);
            doc.Open();

            Font titleFont = FontFactory.GetFont("Arial", 20, Font.BOLD);
            Paragraph title = new Paragraph("Order Invoice", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            };
            doc.Add(title);

            doc.Add(new Paragraph($"Order ID: {order.OrderID}"));
            doc.Add(new Paragraph($"Customer: {user.FirstName}"));
            doc.Add(new Paragraph($"Email: {user.Email}"));
            doc.Add(new Paragraph($"Order Date: {order.OrderDate}"));

            doc.Add(new Paragraph("\nOrder Details:\n"));

            PdfPTable table = new PdfPTable(4);
            table.AddCell("Product");
            table.AddCell("Quantity");
            table.AddCell("Price");
            table.AddCell("Total");

            decimal grandTotal = 0;

            foreach (var item in cartItems)
            {
                table.AddCell(item.Name);
                table.AddCell(item.Quantity.ToString());
                table.AddCell($"₹{item.Price}");
                table.AddCell($"₹{item.Quantity * item.Price}");
                grandTotal += item.Quantity * (item.Price ?? 0);
            }

            doc.Add(table);
            doc.Add(new Paragraph($"\nGrand Total: ₹{grandTotal}", titleFont));
            doc.Close();
        }

        return fileName;
    }

   
    public ActionResult CartCount()
    {
        var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
        return Json(new { cartCount = cart.Sum(c => c.Quantity) });
    }

}
