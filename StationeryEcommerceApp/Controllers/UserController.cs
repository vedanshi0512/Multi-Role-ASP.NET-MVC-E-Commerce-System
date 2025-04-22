using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StationeryEcommerceApp.Models; // Import the database model
using System.Web.Security;
using System.Security.Cryptography;
using System.Text;
using StationeryEcommerceApp.Services;

namespace StationeryEcommerceApp.Controllers
{
    public class UserController : Controller
    {
        private readonly string _cipherKey = "b14ca5898a4e4133bbce2ea2315a1916";
        private StationeryEcommerceDBEntities1 db = new StationeryEcommerceDBEntities1(); // Database Context

        // GET: User/Index (List of Users)
        public ActionResult Index()
        {
            var users = db.Users.ToList();
            return View(users);
        }

        // GET: User/Details/5
        public ActionResult Details(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();
            return View(user);
        }

        // GET: User/Create (Form to Add User)
        public ActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        public ActionResult Create(User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: User/Edit/5
        public ActionResult Edit(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        public ActionResult Edit(User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: User/Delete/5
        public ActionResult Delete(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();
            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult Login()
        {
            return View();
        }

        // POST: User/Login
        [HttpPost]
        public ActionResult Login(User user)
        {
            
            
            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.password))
            {
                ViewBag.Message = "Email and Password are required";
                return View();
            }

            // Fetch user by email
            var userInDb = db.Users.FirstOrDefault(u => u.Email == user.Email);


            if (userInDb != null && VerifyPassword(user.password, userInDb.password))
            {
                // Store User ID & Role in Session
                Session["UserID"] = userInDb.UserID;
                Session["UserEmail"] = userInDb.Email;
                Session["username"] = userInDb.username;
                // Session["UserRole"] = userInDb.Role; // Uncomment if using roles

                FormsAuthentication.SetAuthCookie(userInDb.Email, false);

                return RedirectToAction("Index", "Home");
            }

            

            
            ViewBag.Message = "Invalid Email or Password";
            return View();
        }
        
        // GET: User/Register (Show registration form)
        public ActionResult Register()
        {
            return View();
        }

        // POST: User/Register (Save new user)
        [HttpPost]
        public ActionResult Register(User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Message = "Invalid data. Please check your input.";
                    return View(user);
                }

                var existingUser = db.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ViewBag.Message = "Email is already registered.";
                    return View(user);
                }

                user.password = SetPassword(user.password);
                
                db.Users.Add(user);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful. Please login.";
                return RedirectToAction("Login", "User"); // Explicit controller name
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred: " + ex.Message;
                return View(user);
            }
        }



        // GET: User/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            //FormsAuthentication.SignOut();
            return RedirectToAction("Landing", "Home");
        }

        [HttpPost]
        public ActionResult PlaceOrder(int productId, int quantity)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userID = Convert.ToInt32(Session["UserID"]);
            var product = db.Products.Find(productId);

            if (product == null || quantity <= 0)
                return RedirectToAction("Products");

            var order = new Order
            {
                UserID = userID,
                OrderDate = DateTime.Now,
                Status = "Placed"
            };

            db.Orders.Add(order);
            db.SaveChanges();

            var orderDetail = new OrderDetail
            {
                OrderID = order.OrderID,
                ProductID = productId,
                Quantity = quantity,
                Price = product.Price
            };

            db.OrderDetails.Add(orderDetail);
            db.SaveChanges();

            return RedirectToAction("MyOrders");
        }

        public ActionResult MyOrders()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userID = Convert.ToInt32(Session["UserID"]);
            var orders = db.Orders.Where(o => o.UserID == userID).ToList();

            return View(orders);
        }
        public ActionResult UserProfile()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = (int)Session["UserID"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        public ActionResult OrderHistory()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = (int)Session["UserID"];
            var orders = db.Orders
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }
        



        private bool VerifyPassword(string inputPassword, string encryptedPassword)
        {
            string decryptedPassword = GetPassword(encryptedPassword);
            return decryptedPassword.Equals(inputPassword);
        }

        private string GetPassword(string encryptedPassword)
        {
            CryptographyService cryptographyService = new CryptographyService();
            return cryptographyService.DecryptString(_cipherKey, encryptedPassword);
        }

        private string SetPassword(string PlainTextPassword)
        {
            CryptographyService cryptographyService = new CryptographyService();
            return cryptographyService.EncryptString(_cipherKey, PlainTextPassword);
        }
    }
}
