using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Core.Objects;
using System.Text;
using PayPal.Api.Payments;
using PayPal;
using MSXML2;
using System.Net.Mail;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using System.Data.Common;
using System.Data;

namespace AvonessaMVC5.Controllers
{
    public class ShopBagOrderController : Controller
    {
        private AvonessaDBEntities db = null;
        public string Payer_email = "";
        public string Address_country = "";
        public string Txn_id = "";
        public string First_name = "";
        public string Last_name = "";
        public string Address_country_code = "";
        public string Address_zip = "";
        public string Address_state = "";
        public string Address_city = "";
        public string Address_street = "";
        public string Payment_date = "";
        //public string Recurring_payment_id = "";
        private ObjectParameter outputProductExistsInBag = null;

        public ActionResult ShoppingBag(int sb_id = 0, int p_id = 0)
        {
            db = new AvonessaDBEntities();
            if (p_id != 0 & sb_id != 0) //удаление товара из сумки:
            {
                outputProductExistsInBag = new ObjectParameter("pExistsInBag", typeof(bool));
                var i = db.sp_DeleteProductFromSB(sb_id, p_id, outputProductExistsInBag);
                db.SaveChanges();
                bool exists = true;
                exists = bool.Parse(outputProductExistsInBag.Value.ToString());
                if (!exists) //сумка пуста , exists = false
                {
                    //удаляем куки:=======================================
                    HttpCookie ShoppingBagCookie;
                    ShoppingBagCookie = new HttpCookie("AvonessaShoppingBag");
                    ShoppingBagCookie.Expires = DateTime.Now.AddDays(-1d);
                    Response.Cookies.Add(ShoppingBagCookie);
                    //====================================================
                    return RedirectToAction("Index", "Home");
                }
            }
            
            var sb = from SB in db.sp_GetShoppingBagById(sb_id)
                     select SB;
            
            //SendMail("info@avonessa.com", "temir51@hotmail.com", "Оплачен товар на Avonessa.com", "ToBuyer", "164", "RU");
            //SendMail("info@avonessa.com", "temir51@hotmail.com", "Payment to Avonessa.com is completed", "ToBuyer", "165", "EN");
            ViewBag.sbUrl = "";
            return View(sb.ToList());
        }

        public string GetShoppingBagQuantity()
        {
            int iSB_Id = int.Parse(Request.Cookies["AvonessaShoppingBag"]["sb_id"].ToString());
            db = new AvonessaDBEntities();
            string sbq = "";

            var SBQ = (from sb in db.sp_GetSBQuantity(iSB_Id)
                          select sb).SingleOrDefault();
            sbq = SBQ.Value.ToString();
            return sbq;
        }
        //[HttpPost]
        //public ActionResult InsertContacts()
        //{
        //    string sPostCode = Request.Form["postalCodeRu"].ToString();
        //    string sContactName = Request.Form["yourNameRu"].ToString();
        //    string sEmail = Request.Form["emailRu"].ToString();
        //    //db = new AvonessaDBEntities();
        //    //var i = db.sp_InsertContactData(contact.PostalCode, contact.ContactName, contact.Email);
        //    Dictionary<string, object> postData = new Dictionary<string, object>();
        //    //перевод из PHP в C# для Payeer.com:+++++++++++++++++++++++++++
        //    /*
        //    string sm_shop = "61102332";
        //    string sm_orderid = "12345";
        //    //=============================
        //    var m_amount = 1.05;
        //    string sm_amount = m_amount.ToString("f2");
        //    //=============================
        //    string sm_curr = "USD";
        //    string senc_description = Base64Encode("Test Description");
        //    string sm_key = "545747sql";
            
        //    String[] arHash = new String[] {
        //        sm_shop, sm_orderid, sm_amount, sm_curr, senc_description, sm_key
        //    };
        //    string sm_sign = sha256(String.Join(":", arHash));

        //    postData.Add("m_shop", sm_shop);
        //    postData.Add("m_orderid", sm_orderid);
        //    postData.Add("m_amount", sm_amount);
        //    postData.Add("m_curr", sm_curr);
        //    postData.Add("m_desc", senc_description);
        //    postData.Add("m_sign", sm_sign.ToUpper());
        //    postData.Add("m_process", "send");
            
        //    return this.RedirectAndPost("https://payeer.com/merchant/", postData);
        //     */
        //    //++++++++++++++++++++++++++++++++++++++++++++++++++
        //    //return new RedirectAndPostActionResult("ShoppingBag", postData);
        //    //или
        //    //return this.RedirectAndPost("http://TheUrlToPostDataTo", postData);
        //    return this.RedirectAndPost("http://TheUrlToPostDataTo", postData);
        //}
        //Для PayPal отправляется Form "SBCFormRu". Не забыть изменить sandbox на live 
        public void MakePayPalTransactionRu()
        {
            string sTotal = "";
            decimal dTotal = 0;
            short sQuantity = 0;
            short i = 0;
            //выборка сумки из базы =====
            db = new AvonessaDBEntities();
            int iSB_Id = int.Parse(Request.Cookies["AvonessaShoppingBag"]["sb_Id"].ToString());
            var query = from sb in db.sp_GetShoppingBagById(iSB_Id) select sb;
            
            List<Item> itms = new List<Item>();
            Item item = null;
            foreach (var sb_data in query.ToList())
            {
                sQuantity = short.Parse(Request.Form["qty_item_ru_" + i.ToString()].ToString());
                var iq = db.sp_InsertQuantityToSB(int.Parse(sb_data.P_Id.ToString()), iSB_Id, sQuantity);
                dTotal += sQuantity * decimal.Parse(sb_data.ProductCost.ToString());
                item = new Item();
                item.name = sb_data.ProductName; //Русское название
                item.currency = "USD";
                item.price = sb_data.ProductCost.ToString().Replace(",", ".");
                item.quantity = Request.Form["qty_item_ru_" + i.ToString()].ToString();
                item.sku = sb_data.P_Id.ToString();
                itms.Add(item);
                i++;
            }
            
            ItemList itemList = new ItemList();
            itemList.items = itms;
            //заносим адрес поставки в базу:=====
            var ic = db.sp_InsertContactData(Request.Form["yourNameRu"].ToString(), Request.Form["emailRu"].ToString().Trim(),
                Request.Form["countryRu"].ToString(),Request.Form["postalCodeRu"].ToString(),Request.Form["cityRu"].ToString(),
                Request.Form["streetHomeFlatRu"].ToString());
            //Country Code=======================
            LookupService ls = new LookupService(@"E:/web/avoness1/App_Data/GeoIP.dat", LookupService.GEOIP_MEMORY_CACHE);
            Country c = ls.getCountry(Request.ServerVariables["REMOTE_ADDR"]);  
            string cc = c.getCode();
            //===================================
            ShippingAddress sa = new ShippingAddress();
            sa.country_code = cc;
            sa.city = Request.Form["cityRu"].ToString();
            sa.line1 = Request.Form["streetHomeFlatRu"].ToString();
            sa.recipient_name = Request.Form["yourNameRu"].ToString();
            sa.postal_code = Request.Form["postalCodeRu"].ToString();
            
            itemList.shipping_address = sa;
            
            //var sdkConfig = new Dictionary<string, string> { { "mode", "sandbox" } };// when you're live, change "sandbox" to "live"
            //string accessToken = new OAuthTokenCredential("ATrx7laOExss5QgsAMYpryJJZfo_Vw3gz0_HzOXMNFJjl4bb5rMrmUpj6nRyzm7uJgi-mebtQ7LhUF8y", 
            //    "ECDJMgNsx4rwhkHsATlsnp1DUoy6xZXOdLfLpOPqE6ErAHKkRuDQOctcDgEBgSBz68jmzn5OGtwuMTYN", sdkConfig)
            //                            .GetAccessToken();

            //мои данные k.temirgaliyev@hotmail.com
            /*
            var sdkConfig = new Dictionary<string, string> { { "mode", "live" } };// when you're live, change "sandbox" to "live"
            string accessToken = new OAuthTokenCredential("AYdL5I1bbxg91dez6GLuMSkQHVqtzocJ7LlgNULyEks_ElPjniV5OTP2Ka4IQotS6NyRjAQA87E6hztU", 
                "EKUkXPL0ZxM2tdmI-r9iMRiQYCVfQxIP7qh6kS21WI5qJ3gPuMOIt3kYGAH0VMi9Eaid07p1KsHecvtD", sdkConfig)
                                        .GetAccessToken();
            */
            var sdkConfig = new Dictionary<string, string> { { "mode", "live" } };// when you're live, change "sandbox" to "live"
            string accessToken = new OAuthTokenCredential("AbZ5lBaf9HfwR9ixms8A4qunW_4m06hliKjqB8NgLOup_4kP1AemIlzu04FOZoKjg2cu0OJYUt4P-YUt",
                "EOniNB_yLiy4XCNsTzZoadF9dG7MK5aUYwJo4cJk6v0WY54ZIKkZIiBtJWqA1Jam_1rDuR57sYqKCXts", sdkConfig)
                                        .GetAccessToken();

            var redirectUrls = new RedirectUrls {
                cancel_url = "http://www.avonessa.com/ShopBagOrder/ShoppingBag/?sb_id=" + iSB_Id +"&cancel=true",
                return_url = "http://www.avonessa.com/ShopBagOrder/Success/?sb_id=" + iSB_Id + "&success=true"
            };


            // Specify details of your payment amount.
            Details _details = new Details();
            decimal _dShip = 8.75m; // цена поставки
            sTotal = dTotal.ToString().Replace(",", ".");
            _details.shipping = _dShip.ToString().Replace(",", ".");
            _details.subtotal = sTotal;
            _details.tax = "0";

            
            dTotal = dTotal + _dShip;
            sTotal = dTotal.ToString().Replace(",", ".");
            var amnt = new Amount { currency = "USD", total = sTotal, details = _details};
           
            var createdPayment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                transactions = new List<Transaction> {new Transaction { description = "Оплата за товар", 
                                                                        amount = amnt, 
                                                                        item_list = itemList}},
                redirect_urls = redirectUrls}.Create(new APIContext(accessToken) { Config = sdkConfig });
             
            var approvalUrl = createdPayment.links.Single(l => l.rel == "approval_url").href;
            
            //Переход на страницу PayPal для продолжения оплаты:
            Response.Redirect(approvalUrl);
        }
        //Для PayPal отправляется Form "SBCFormEn". Не забыть изменить sandbox на live 
        public void MakePayPalTransactionEn()
        {
            string sTotal = "";
            decimal dTotal = 0;
            short sQuantity = 0;
            short i = 0;
            //выборка сумки из базы 
            db = new AvonessaDBEntities();
            int iSB_Id = int.Parse(Request.Cookies["AvonessaShoppingBag"]["sb_Id"].ToString());
            var query = from sb in db.sp_GetShoppingBagById(iSB_Id) select sb;

            List<Item> itms = new List<Item>();
            Item item = null;
            foreach (var sb_data in query.ToList())
            {
                sQuantity = short.Parse(Request.Form["qty_item_en_" + i.ToString()].ToString());
                var iq = db.sp_InsertQuantityToSB(int.Parse(sb_data.P_Id.ToString()), iSB_Id, sQuantity);
                dTotal += sQuantity * decimal.Parse(sb_data.ProductCost.ToString());
                item = new Item();
                item.name = sb_data.ProductNameEnglish; //название на английском
                item.currency = "USD";
                item.price = sb_data.ProductCost.ToString().Replace(",", ".");
                item.quantity = Request.Form["qty_item_en_" + i.ToString()].ToString();
                item.sku = sb_data.P_Id.ToString();
                itms.Add(item);
                i++;
            }
            
            ItemList itemList = new ItemList();
            itemList.items = itms;

            ////Country Code=======================
            //LookupService ls = new LookupService(@"E:/web/avoness1/App_Data/GeoIP.dat", LookupService.GEOIP_MEMORY_CACHE);
            //Country c = ls.getCountry(Request.ServerVariables["REMOTE_ADDR"]);  
            //string cc = c.getCode();
            ////===================================
            //ShippingAddress sa = new ShippingAddress();
            //sa.country_code = cc;
                       
            //itemList.shipping_address = sa;
            //=====================================
            //var sdkConfig = new Dictionary<string, string> { { "mode", "sandbox" } };// when you're live, change "sandbox" to "live"
            //string accessToken = new OAuthTokenCredential("ATrx7laOExss5QgsAMYpryJJZfo_Vw3gz0_HzOXMNFJjl4bb5rMrmUpj6nRyzm7uJgi-mebtQ7LhUF8y", 
            //    "ECDJMgNsx4rwhkHsATlsnp1DUoy6xZXOdLfLpOPqE6ErAHKkRuDQOctcDgEBgSBz68jmzn5OGtwuMTYN", sdkConfig)
            //                            .GetAccessToken();

            var sdkConfig = new Dictionary<string, string> { { "mode", "live" } };// when you're live, change "sandbox" to "live"
            string accessToken = new OAuthTokenCredential("AbZ5lBaf9HfwR9ixms8A4qunW_4m06hliKjqB8NgLOup_4kP1AemIlzu04FOZoKjg2cu0OJYUt4P-YUt",
                "EOniNB_yLiy4XCNsTzZoadF9dG7MK5aUYwJo4cJk6v0WY54ZIKkZIiBtJWqA1Jam_1rDuR57sYqKCXts", sdkConfig)
                                        .GetAccessToken();

            var redirectUrls = new RedirectUrls {
                cancel_url = "http://www.avonessa.com/ShopBagOrder/ShoppingBag/?sb_id=" + iSB_Id +"&cancel=true",
                return_url = "http://www.avonessa.com/ShopBagOrder/Success/?sb_id=" + iSB_Id + "&success=true"
            };

            // Specify details of your payment amount.
            Details _details = new Details();
            decimal _dShip = 8.75m; // цена поставки
            sTotal = dTotal.ToString().Replace(",", ".");
            _details.shipping = _dShip.ToString().Replace(",", ".");
            _details.subtotal = sTotal;
            _details.tax = "0";

            
            dTotal = dTotal + _dShip;
            sTotal = dTotal.ToString().Replace(",", ".");
            var amnt = new Amount { currency = "USD", total = sTotal, details = _details};
           
            var createdPayment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                transactions = new List<Transaction> {new Transaction { description = "Payment for the product", 
                                                                        amount = amnt, 
                                                                        item_list = itemList}},
                redirect_urls = redirectUrls}.Create(new APIContext(accessToken) { Config = sdkConfig });
            
            var approvalUrl = createdPayment.links.Single(l => l.rel == "approval_url").href;
            
            //Переход на страницу PayPal для продолжения оплаты:
            Response.Redirect(approvalUrl);
        }

        //Для Payeer.Com
        public static string Base64Encode(string plainText) {
          var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
          return System.Convert.ToBase64String(plainTextBytes);
        }
        //Для Payeer.Com
        public static string sha256(string inputString)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(inputString), 0, Encoding.UTF8.GetByteCount(inputString));
            foreach (byte bit in crypto)
            {
                hash.Append(bit.ToString("x2"));
            }
            return hash.ToString();
        }
        //Для Payeer.Com
        public ActionResult Status()
        {
            string ipAddress = Request.ServerVariables["REMOTE_ADDR"].ToString();

            if (ipAddress != "37.59.221.230")
                return RedirectToAction("Fail", "ShopBagOrder", new { id = "Bad_Client"});
            if (Request.Form["m_operation_id"] != null && Request.Form["m_sign"] != null)
            {
                string m_key = "545747sql";
                String[] arHash = new String[] {
                    Request.Form["m_operation_id"],
                    Request.Form["m_operation_ps"],
                    Request.Form["m_operation_date"],
                    Request.Form["m_operation_pay_date"],
                    Request.Form["m_shop"],
                    Request.Form["m_orderid"],
                    Request.Form["m_amount"],
                    Request.Form["m_curr"],
                    Request.Form["m_desc"],
                    Request.Form["m_status"],
                    m_key
                };
                string sign_hash = sha256(String.Join(":", arHash)).ToUpper();
                if (Request.Form["m_sign"].ToString() == sign_hash && Request.Form["m_status"].ToString() == "success")
                {
                    //echo $_POST['m_orderid'].'|success';
                    //тут сделать внесение данных о платеже в базу - update Orders
                    return RedirectToAction("Success", "ShopBagOrder", new { id = Request.Form["m_orderid"] + "|success"});
                }
                else
                {
                    return RedirectToAction("Fail", "ShopBagOrder", new { id = Request.Form["m_orderid"] + "|error"});
                }
            }
            return View();
        }
        //Для PayPal
        public ActionResult Success(string sb_id)
        {
            string payerID = Request.QueryString["PayerID"].ToString();
            string paymentId = Request.QueryString["paymentId"].ToString();

            //var sdkConfig = new Dictionary<string, string> { { "mode", "sandbox" } };// when you're live, change "sandbox" to "live"
            //string accessToken = new OAuthTokenCredential("ATrx7laOExss5QgsAMYpryJJZfo_Vw3gz0_HzOXMNFJjl4bb5rMrmUpj6nRyzm7uJgi-mebtQ7LhUF8y",
            //    "ECDJMgNsx4rwhkHsATlsnp1DUoy6xZXOdLfLpOPqE6ErAHKkRuDQOctcDgEBgSBz68jmzn5OGtwuMTYN", sdkConfig)
            //                            .GetAccessToken();

            var sdkConfig = new Dictionary<string, string> { { "mode", "live" } };// when you're live, change "sandbox" to "live"
            string accessToken = new OAuthTokenCredential("AbZ5lBaf9HfwR9ixms8A4qunW_4m06hliKjqB8NgLOup_4kP1AemIlzu04FOZoKjg2cu0OJYUt4P-YUt",
                "EOniNB_yLiy4XCNsTzZoadF9dG7MK5aUYwJo4cJk6v0WY54ZIKkZIiBtJWqA1Jam_1rDuR57sYqKCXts", sdkConfig)
                                        .GetAccessToken();

            var pymntExecution = new PaymentExecution { payer_id = payerID };
            
            var payment = new Payment { id = paymentId }
                              .Execute(new APIContext(accessToken) { Config = sdkConfig }, pymntExecution);

            //добавляем в таблицу БД Orders sPayerId========
            int iSB_Id = int.Parse(sb_id);// int.Parse(Request.Cookies["AvonessaShoppingBag"]["sb_Id"].ToString());
            db = new AvonessaDBEntities();
            var i = db.sp_InsertOrderDeleteSB(payerID, iSB_Id, paymentId);
            //удаляем куки:=======================================
            HttpCookie ShoppingBagCookie;
            ShoppingBagCookie = new HttpCookie("AvonessaShoppingBag");
            ShoppingBagCookie.Expires = DateTime.Now.AddDays(-1d);
            Response.Cookies.Add(ShoppingBagCookie);

            ViewBag.OrderId = payerID;
            return View();
        }
        //Для Payeer.Com
        public ActionResult Fail(string id)
        {
            ViewBag.OrderId = id;
            return View();
        }
        //Для PayPal IPN не забыть изменить адрес без sandbox!!!
        public void MakePayPalOrderVerification()
        {
            string Receiver_email = "";
            string Payment_status = "";
            string Payer_id = ""; 
            string Payment_amount;
            string str = ""; 
            str = Request.Form + "&cmd=_notify-validate";
            ServerXMLHTTP srv = new ServerXMLHTTP();

            try
            {
                //srv.open("POST", @"https://www.sandbox.paypal.com/cgi-bin/webscr", false, null, null);
                srv.open("POST", @"https://www.paypal.com/cgi-bin/webscr", false, null, null);
                srv.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
                srv.send(str);

                Receiver_email = Request.Form["receiver_email"].ToString();
                Payer_id = Request.Form["payer_id"].ToString();
                Payment_status = Request.Form["payment_status"].ToString();
                Payment_date = Request.Form["payment_date"].ToString();
                Payer_email = Request.Form["payer_email"].ToString();
                Payment_amount = Request.Form["mc_gross"].ToString();
                First_name = Request.Form["first_name"].ToString();
                Last_name = Request.Form["last_name"].ToString();
                Address_country = Request.Form["address_country"].ToString();
                Address_country_code = Request.Form["address_country_code"].ToString();
                Address_zip = Request.Form["address_zip"].ToString();
                Address_state = Request.Form["address_state"].ToString();
                Address_city = Request.Form["address_city"].ToString();
                Address_street = Request.Form["address_street"].ToString();
                Txn_id = Request.Form["txn_id"].ToString();

                if (srv.status != 200) //HTTP error handling
                    //отправка email мне
                    SendMail("info@avonessa.com", "k.temirgaliyev@hotmail.com", 
                        "Ошибка 200 - платёж не прошёл", "Error_200");
                else if (srv.responseText == "VERIFIED")
                {
                    if (Payment_status == "Completed") //check that Payment_status=Completed
                    {
                        //insert into DB значений
                        db = new AvonessaDBEntities();
                        var i = (db.sp_InsertDataFromPayPal_0(First_name + " " + Last_name, Payer_email, Address_country,
                            Address_zip, Address_country_code, Address_state, Address_city, Address_street, Payer_id, Payment_date,
                            Txn_id)).SingleOrDefault();
                        //отправляем оповещения
                        SendMail("info@avonessa.com", "k.temirgaliyev@hotmail.com", "Оплачено покупателем avonessa.com", "ToUs",
                            i.Value.ToString());
                        SendMail("info@avonessa.com", Payer_email, "", "ToBuyer",
                            i.Value.ToString(), Address_country_code);
                    }
                    if (Receiver_email != "avonessa@gmail.com") //check that Receiver_email is your Primary PayPal emailk.temirgaliyev@hotmail.com
                        SendMail("info@avonessa.com", "k.temirgaliyev@hotmail.com", "IPN is hacked", "IPN_IsHacked");
                }
                else if (srv.responseText == "INVALID")
                {
                    SendMail("info@avonessa.com", "k.temirgaliyev@hotmail.com", "IPN is hacked, maybe", "responseText_INVALID");
                }
                else
                {
                    SendMail("info@avonessa.com", "k.temirgaliyev@hotmail.com", "Unknown error", "Unknown_error");
                }
            }
            finally
            {
                IDisposable disposable = srv as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
        }

        private void SendMail(string mailFrom, string mailTo, string subject, string whatBody, string sO_Id = "", string Ccode="")
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("mail.avonessa.com");

            mail.From = new MailAddress(mailFrom);
            mail.To.Add(mailTo);
            mail.Subject = subject;

            mail.IsBodyHtml = true;
            string htmlBody = "";
            //формирование Message Body:
            if (whatBody == "Error_200")
                htmlBody = "Ошибка 200, платёж не прошёл. HTTP error handling.";
            if (whatBody == "ToUs")
            {
                mail.To.Add("info@avonessa.com");
                mail.To.Add("avonessa@gmail.com");
                htmlBody = @"Оплачен товар на сайте Avonessa.com.<br />E-Mail покупателя: " + Payer_email + ".<br />" +
                "Номер транзакции: " + Txn_id + ".<br />Номер заказа: " + sO_Id + "<br />Проверить на сайте http://www.avonessa.com/Administrator";
            }
            string c_code = "";
            if (Ccode == "RU" || Ccode == "KZ" || Ccode == "AZ" || Ccode == "BY" || Ccode == "AM" || Ccode =="GE" 
                    || Ccode == "KG" || Ccode == "LV" || Ccode =="LT" || Ccode =="MD" || Ccode=="TJ" || Ccode=="TM"
                    || Ccode =="UZ" || Ccode=="UA" || Ccode=="EE") // Russian only
                c_code = "RU";
            DataSet ds =null;
            DataSet dsCustomer = null;
            string sTR = "";
            decimal dSum = 0.00m;
            decimal dItog = 0.00m;
            if (sO_Id != "")
            {
                ds = GetOrderData(int.Parse(sO_Id));
                
                if (c_code == "RU")
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        sTR += "<tr><td>" + dr["ProductName"].ToString() + "</td><td align='right'>$"+ dr["ProductCost"].ToString()+ " USD</td>" +
                        "<td align='center'>"+ dr["P_Quantity"].ToString()+ "</td><td align='right'>$"+ dr["pSum"].ToString()+ " USD</td></tr>";
                        //стоимость товаров:
                        dSum += decimal.Parse(dr["ProductCost"].ToString()) * int.Parse(dr["P_Quantity"].ToString());
                    }
                else
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        sTR += "<tr><td>" + dr["ProductNameEnglish"].ToString() + "</td><td align='right'>$"+ dr["ProductCost"].ToString()+ " USD</td>" +
                        "<td align='center'>"+ dr["P_Quantity"].ToString()+ "</td><td align='right'>$"+ dr["pSum"].ToString()+ " USD</td></tr>";
                        //стоимость товаров:
                        dSum += decimal.Parse(dr["ProductCost"].ToString()) * int.Parse(dr["P_Quantity"].ToString());
                    }
            }
            
            dItog = dSum + 8.75m;
            dsCustomer = GetCustomerData(int.Parse(sO_Id));
            string sShipAddress = "";
            if (c_code == "RU")
                foreach (DataRow drC in dsCustomer.Tables[0].Rows)
                {
                    sShipAddress = @"ФИО: " + drC["CR_ContactName"].ToString() + "<br />E-Mail: " + drC["CR_Email"].ToString() +
                        "<br />Адрес доставки: <div style='font-weight: bold;'>" + drC["CR_AddressCountry"].ToString() + ", " + drC["CR_PostalCode"].ToString() + 
                        ", " + drC["CR_AddressCity"].ToString() + ", " + drC["CR_AddressStreet"].ToString() + "</div>";
                }
            else
                foreach (DataRow drC in dsCustomer.Tables[0].Rows)
                {
                    sShipAddress = @"Contact person: " + drC["CR_ContactName"].ToString() + "<br />E-Mail: " + drC["CR_Email"].ToString() +
                        "<br />Shipping address: <div style='font-weight: bold;'>" + drC["CR_AddressStreet"].ToString()+ ", " +
                        drC["CR_AddressCity"].ToString() + ", " + drC["CR_AddressState"].ToString()+ " " + drC["CR_PostalCode"].ToString() +
                        ", " + drC["CR_AddressCountry"].ToString() + "</div>";
                }
            if (whatBody == "ToBuyer")//потом доделать с изображениями
            {
                if (c_code == "RU") // Russian only
                {
                    mail.Subject = "Оплачен товар на Avonessa.com";
                    htmlBody = @""+
                    "<head><basefont face='verdana, courier, serif' size='2'></head>Уважаемый покупатель,<br />Ваш заказ №" + sO_Id +
                    " принят в обработку.<br /><br /> Товары: <table class='boldtable' border='1' cellspacing='1' cellpadding='4'>" +
                    "<tbody style='font-family:verdana; font-size:x-small'>"+
                    "<tr><td bgcolor='#FFFFCC' align='center'>Описание</td><td bgcolor='#FFFFCC' align='center'>Цена</td><td bgcolor='#FFFFCC' align='center'>Количество</td><td bgcolor='#FFFFCC' align='center'>Сумма</td></tr>"+ sTR + 
                    "<tr><td colspan=2></td><td align='right'>Итог:</td><td align='right'>$"+ dSum.ToString().Replace(",", ".") +" USD</td></tr>"+
                    "<tr><td colspan=2></td><td align='right'>Доставка:</td><td align='right'>$"+ 8.75 +" USD</td></tr>"+
                    "<tr><td colspan=2></td><td align='right'>Общий итог:</td><td align='right'>$"+ dItog.ToString().Replace(",", ".") +" USD</td></tr>"+
                    "</tbody></table><br /><br />" + sShipAddress + "<br />Спасибо за покупку на http://Avonessa.com!<br />*Проверьте, пожалуйста, адресные данные для поставки.<br />"+
                    "По любым вопросам, пожалуйста, пишите на info@avonessa.com";
                }
                else //English only
                {
                    mail.Subject = "Payment to Avonessa.com is completed";
                    htmlBody =  @""+
                    "<head><basefont face='verdana, courier, serif' serif' size='2'></head>Dear Buyer,<br />Your order #" + sO_Id +
                    " accepted for processing.<br /><br />Products: <table class='boldtable' border='1' cellspacing='1' cellpadding='4'>"+
                    "<tbody style='font-family:verdana; font-size:x-small'>"+
                    "<tr><td bgcolor='#FFFFCC' align='center'>Description</td><td bgcolor='#FFFFCC' align='center'>Unit price</td><td bgcolor='#FFFFCC' align='center'>Quantity</td><td bgcolor='#FFFFCC' align='center'>Amount</td></tr>"+ sTR + 
                    "<tr><td colspan=2></td><td align='right'>Subtotal:</td><td align='right'>$"+ dSum.ToString().Replace(",", ".") +" USD</td></tr>"+
                    "<tr><td colspan=2></td><td align='right'>Shipping and handling:</td><td align='right'>$"+ 8.75 +" USD</td></tr>"+
                    "<tr><td colspan=2></td><td align='right'>Grand total:</td><td align='right'>$"+ dItog.ToString().Replace(",", ".") +" USD</td></tr>"+
                    "</tbody></table><br /><br />" + sShipAddress + "<br />Thank you for an order in the online store http://Avonessa.com!<br />" +
                    "*Check the shipping address, please.<br />" +
                    "For any questions write to info@avonessa.com, please.";
                }
            }
            if (whatBody == "IPN_IsHacked")
                htmlBody = "IPN взломан хакерами!";
            if (whatBody == "responseText_INVALID")
                htmlBody = "srv.responseText = INVALID";
            if (whatBody == "Unknown_error")
                htmlBody = "Unknown error";

            mail.Body = htmlBody;

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("info@avonessa.com", "545747[]sql");
            SmtpServer.EnableSsl = false;

            SmtpServer.Send(mail);
        }

        protected DataSet GetOrderData(int o_id)
        {
            Database db = DatabaseFactory.CreateDatabase();

            string sqlCommand = "sp_GetOrderProducts";
            DbCommand dbCommand = db.GetStoredProcCommand(sqlCommand);

            // Retrieve products from the specified category.
            db.AddInParameter(dbCommand, "O_ID", DbType.Int32, o_id);

            // DataSet that will hold the returned results		
            DataSet productsDataSet = null;

            productsDataSet = db.ExecuteDataSet(dbCommand);

            // Note: connection was closed by ExecuteDataSet method call 

            return productsDataSet;
        }

        protected DataSet GetCustomerData(int o_id)
        {
            Database db = DatabaseFactory.CreateDatabase();

            string sqlCommand = "sp_GetCustomerData";
            DbCommand dbCommand = db.GetStoredProcCommand(sqlCommand);

            // Retrieve products from the specified category.
            db.AddInParameter(dbCommand, "O_ID", DbType.Int32, o_id);

            // DataSet that will hold the returned results		
            DataSet customerDataSet = null;

            customerDataSet = db.ExecuteDataSet(dbCommand);

            // Note: connection was closed by ExecuteDataSet method call 

            return customerDataSet;
        }
    }
}