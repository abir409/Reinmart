using OnlineBidder2.Models;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace OnlineBidder2.Controllers
{
    public class HomeController : Controller
    {
        REINMARTEntities db = new REINMARTEntities();
        
        

        public ActionResult Index(int? page)
        {
            
            // pagination

            var data = (from s in db.products select s);

            var newProduct = data.OrderByDescending(s => s.productId).Take(3);

            ViewBag.newProduct = newProduct.ToList();

            if (page > 0)
                page = page;
            else page = 1;

            int limit = 1;
            int start =(int) (page - 1) * limit;
            int totalProduct = data.Count();
            ViewBag.totalPage = totalProduct;
            ViewBag.pageCurrent = page;
            float numberPage = (float)(totalProduct / limit);
            ViewBag.numberPage = (int)Math.Ceiling(numberPage);
            var dataProduct = data.OrderByDescending(s => s.productId).Skip(start).Take(limit);
            return View(dataProduct.ToList());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View(); 
            
        }

        [HttpGet]
        public ActionResult Login()
        {

            if (Request.Cookies["user"] == null)
            {
                return View();

            }
             else  return RedirectToAction("Index","Home");
            
        }

        [HttpPost]
        public ActionResult Login(user u)
        {
            try
            {
                var usr = db.users.Single(uc => uc.userMail == u.userMail && uc.userPassword == u.userPassword);
                if (usr != null && usr.status == "User")
                {
                    HttpCookie userCookie = new HttpCookie("user", usr.userId.ToString());
                    //HttpCookie username = new HttpCookie("usernameshow", usr.userName);

                    //userCookie.Expires = DateTime.Now
                    Response.Cookies.Add(userCookie);
                    //Response.Cookies.Add(username);

                    return RedirectToAction("");
                }
                else if (usr != null && usr.status == "admin")
                {
                    HttpCookie userCookie = new HttpCookie("user", usr.userId.ToString());
                    //HttpCookie username = new HttpCookie("usernameshow", usr.userName);

                    //userCookie.Expires = DateTime.Now
                    Response.Cookies.Add(userCookie);
                    userCookie = new HttpCookie("status", usr.status);
                    Response.Cookies.Add(userCookie);
                    //Response.Cookies.Add(username);

                    return RedirectToAction("Index", "Admin");
                }
            }
            catch (Exception)
            {
                ModelState.Clear();
                ViewBag.Message = "Login Failed";

                return View();
            }
            return View();



        }

        public ActionResult Logout()
        {

            FormsAuthentication.SignOut();
            HttpCookie nameCookie = Request.Cookies["user"];
            nameCookie.Expires = DateTime.Now.AddDays(-1);
            Response.Cookies.Add(nameCookie);

            nameCookie = Request.Cookies["status"];
            nameCookie.Expires = DateTime.Now.AddDays(-1);
            Response.Cookies.Add(nameCookie);


            return RedirectToAction("Login");
        }
    


        [HttpGet]
        public ActionResult Register()
        {

            if (Request.Cookies["user"] == null)
            {
                return View();

            }
            else return RedirectToAction("Index", "Home");

        }

        [HttpPost]
        public ActionResult Register(user u)
        {
            if (ModelState.IsValid)
            {                
                u.status = "User";
                db.users.Add(u);
                db.SaveChanges();
            }
            ModelState.Clear();
            ViewBag.Message = u.userName + " successfully registered";
            return View();
        }

        public ActionResult UserProfile(int? id)
        {

            if (Request.Cookies["user"] != null)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                var loggedUser = db.users.Find(id);
                return View(loggedUser);

            }
            else return RedirectToAction("Index", "Home");

        }
        [HttpPost]
        public ActionResult UserProfile(HttpPostedFileBase file, user u)
        {
            return View();
        }


        public ActionResult SingleProduct(int? id)
        {

            if (Request.Cookies["user"] != null)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                var product = db.products.Find(id);
                List<placeBid> highestBid = db.placeBids.Where(t => t.productId == id).ToList();
                int highestBidAmount;
                if (highestBid.Count > 0)
                {
                    highestBidAmount = Convert.ToInt32(highestBid[0].HighestBid);
                    var user = db.users.Find(highestBid[0].userId);
                    ViewBag.highestBidderName = user.userName;

                }
                else
                {
                    highestBidAmount = Convert.ToInt32(product.StartingPrice);
                    ViewBag.highestBidderName = "";
                }
                ViewBag.highestBidAmount = highestBidAmount;

                

                if (product == null)
                {
                    return HttpNotFound();
                }

                if (product.EndBidTime > DateTime.Now)
                {
                    ViewBag.Message = "Bid";

                }
                else ViewBag.Message = "NotBid";
              
                return View(product);

            }else return RedirectToAction("Login", "Home");
            
        }

        [HttpGet]
        public ActionResult AddToCart()
        {
            int userId = Convert.ToInt32(Request.Cookies["user"].Value);
            List<placeBid> placebid = db.placeBids.Where(temp => temp.userId == userId).ToList();
            List<product> products = new List<product>();
            product p;
            foreach(var placeBid in placebid)
            {
                p = db.products.Find(placeBid.productId);
                products.Add(p);
            }
            List<product>products1 = new List<product>();
            foreach(var product in products)
            {
                if(product.EndBidTime < DateTime.Now && product.status == "unsold") products1.Add(product);
            }

            int amount = 0;
            foreach(var product in products1)
            {
                List<placeBid> pl = db.placeBids.Where(temp => temp.productId == product.productId).ToList();
                amount += Convert.ToInt32(pl[0].HighestBid);

            }
            ViewBag.amount = amount;
            Session["amount"] = amount.ToString();
            Session["cart"] = products1;
          
            return View(products1);
        }


        [HttpPost]
        public ActionResult checkout()
        {
           
            if (Session["cart"] == null)
            {
               
            }
            else
            {
                
                List<product> prodct_list = (List<product>)Session["cart"];
              
                String st = "";
                foreach(var prodct in prodct_list)
                {
                    prodct.status = "sold";
                    st = st + prodct.productName + ",";
                }
                
                int totProduct = prodct_list.Count;

                var option = new Stripe.Checkout.SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = Convert.ToInt32(Session["amount"])*100,
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = st,
                            },
                        },
                        Quantity = totProduct,
                    },
                },

                    Mode = "payment",
                    SuccessUrl = "https://localhost:44363/Home/Index",
                    CancelUrl = "https://localhost:44363/Home/Index",
                };

               foreach(var prod in prodct_list)
                {
                    product pro = db.products.Find(prod.productId);
                    pro.status = "sold";

                    db.Entry(pro).State = EntityState.Modified;
                    db.SaveChanges();
                }

                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Create(option);

                Response.Headers.Add("Location", session.Url);
                return new HttpStatusCodeResult(303);
            }

            return RedirectToAction("Index", "Home");


        }

        [HttpPost]
        public ActionResult PlaceBid(string id, string amount)
        {
            int idd = Convert.ToInt32(id);
            List<placeBid> placebid = db.placeBids.Where(temp => temp.productId == idd).ToList();
            

            product product = db.products.Find(Convert.ToInt32(idd));

            placeBid pd = new placeBid();

            if(product==null)
            {
                return HttpNotFound();
            }
           
            if (placebid.Count == 0)
            {
                if (Convert.ToInt32(product.StartingPrice) < Convert.ToInt32(amount))
                {
                    pd.userId = Convert.ToInt32(Request.Cookies["user"].Value);
                    pd.productId = Convert.ToInt32(id);
                    pd.HighestBid = amount;
                    DateTime dateTime = DateTime.UtcNow.Date;
                    pd.BidDate = dateTime;

                    db.placeBids.Add(pd);
                    db.SaveChanges();
                    
                }
                else
                {

                }
            }
            else
            {
                if (Convert.ToInt32(placebid[0].HighestBid) < Convert.ToInt32(amount))
                {
                    placebid[0].userId = int.Parse(Request.Cookies["user"].Value);

                    placebid[0].productId = Convert.ToInt32(id);
                    placebid[0].HighestBid = amount;

                    db.Entry(placebid[0]).State = EntityState.Modified;

                    if (db.SaveChanges() > 0)
                    {
                        ViewBag.Message = "Data has been updated";
                        return RedirectToAction("Index");
                    }
                }
                else
                {

                }

            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult OnGoingBdding(int? page)
        {

            // pagination

            var data = (from s in db.products where s.status=="unsold" && s.EndBidTime > DateTime.Now select s);


            if (page > 0)
                page = page;
            else page = 1;

            int limit = 1;
            int start = (int)(page - 1) * limit;
            int totalProduct = data.Count();
            ViewBag.totalPage = totalProduct;
            ViewBag.pageCurrent = page;
            float numberPage = (float)(totalProduct / limit);
            ViewBag.numberPage = (int)Math.Ceiling(numberPage);
            var dataProduct = data.OrderByDescending(s => s.productId).Skip(start).Take(limit);
            return View(dataProduct.ToList());
        }
    }
}
