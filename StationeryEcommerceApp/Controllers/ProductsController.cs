using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using StationeryEcommerceApp.Models;

namespace StationeryEcommerceApp.Controllers
{
    public class ProductController : Controller
    {
        private StationeryEcommerceDBEntities1 db = new StationeryEcommerceDBEntities1();

        // GET: Product (List all products)
        public ActionResult Index(int page = 1)
        {
            int pageSize = 3; // Show 3 products per page
            var products = db.Products.OrderBy(p => p.ProductID)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToList();

            int totalProducts = db.Products.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(products);
        }


        // GET: Product/Details/5
        public ActionResult Details(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // GET: Product/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        public ActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                if (Session["MerchantID"] == null)
                {
                    return RedirectToAction("Login", "Merchant"); // Ensure merchant is logged in
                }

                product.MerchantID = (int)Session["MerchantID"]; // Assign the current merchant's ID
                if (Request.Files.Count >= 1)
                {
                    var file = Request.Files[0];
                    var imgBytes = new Byte[file.ContentLength];
                    file.InputStream.Read(imgBytes, 0, file.ContentLength);
                    var base64String = Convert.ToBase64String(imgBytes, 0, imgBytes.Length);
                    product.ImageUrl = base64String;
                }
                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("ManageProducts", "Merchant");
            }
            return View(product);
        }

        // GET: Product/Edit/5
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
            return RedirectToAction("Index");
        }
        //return View(product);
        //}

        // GET: Product/Delete/5
        public ActionResult Delete(int id)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Products.Find(id);
            if (product != null)
            {
                db.Products.Remove(product);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        public ActionResult Categories()
        {
            var categories = db.Products
                               .Select(p => p.Category)
                               .Distinct()
                               .ToList();

            return View(categories);
        }

        // Show products under a selected category
        public ActionResult ProductsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return RedirectToAction("Categories");
            }

            var products = db.Products.Where(p => p.Category == category).ToList();

            if (!products.Any())
            {
                ViewBag.Message = "No products found in this category.";
            }

            ViewBag.Category = category;
            return View(products);
        }

    }
}
