using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Core.Objects;
using AvonessaMVC5.Models;
using CaptchaMvc.Attributes;
using CaptchaMvc.HtmlHelpers;
using CaptchaMvc.Interface;


namespace AvonessaMVC5.Controllers
{
    public class HomeController : Controller
    {
        private bool bIsRussian = true;
        AvonessaDBEntities db = null;
        private int iSB_Id = -1; //корзина пуста
        private ObjectParameter outputSB_ID = null;
        private bool bpExists = false;

        public ActionResult Index(int id = 0)
        {
            if (Request.ServerVariables["HTTP_ACCEPT_LANGUAGE"].ToString().Substring(0, 2) != "ru")
                bIsRussian = false;
            db  = new AvonessaDBEntities();
            int c_id = 0; 
            if (id == 0)
                c_id = 1;
            else
                c_id = 13;
            if (bIsRussian)
            {
                var vCatNameRu = (from cn in db.Categories where cn.C_Id == c_id
                            select new {n = cn.CategoryName}).SingleOrDefault();    
                ViewBag.CategoryNameRu = vCatNameRu.n;
            }
            else
            {
                var vCatNameEn = (from cn in db.Categories where cn.C_Id == c_id
                            select new {n = cn.CategoryNameEnglish}).SingleOrDefault();
                ViewBag.CategoryNameEn = vCatNameEn.n;
            }
             

            var ci = from CI in db.sp_GetCategories(c_id)
                     select CI;
            
            return View(ci.ToList());
            
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Products(int id = 0)
        {
            db  = new AvonessaDBEntities();
            var ps = from p in db.sp_GetProducts_1(id, -1)
                     select p;

            var cn = (from c in db.Categories where c.C_Id == id
                          select new { n = c.CategoryName}).SingleOrDefault();

            var cnEn = (from c in db.Categories where c.C_Id == id
                          select new { n = c.CategoryNameEnglish}).SingleOrDefault();

            ViewBag.CategoryName = cn.n;
            ViewBag.CategoryNameEnglish = cnEn.n;
            
            return View(ps.ToList());
        }

        public ActionResult _Product(int id = 0, int p_id = 0)
        {
            GetProductData(p_id);

            return View();
        }
        [HttpPost]
        public ActionResult _Product(string id = "", string p_id = "")
        {
            if (Request.ServerVariables["HTTP_ACCEPT_LANGUAGE"].ToString().Substring(0, 2) != "ru")
                bIsRussian = false;
            string sQuantity = "";
            
            if (Request.Form["tbQuantity"] != null)
            {
                sQuantity = Request.Form["tbQuantity"].ToString();
                
                if (Request.Cookies["AvonessaShoppingBag"] != null) // сумка пуста
                { 
                    iSB_Id = int.Parse(Request.Cookies["AvonessaShoppingBag"]["sb_Id"].ToString());
                    MakeShoppingBagInDB(p_id, sQuantity);
                    iSB_Id = int.Parse(Request.Cookies["AvonessaShoppingBag"]["sb_Id"].ToString());

                    /*if (bpExists)
                        if (bIsRussian)
                            ViewBag.AlreadyInBagRuEn = "Этот товар уже есть в сумке.";
                        else
                            ViewBag.AlreadyInBagRuEn = "This product in the bag already.";
                     
                    else
                     */
                        // потом исправить:
                    return RedirectToAction("ShoppingBag", "ShopBagOrder", new { sb_Id =  iSB_Id});
                }
                else
                {
                    MakeShoppingBagInDB(p_id, sQuantity);
                    //делаем куки сумки:
                    HttpCookie ShoppingBagCookie = new HttpCookie("AvonessaShoppingBag");

                    ShoppingBagCookie["sb_Id"] = iSB_Id.ToString(); //присвоен в MakeShoppingBagInDB(ipQuantity)
                    ShoppingBagCookie.Expires = DateTime.Now.AddDays(10d);//десять дней
                    Response.Cookies.Add(ShoppingBagCookie);
                    
                    return RedirectToAction("ShoppingBag", "ShopBagOrder", new { sb_Id =  iSB_Id});
                }
                
            }

            GetProductData(int.Parse(p_id));

            return View();
        }

        private void GetProductData(int p_id)
        {
            db = new AvonessaDBEntities();
            var pt = (from p in db.sp_GetProducts_1(0, p_id)
                     select p).SingleOrDefault();

            ViewBag.ProductName = pt.ProductName;
            ViewBag.ProductNameEnglish = pt.ProductNameEnglish;
            ViewBag.ProductCost = pt.ProductCost;
            ViewBag.P_Id = pt.P_Id;
            ViewBag.ImageFilePath = pt.ImageFilePath;
            ViewBag.ImageFilePath2 = pt.ImageFilePath2;
            ViewBag.Notes = pt.Notes;
            ViewBag.NotesEnglish = pt.NotesEnglish;
            ViewBag.CategoryName = pt.CategoryName;
            ViewBag.CategoryNameEnglish = pt.CategoryNameEnglish;
        }

        private void MakeShoppingBagInDB(string p_id, string quantity)
        {
            db = new AvonessaDBEntities();
            short iQuntity = short.Parse(quantity);
            outputSB_ID = new ObjectParameter("SB_Id_out", typeof(int));                
             
            var i = db.sp_MakeShoppingBag(int.Parse(p_id), iSB_Id, iQuntity, outputSB_ID);
            db.SaveChanges();
            iSB_Id = int.Parse(outputSB_ID.Value.ToString());
            if (iSB_Id == -1)
                bpExists = true; //уже есть в сумке
        }

        //public ActionResult ShoppingBag(int id = 0, int p_id = 0)
        //{
        //    db = new AvonessaDBEntities();
        //    //db.sp_MakeShoppingBag(p_id, -1, )

        //    ViewBag.Message = "Your application description page.";
        //    db.Dispose();
        //    return View();
        //}

        public ActionResult Mailer()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Mailer(EmailModel model, string empty)
        {
            string sCaptchaVal = "";
            if (Request.ServerVariables["HTTP_ACCEPT_LANGUAGE"].ToString().Substring(0, 2) != "ru")
                sCaptchaVal = "Captcha is not valid";
            else
                sCaptchaVal = "Неправильно ввели каптчу.";

            if (this.IsCaptchaValid(sCaptchaVal))
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        new EmailController().SendEmail(model).Deliver();

                        return RedirectToAction("Success");
                    }
                    catch (Exception ex)
                    {
                        ViewBag.ErrorMsg = ex.Message;
                        //return RedirectToAction("Error");
                    }
                }
                return View(model);
            }

            TempData["ErrorMessage"] = "Error: captcha is not valid.";
            return View();
            
        }

        public ActionResult Success()
        {
            return View();
        }

        public ActionResult Photoshoots()
        {
            db  = new AvonessaDBEntities();
            var ps = from s in db.sp_GetPhotoshoots()
                      select s;
            return View(ps.ToList());
        }

        public ActionResult ShippingAndOrder(string WhichPage)
        {
            var vm = new ShippingAndOrderModel();
            vm.WhichPage = WhichPage;
            //if (WhichPage == "Shipping")
            //    ViewBag.WhichPage = "Shipping";
            //else
            //    ViewBag.WhichPage = "Order";

            return View(vm);
        }

        public ActionResult History()
        {

            return View();
        }

        public ActionResult Contacts()
        {

            return View();
        }

        
    }
}