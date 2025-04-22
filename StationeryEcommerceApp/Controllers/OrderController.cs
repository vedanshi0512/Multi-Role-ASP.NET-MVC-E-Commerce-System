using System.Collections.Generic;
using System;
using System.Linq;
using System.Web.Mvc;
using StationeryEcommerceApp.Models;
using System.Data.Entity;


namespace StationeryEcommerceApp.Controllers
{
    public class OrderController : Controller
    {
        private StationeryEcommerceDBEntities1 db = new StationeryEcommerceDBEntities1();

        // GET: Order
        public ActionResult Index()
        {
            return View(db.Orders.ToList());
        }

        // GET: Order/Details/5
        public ActionResult Details(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        // GET: Order/PlaceOrder
        public ActionResult PlaceOrder()
        {
            var cart = (List<Product>)Session["Cart"];
            if (Session["Cart"] == null || ((List<Product>)Session["Cart"]).Count == 0)
                return RedirectToAction("Index", "Cart");

            var order = new Order
            {
                UserID = (int)Session["UserID"],
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(p => p.Price)
            };

            db.Orders.Add(order);
            db.SaveChanges();

            Session["Cart"] = null;
            return RedirectToAction("Index");
        }
        public ActionResult UserOrders()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            int userId = (int)Session["UserID"];
            var orders = db.Orders
                           .Where(o => o.UserID == userId)
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();

            return View(orders);
        }

        public ActionResult MerchantOrders()
        {
            if (Session["MerchantID"] == null)
            {
                return RedirectToAction("Login", "Merchant");
            }

            int merchantId = (int)Session["MerchantID"];
            var merchantProducts = db.Products.Where(p => p.MerchantID == merchantId).Select(p => p.ProductID).ToList();

            var orders = db.Orders
       .Where(o => o.OrderDetails.Any(od => od.MerchantID == merchantId)) // Orders that contain this merchant's products
       .Include(o => o.OrderDetails) // Include OrderDetails for display
       .ToList();

            return View(orders);
        }
        [HttpPost]
        public ActionResult UpdateOrderStatus(int orderId, string status)
        {
            var order = db.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = status;
                db.SaveChanges();
            }

            return RedirectToAction("MerchantOrders");
        }
        public ActionResult MerchantCustomers()
        {
            if (Session["MerchantID"] == null)
            {
                return RedirectToAction("Login", "Merchant");
            }

            int merchantId = (int)Session["Merchant"];

            var merchantProducts = db.Products
                                     .Where(p => p.MerchantID == merchantId)
                                     .Select(p => p.ProductID)
                                     .ToList();

            var customers = db.Orders
                              .Where(o => o.OrderDetails
                               .Any(oi => oi.ProductID.HasValue &&
                                          merchantProducts.Contains(oi.ProductID.Value)))
                              .Select(o => o.User)
                              .Distinct()
                              .ToList();

            return View(customers);
        }


    }
}
