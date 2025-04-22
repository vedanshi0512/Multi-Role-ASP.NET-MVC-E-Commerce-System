using System.Linq;
using System.Web.Mvc;
using StationeryEcommerceApp.Models;

namespace StationeryEcommerceApp.Controllers
{
    public class PaymentController : Controller
    {
        private StationeryEcommerceDBEntities1 db = new StationeryEcommerceDBEntities1();

        // GET: Payment
        public ActionResult Index()
        {
            return View(db.Payments.ToList());
        }

        // GET: Payment/Process
        public ActionResult ProcessPayment()
        {
            return View();
        }

        // POST: Payment/Process
        [HttpPost]
        public ActionResult ProcessPayment(Payment payment)
        {
            if (ModelState.IsValid)
            {
                db.Payments.Add(payment);
                db.SaveChanges();
                return RedirectToAction("Index", "Order");
            }
            return View(payment);
        }
    }
}
