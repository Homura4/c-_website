﻿using BLL.DataBLL;
using Common;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using website.Common;
using website.Common.Authentication;
using website.Models;

namespace website.Controllers
{
    public class LoginController : Controller
    {
        #region 页面访问方法

        /// <summary>
        /// 登录页面返回视图
        /// </summary>
        /// <returns>ActionResult.</returns>
        [CustomAuthorize]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 跳转登录页面
        /// </summary>
        /// <param name="returnUrl">登录后跳转页面</param>
        /// <returns>ActionResult.</returns>
        [HttpGet]
        public ActionResult Login(String returnUrl = null)
        {
            // 清除登录信息
            FormsAuthenticationService.SignOut();
            Session.Clear();
            return View(new UserViewModel() { ReturnUrl = returnUrl });
        }

        /// <summary>
        /// 验证码
        /// </summary>
        [OutputCache(Location = OutputCacheLocation.None)]
        public void VCode()
        {
            var code = new ValidateCode();
            String retInfo;
            var codeStr = code.CreateValidateCode(out retInfo);
            Session["vcode"] = retInfo;
            code.CreateValidateGraphic(codeStr);
        }

        /// <summary>
        /// 登录
        /// </summary>
        [HttpPost]
        public ActionResult Login(UserViewModel model, String vcode)
        {
            String message = String.Empty;
            Boolean result = false;

            if (Session["vcode"] == null)
            {
                message = "验证码过期";
                model.Message = message;
                model.UserPwd = String.Empty;
                return View("Login", model);
            }

            if (Session["vcode"].ToString() != vcode)
            {
                message = "验证码错误";
                model.Message = message;
                model.UserPwd = String.Empty;
                return View("Login", model);
            }

            if (String.IsNullOrEmpty(model.UserName) || String.IsNullOrEmpty(model.UserPwd))
            {
                message = "请输入账号、密码！";
                model.Message = message;
                model.UserPwd = String.Empty;
                return View("Login", model);
            }

            var loginUserByDB = GetUser(model.UserName, model.UserPwd);
            if (loginUserByDB == null)
            {
                message = "请输入正确的账号、密码！";
                model.Message = message;
                model.UserPwd = String.Empty;
                return View("Login", model);
            }

            if (loginUserByDB.Status != 0)
            {
                message = "您的帐号已被锁定，请联系管理员！";
                model.Message = message;
                model.UserPwd = String.Empty;
                return View("Login", model);
            }

            var loginUser = ModelConvert(loginUserByDB);
            var loginIp = Request.UserHostAddress;
            UpdateLoginInfo(loginUserByDB, loginIp);
            FormsAuthenticationService.SignIn(loginUser);

            //日志记录
            DataAccessBll.Insert(new UserOperationLog
            {
                UserID = loginUser.UserID,
                UserName = loginUser.UserName,
                OperationMothod = "Home.Login",
                OperationName = "系统登录",
                OperationData = "",
                ReturnData = String.Empty,
                Crdate = DateTime.Now
            });

            Session["vcode"] = String.Empty;

            // 登陆成功 判断之前是否访问某个页面 没有就跳转到home
            if (String.IsNullOrEmpty(model.ReturnUrl) || model.ReturnUrl.Trim() == "/")
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return Redirect(model.ReturnUrl);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="pwd">密码</param>
        /// <returns></returns>
        private static User GetUser(String userName, String pwd)
        {
            // 获取用户
            String userPwd = EncryptionMD5.MD5Encrypt32(pwd, EncryptionMD5.LetterCase.UpperCase);
            var user = DataAccessBll.GetDefinedList(new User
            {
                UserName = userName,
                UserPwd = userPwd
            });

            return user.FirstOrDefault();
        }

        /// <summary>
        /// 模型转化
        /// </summary>
        private UserViewModel ModelConvert(User model)
        {
            if (model == null)
            {
                return null;
            }

            var role = DataAccessBll.GetList(new Role { ID = model.UserRole }).FirstOrDefault();

            return new UserViewModel()
            {
                UserID = model.UserID,
                Status = model.Status,
                UserName = model.UserName,
                UserPwd = model.UserPwd,
                IfSuper = model.IfSuper,
                UserRole = model.UserRole,
                LastLoginIP = model.LastLoginIP,
                LastLoginTime = model.LastLoginTime,
                Crdate = model.LastLoginTime,

                UserRoleName = role == null ? "角色不存在" : role.RolesName,
                MenuId = role == null ? "" : role.Page,
            };
        }

        /// <summary>
        /// 更新登录信息
        /// </summary>
        /// <param name="user">玩家</param>
        /// <param name="loginIp">登录ip</param>
        private static void UpdateLoginInfo(User user, String loginIp)
        {
            user.LastLoginIP = loginIp;
            user.LastLoginTime = DateTime.Now;

            DataAccessBll.Update(user);
        }

        #endregion
    }
}