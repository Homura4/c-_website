﻿// ***********************************************************************
// Assembly         : website
// Author           : yangyang
// Created          : 11-01-2018
//
// Last Modified By : yangyang
// Last Modified On : 11-01-2018
// ***********************************************************************
// <copyright file="OperationLogAttribute.cs" company="">
//     版权所有(C)  2018
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace website
{
    using Model;
    using BLL.DataBLL;
    using website.Common.Authentication;

    /// <summary>
    /// 操作日志属性
    /// </summary>
    public class OperationLogAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 参数名称列表,可以用, | 分隔
        /// </summary>
        private readonly String parameterNameList;

        /// <summary>
        /// 操作说明
        /// </summary>
        private string operationExplain = "";

        /// <summary>
        /// 操作日志
        /// </summary>
        /// <param name="explain">操作说明</param>
        /// <param name="parm">日志需要记录的参数名称列表,用(, 或| )分隔</param>
        public OperationLogAttribute(String explain, String parm)
        {
            operationExplain = String.IsNullOrEmpty(explain) ? String.Empty : explain;
            parameterNameList = String.IsNullOrEmpty(parm) ? String.Empty : parm;
        }

        /// <summary>
        /// 执行操作（调用方法特性后由框架调用）
        /// </summary>
        /// <param name="filterContext">过滤上下文</param>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //方法名称
            var actionName = filterContext.ActionDescriptor.ActionName;
            //控制器
            var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;

            Dictionary<String, String> parmsObj = new Dictionary<String, String>();

            foreach (var item in parameterNameList.Split(',', '|'))
            {
                var valueProviderResult = filterContext.Controller.ValueProvider.GetValue(item);

                if (valueProviderResult != null && !parmsObj.ContainsKey(item))
                {
                    parmsObj.Add(item, valueProviderResult.AttemptedValue);
                }
            }

            var ticketUser = FormsAuthenticationService.GetAuthenticatedUser();
            try
            {
                String responseDataStr = JsonConvert.SerializeObject(filterContext.Result);
                if (responseDataStr == null) responseDataStr = String.Empty;

                DataAccessBLL.Insert(new UserOperationLog
                {
                    UserID = ticketUser?.UserID ?? 0,
                    UserAccount = ticketUser == null ? "未知用户" : ticketUser.UserAccount,
                    OperationMothod = $"{controllerName}.{actionName}",
                    OperationName = operationExplain,
                    OperationData = JsonConvert.SerializeObject(parmsObj),
                    ReturnData = responseDataStr,
                    Crdate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // 记录错误日志
            }
        }
    }
}