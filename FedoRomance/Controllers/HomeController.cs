﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using FedoRomance.Data;
using FedoRomance.Data.Repositories;
using FedoRomance.Web.Models;

namespace FedoRomance.Web.Controllers
{
    
    public class HomeController : Controller
    {
        
        
        //Hämtar pending requests för användaren och visar i layout//
        [AllowAnonymous]
        public ActionResult FriendCount(string username) {
            

                var profile = ProfileRepository.GetProfile(username);
                var friendRequests = FriendsRepositories.GetPendingRequests(profile.UID);

                ViewBag.FriendRequests = friendRequests;

                return PartialView("FriendCount");
            
        }

        public ActionResult Index()
        {
            var getUser1 = IndexRepository.GetUsers("GasPer");
            var getUser2 = IndexRepository.GetUsers("antkop");
            var getUser3 = IndexRepository.GetUsers("HenHo");

            var model = new IndexModel();
            model.ExampleUser1 = getUser1.Username;
            model.ExampleUser2 = getUser2.Username;
            model.ExampleUser3 = getUser3.Username;
            model.ExampleUserPic1 = getUser1.Picture;
            model.ExampleUserPic2 = getUser2.Picture;
            model.ExampleUserPic3 = getUser3.Picture;

            return View(model);
        }




        /*--------------------------LOGIN/LOGOUT-------------------*/

        public ActionResult LogIn() {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Profile", "Home", new { username = User.Identity.Name});
            }
            return View();
        }


        [HttpPost] public ActionResult LogIn(LogInModel model)
        {
            var user = LogInRepository.LogIn(model.Username, model.Password);

            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(model.Username, false);
                return RedirectToAction("Profile", "Home", new {username = User.Identity.Name});
            }
            else
            {

                TempData["loginAlert"] = "<script>alert('Wrong username or password!');</script>";
                FormsAuthentication.SignOut();


                return View();
            }
        }

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        


        /*--------------------------EDIT/REGISTER-------------------*/
        
        public void EditAndRegister() {
            var age = new List<SelectListItem>();
            var gender = new List<SelectListItem>();

            for (int i = 18; i <= 150; i++) {
                age.Add(new SelectListItem {
                    Text = i.ToString(),
                    Value = i.ToString()
                });
            }

            string[] genderList =
            {
                "Gentleman", "M'lady"
            };

            foreach (var x in genderList) {
                gender.Add(new SelectListItem {
                    Text = x,
                    Value = x
                });
            }

            ViewData["Age"] = age;
            ViewData["Gender"] = gender;
        }
        
        public ActionResult Edit() {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }

            EditAndRegister();
            var currentUser = User.Identity.Name;
            var info = EditRepository.GetUser(currentUser);
            
            var model = new EditModel();
            model.Name = info.Name;
            model.Age = info.Age;
            model.Gender = info.Gender;
            model.About = info.About;
            model.Visible = info.Visible;
            return View(model);
        }


        [HttpPost] public ActionResult Edit(EditModel model, HttpPostedFileBase file)
        {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }

            EditAndRegister();
            var currentUser = User.Identity.Name;
            var info = EditRepository.GetUser(currentUser);

            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    var extension = Path.GetExtension(file.FileName);

                    if (extension == ".jpeg" || extension == ".jpg" || extension == ".png")
                    {
                        var picName = currentUser + extension;
                        var folder = Server.MapPath("~/Images");
                        var path = Path.Combine(folder, picName);

                        if (info.Picture != null)
                        {
                            var currentPicName = info.Picture;
                            var currentPicPath = Path.Combine(folder, currentPicName);

                            if (System.IO.File.Exists(currentPicPath))
                            {
                                System.IO.File.Delete(currentPicPath);
                            }
                        }
                        file.SaveAs(path);
                        model.Picture = picName;
                        EditRepository.UploadUserPic(currentUser, picName);
                    }
                    else
                    {
                        TempData["alertWrongPicFormat"] = "<script>alert('Wrong picture format! (.jpeg, .jpg or .png or gtfo!)');</script>";
                        return View();
                    }
                }

                EditRepository.EditUser(currentUser, model.Name, model.Age, model.Gender, model.About, model.Visible);
                TempData["alertSuccessfulEdit"] = "<script>alert('Editing successful!');</script>";
                return View();
            }
            return View();
        }



        public ActionResult EditPassword()
        {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }
            return View();
        }



        [HttpPost]
        public ActionResult EditPassword(EditPasswordModel model) 
        {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }

            var currentUser = User.Identity.Name;

            if (ModelState.IsValid)
            {
                if (EditRepository.PasswordCheck(currentUser, model.CurrentPassword) == false)
                {
                    TempData["alertFailPasswordEdit"] = "<script>alert('Current password incorrect. Go away invader!');</script>";
                    return View();
                }
                EditRepository.NewPassword(currentUser, model.CurrentPassword, model.NewPassword);
                TempData["alertSuccessfulPasswordEdit"] = "<script>alert('Password changed. Fuck em hackers!');</script>";
                return View();
            }
            return View();
        }



        public ActionResult Register() {
            if (User.Identity.IsAuthenticated) {
                return RedirectToAction("Profile", "Home", new { username = User.Identity.Name });
            }
            EditAndRegister();
            return View();
        }

        
        [HttpPost] public ActionResult Register(RegisterModel model)
        {
            EditAndRegister();
            if (ModelState.IsValid)
            {      
                if (RegisterRepository.UsernameCheck(model.Username) == false)
                {
                    RegisterRepository.Register(model.Name, model.Age, model.Gender, model.About, model.Username, model.Password, model.Visible);
                    TempData["alertSuccessfulRegister"] = "<script>alert('Registration successful!');</script>";
                    return RedirectToAction("LogIn", "Home");
                }
                    TempData["alertFailRegister"] = "<script>alert('Username already taken yo! :(');</script>";
                    return View();
            }
            return View();
        }





        /*--------------------------SEARCH/PROFILE-------------------*/

        public ActionResult Search() {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }
            return View();
        }

        [HttpPost] public ActionResult Search(SearchModel model)
        {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }
            var search = SearchRepository.Search(model.SearchValue);
            var result = new List<string>();

            foreach (User item in search)
            {
                if (item.Visible != 1)
                {
                    result.Add(item.Username);
                }
            }

            ViewData["SearchResult"] = result;
            
            return View();
        }
        
        public ActionResult Profile(string username) {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }

            if (username == null) {
                username = User.Identity.Name;
            }

            var info = ProfileRepository.GetProfile(username);
            var loggedInUser = ProfileRepository.GetProfile(User.Identity.Name);
            
            var model = new ProfileModel();
            model.UID = info.UID;
            model.Username = info.Username;
            model.Name = info.Name;
            model.Age = info.Age;
            model.Gender = info.Gender;
            model.About = info.About;
            model.Picture = info.Picture;

            Friend friendship = FriendsRepositories.GetFriendship(info.UID, loggedInUser.UID);

            ViewBag.Friendship = friendship;
            ViewBag.CurrentUser = loggedInUser.Username;
            
            return View(model);
        }

 /*--------------------------FRIENDS-------------------*/


        //Friends and outgoing/incoming requests//

        public ActionResult Friends() {
            if (User.Identity.IsAuthenticated == false) {
                return RedirectToAction("LogIn", "Home");
            }

            var userName = System.Web.HttpContext.Current.User.Identity.Name;
            User myProfile = ProfileRepository.GetProfile(userName);

            List<Friend> friendList = FriendsRepositories.GetFriends(myProfile.UID);
            List<Friend> pendingList = FriendsRepositories.CheckFriendsRequests(myProfile.UID);
            ViewBag.Friends = friendList;
            ViewBag.CurrentUser = myProfile.Username;
            ViewBag.Pending = pendingList;

            return View();
        }
       

        //Send friend request to the current profile you are visiting//
        public ActionResult AddFriend(string friendName) {
            
                var userName = System.Web.HttpContext.Current.User.Identity.Name;

               var model = new Friend();
               var myProfile = ProfileRepository.GetProfile(userName);
               var friendProfile = ProfileRepository.GetProfile(friendName);

                model.UID = myProfile.UID;
                model.FID = friendProfile.UID;
                model.Accepted = false;

               FriendsRepositories.AddFriend(model);

               ViewBag.Message = "Test";

               return RedirectToAction("Friends");
           
       }
        

        //Accept a friend request
        public ActionResult AcceptFriend(string uid, string fid) {

            
                var userid = int.Parse(uid);
                var friendid = int.Parse(fid);

                FriendsRepositories.UpdateFriendConfirmed(userid, friendid);

                return RedirectToAction("Friends");
            
            
        }


        public ActionResult ShowRequestingUsers() {
            
                var userId = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
                var result = FriendsRepositories.CheckFriendsRequests(userId);
                var model = new FriendsModel {
                    FriendsList = result
                };

                ViewBag.UserId = userId;
                return RedirectToAction("Friends");
            
          }
        


/*--------------------------language-------------------*/
        [HttpGet]
        public ActionResult ChangeCulture(string lang)
        {
            var langCookie = new HttpCookie("lang", lang) { HttpOnly = true };

            Response.AppendCookie(langCookie);

            return RedirectToAction("Register", "Home", new { culture = lang });  
        }

	}
}