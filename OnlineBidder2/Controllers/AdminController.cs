using OnlineBidder2.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;


namespace OnlineBidder2.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        // GET: Addproduct

        REINMARTEntities db = new REINMARTEntities();
        public ActionResult Index()
        {

            return View(db.products.ToList());
        }
        public ActionResult Create()
        {

            return View();
        }
        [HttpPost]
        public ActionResult Create(HttpPostedFileBase file, product emp)
        {
            string filename = Path.GetFileName(file.FileName);
            //string _filename = DateTime.Now.ToString("yymmssfff");
            string extension = Path.GetExtension(file.FileName);
            string path = Path.Combine(Server.MapPath("~/assets/img/product/"), filename);
            emp.image = "~/assets/img/product/" + filename;
            if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png") ;
            {
                if (file.ContentLength <= 1000000)
                {
                    db.products.Add(emp);
                    if (db.SaveChanges() > 0)
                    {
                        file.SaveAs(path);
                        ViewBag.Message = "Record Added";
                        ModelState.Clear();
                    }
                }
                else
                {
                    ViewBag.Message = "Size is not valid";
                }
            }
            return View();
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var product = db.products.Find(id);
            Session["imgPath"] = product.image;

            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(HttpPostedFileBase file, product emp)
        { 
            if (ModelState.IsValid)
            {
                if (file != null)// image is given
                {
                    string filename = Path.GetFileName(file.FileName);
                    string _filename = DateTime.Now.ToString("yymmssfff");
                    string extension = Path.GetExtension(file.FileName);
                    string path = Path.Combine(Server.MapPath("~/assets/img/product/"), filename);
                    emp.image = "~/assets/img/product/" + filename;
                    if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png") ;
                    {
                        if (file.ContentLength <= 1000000)
                        {
                            db.Entry(emp).State = EntityState.Modified;
                            string oldImgPath = Request.MapPath(Session["imgPath"].ToString());
                            if (db.SaveChanges() > 0)
                            {
                                file.SaveAs(path);
                                if (System.IO.File.Exists(oldImgPath)) ;
                                {
                                    System.IO.File.Delete(oldImgPath);
                                }

                                ViewBag.Message = "Data has been updated";
                                ModelState.Clear();
                            }
                        }
                        else
                        {
                            ViewBag.Message = "Image Size is not valid. Please Change It.";
                        }
                    }
                }
                else // image is not given
                {
                    emp.image = Session["imgPath"].ToString();
                    db.Entry(emp).State = EntityState.Modified;
                    if (db.SaveChanges() > 0)
                    {
                        ViewBag.Message = "Data has been updated";
                        return RedirectToAction("Index");
                    }
                }
            }
            

            return View();
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var product = db.products.Find(id);
            //Session["imgPath"] = product.image;

            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var product = db.products.Find(id);
            //Session["imgPath"] = product.image;

            if (product == null)
            {
                return HttpNotFound();
            }
            string currentimg = Request.MapPath(product.image);
            db.Entry(product).State = EntityState.Deleted;
            if (db.SaveChanges() > 0)
            {
                if (System.IO.File.Exists(currentimg))
                {
                    System.IO.File.Delete(currentimg);
                }
                TempData["Message"] = "Data deleted!";
                return RedirectToAction("Index");
            }
            return View();
        }
    }
}