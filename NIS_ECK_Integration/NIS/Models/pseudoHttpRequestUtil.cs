using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Com.Mayaminer;

namespace NIS.Models
{
    public delegate R doSomethingWithPseudoRequest<T, R>(T controller) where T : Controller, new();

    /// <summary>
    /// 此Utility用途
    /// 製造一個假的有Request的Controller，方便在後端串接其他Action
    /// e.g. 存檔後拋列印程式
    /// </summary>
    public class pseudoHttpRequestUtil
    {
        /// <summary>
        /// 產生一個後端的虛擬Request，去執行其他Controller的Action
        /// 可指定同步執行或非同步執行
        /// </summary>
        /// <typeparam name="T">Controller類別</typeparam>
        /// <typeparam name="R">回傳值的型別</typeparam>
        /// <param name="url">Request的URL</param>
        /// <param name="actionName">要執行的Action名稱</param>
        /// <param name="doRequest">執行Action的動作Callback</param>
        /// <param name="async">是否為非同步處理</param>
        /// <returns></returns>
        public static R generatePseudoRequest<T, R>(string url, string actionName, doSomethingWithPseudoRequest<T, R> doRequest, bool async = false) where T:Controller, new()
        {
            R result = default(R);

            if (async)
            {
                //使用背景執行序處理請求
                ThreadPool.QueueUserWorkItem((Object stateInfo) => {
                    generatePseudoRequestSync(url, actionName, doRequest);
                });
            }
            else
            {
                result = generatePseudoRequestSync(url, actionName, doRequest);
            }
            
            return result;
        }

        /// <summary>
        /// 產生一個後端的虛擬Request，去執行其他Controller的Action
        /// </summary>
        /// <typeparam name="T">Controller類別</typeparam>
        /// <typeparam name="R">回傳值的型別</typeparam>
        /// <param name="url">Request的URL</param>
        /// <param name="actionName">要執行的Action名稱</param>
        /// <param name="doRequest">執行Action的動作Callback</param>
        /// <returns></returns>
        private static R generatePseudoRequestSync<T, R>(string url, string actionName, doSomethingWithPseudoRequest<T, R> doRequest) where T : Controller, new()
        {
            R result = default(R);
            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new ArgumentOutOfRangeException("url", "The URL must be a well-formed absolute URI.");
                }
                
                T ctrl = new T();

                using (var sw = new System.IO.StringWriter())
                {
                    HttpRequest request = new HttpRequest(string.Empty, url, null);
                    HttpResponse response = new HttpResponse(sw);
                    var fakeHttpContext = new HttpContext(request, response);
                    var fakeHttpContextWrapper = new HttpContextWrapper(fakeHttpContext);

                    RouteData route = new RouteData();
                    string controllerClassName = TypeDescriptor.GetClassName(typeof(T));
                    string controllerName = controllerClassName.Substring(controllerClassName.LastIndexOf(".") + 1);
                    route.Values.Add("controller", controllerName.Replace("Controller", ""));
                    route.Values.Add("action", actionName);

                    var requestContext = new RequestContext(fakeHttpContextWrapper, route);
                    ControllerContext newContext = new ControllerContext(fakeHttpContextWrapper, route, ctrl);
                    ctrl.ControllerContext = newContext;

                    //Do something
                    if (doRequest != null)
                    {
                        result = doRequest(ctrl);
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    string errorMsg = "";
                    errorMsg += "後端模擬Request時發生錯誤\n";
                    errorMsg += "錯誤訊息：" + e.Message + "\n";
                    errorMsg += "URL：" + url + "\n";
                    errorMsg += "Action：" + actionName + "\n\n";
                    LogTool log = new LogTool();
                    log.saveLogMsg(errorMsg, "PseudoRequest");
                }
                catch (Exception e2)
                {
                    //Do nothing
                }
            }
            return result;
        }
    }
}