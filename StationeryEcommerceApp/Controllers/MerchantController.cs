using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using StationeryEcommerceApp.Models;
using StationeryEcommerceApp.Services;
using System.Diagnostics;
using System.IO;

namespace StationeryEcommerceApp.Controllers
{
    public class MerchantController : Controller
    {
        private readonly string _cipherKey = "b14ca5898a4e4133bbce2ea2315a1916";
        // Merchant login page
        public ActionResult Login()
        {

            return View();
        }

        // After successful login, redirect to dashboard
        public ActionResult Dashboard()
        {
            if (Session["MerchantID"] == null)
            {
                return RedirectToAction("Login");
            }

            //int merchantID = Convert.ToInt32(Session["MerchantID"]);
            //var products = db.Products.Where(p => p.MerchantID == merchantID).ToList();

            //return View(products);
            return View();
        }
        public ActionResult ManageProducts()
        {
            if (Session["MerchantID"] == null) return RedirectToAction("Login");

            int merchantID = Convert.ToInt32(Session["MerchantID"]);
            var products = db.Products.Where(p => p.MerchantID == merchantID).ToList();

            return View(products);
        }


        [HttpPost]
        public ActionResult Login(Merchant merchant)
        {
            Debug.WriteLine(merchant.Email);

            if (string.IsNullOrEmpty(merchant.Email) || string.IsNullOrEmpty(merchant.Password))
            {

                ViewBag.Message = "Email and Password are required";
                return View();
            }

            // Fetch user by email
       
            var MerchantInDb = db.Merchants.FirstOrDefault(m => m.Email == merchant.Email);

            
            if (MerchantInDb != null && VerifyPassword(merchant.Password, MerchantInDb.Password))
            {
                // Store User ID & Role in Session
                
                Session["MerchantID"] = MerchantInDb.MerchantID;
                Session["MerchantEmail"] = MerchantInDb.Email;
                Session["Name"] = MerchantInDb.Name;
                
                FormsAuthentication.SetAuthCookie(MerchantInDb.Email, false);

                return RedirectToAction("Dashboard", "Merchant");
            }


            ViewBag.Message = "Invalid Email or Password";
            return View("Login");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Landing","Home");
        }
        private StationeryEcommerceDBEntities1 db = new StationeryEcommerceDBEntities1();

        // GET: Merchant Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Merchant Register
        [HttpPost]
        public ActionResult Register(Merchant merchant)
        {
            if (db.Merchants.Any(m => m.Email == merchant.Email))
            {
                ViewBag.Error = "Email already exists!";
                return View();
            }

            // Encrypt Password

            Debug.WriteLine(merchant.Email);

            merchant.Password = SetPassword(merchant.Password);
            db.Merchants.Add(merchant);
            db.SaveChanges();

            return RedirectToAction("Login");
        }
        // GET: Product/Create
        public ActionResult Create()
        {
            if (Session["MerchantID"] == null)
                return RedirectToAction("Login");

            return View();
        }

        // POST: Product/Create
        [HttpPost]
        public ActionResult Create(Product product, HttpPostedFileBase imageFile)
        {
            if (Session["MerchantID"] == null)
                return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                int merchantID = Convert.ToInt32(Session["MerchantID"]);
                product.MerchantID = merchantID;
                product.CreatedAt = DateTime.Now;

                // Handle Image Upload as Base64
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        imageFile.InputStream.CopyTo(ms);
                        var imgBytes = ms.ToArray();
                        product.ImageUrl = Convert.ToBase64String(imgBytes); // Store Base64 string in DB
                    }
                }

                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("ManageProducts");
            }

            return View(product);
        }
        public ActionResult Edit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        public ActionResult Edit(Product product, HttpPostedFileBase imageFile)
        {
            var existingProduct = db.Products.Find(product.ProductID);
            if (existingProduct == null) return HttpNotFound();

            // Handle Image Update
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string fileName = Path.GetFileName(imageFile.FileName);
                string path = Path.Combine(Server.MapPath("~/ProductImages"), fileName);
                imageFile.SaveAs(path);
                existingProduct.ImageUrl = "/ProductImages/" + fileName; // Store relative path
            }

            existingProduct.Name = product.Name;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;

            db.SaveChanges();
            return RedirectToAction("ManageProducts","Merchant");
        }
        //public ActionResult AddProduct()
        //{
        //    if (Session["MerchantID"] == null) return RedirectToAction("Login");
        //    return View();
        //}

        //// POST: Add Product
        //[HttpPost]
        //public ActionResult AddProduct(Product product)
        //{
        //    if (Session["MerchantID"] == null) return RedirectToAction("Login");

        //    product.MerchantID = Convert.ToInt32(Session["MerchantID"]);
        //    db.Products.Add(product);
        //    db.SaveChanges();

        //    return RedirectToAction("Dashboard");
        //}

        // DELETE: Remove Product
        public ActionResult DeleteProduct(int id)
        {
            if (Session["MerchantID"] == null) return RedirectToAction("Login");

            var product = db.Products.Find(id);
            if (product != null)
            {
                db.Products.Remove(product);
                db.SaveChanges();
            }

            return RedirectToAction("Dashboard");
        }
       
        public ActionResult Customers()
        {
            if (Session["MerchantID"] == null) return RedirectToAction("Login");

            int merchantID = Convert.ToInt32(Session["MerchantID"]);
            var customers = db.Users
                  .Where(u => db.OrderDetails
                          .Any(od => od.MerchantID == merchantID && od.Order.UserID == u.UserID))
                  .ToList();
            


            return View(customers);
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