using Gunny.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Gunny.Controllers
{
    public class TransCashController : Controller
    {
        private readonly Member_GMPContext _context;


        public TransCashController(Member_GMPContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            string cookieValueFromReq = Request.Cookies["gunny_userid"];
            if (cookieValueFromReq == null)
            {

                return Redirect("/dang-nhap");
            }
            else
            {
                var listServer = _context.ServerLists.ToList();
                var listUsers = getListUser(listServer.FirstOrDefault().Database);
                ViewBag.listUsers = listUsers;
                return View(listServer);
            }

        }
        public ActionResult getList(int ServerID)
        {
            string cookieValueFromReq = Request.Cookies["gunny_userid"];
            List<Sys_Users_Detail> result = new List<Sys_Users_Detail>();
            if (cookieValueFromReq != null)
            {
                var ServerInfo = _context.ServerLists.Where(p => p.ServerId == ServerID).FirstOrDefault();
                if (ServerInfo != null)
                {
                    result = getListUser(ServerInfo.Database);
                }
            }

            return Json(result);
        }

        [HttpPost]
        public ActionResult Dotranscash(int serverID, int userID, int amount)
        {
            JsonResult result = new JsonResult();
            result.Code = -99;
            result.Message = "Lỗi dữ liệu";

            int _userID = 0;
            int _amount = 0;
            int.TryParse(userID.ToString(), out _userID);
            int.TryParse(amount.ToString(), out _amount);

            MemAccount account = _context.MemAccounts.Where(p => p.Email == Request.Cookies["gunny_username"]).FirstOrDefault();
            if (account != null)
            {
                var ServerInfo = _context.ServerLists.Where(p => p.ServerId == serverID).FirstOrDefault();
                if (ServerInfo != null)
                {
                    if (_amount > 0 && _amount <= account.Money)
                    {
                        var UserDetail = getUserById(ServerInfo.Database, _userID);
                        if (UserDetail != null)
                        {
                            account.Money -= _amount;
                            string ChargeId = Guid.NewGuid().ToString();
                            string strQuery = "INSERT INTO " + ServerInfo.Database + ".[dbo].[Charge_Money] ([ChargeID] ,[UserName] ,[Money] ,[Date] ,[CanUse] ,[PayWay] ,[NeedMoney] ,[IP] ,[NickName]) VALUES ('" + ChargeId + "','" + UserDetail.UserName + "','" + _amount + "','" + DateTime.Now.ToString() + "','true','transCash','0','','" + UserDetail.NickName + "')";
                            Helper.SqlQuery(_context, strQuery);
                            result.Code = 0;
                            result.Message = "Chuyển thành công " + _amount + " xu vào game";
                            _context.Entry(account).State = EntityState.Modified;
                            _context.SaveChanges();

                            try
                            {
                                ServiceReference.CenterServiceClient centerService = new ServiceReference.CenterServiceClient(ServiceReference.CenterServiceClient.EndpointConfiguration.NetTcpBinding_ICenterService, new EndpointAddress(ServerInfo.LinkCenter));
                                centerService.ChargeMoneyAsync(UserDetail.UserID, ChargeId);

                            }
                            catch
                            {

                            }
                        }
                    }
                    else
                    {
                        result.Message = "Số xu muốn chuyển phải lớn hơn 0 và nhỏ hơn số xu bạn đang sở hữu";
                    }
                }

            }
            return Json(result);
        }
        private List<Sys_Users_Detail> getListUser(string dbName)
        {
            string cookieValueFromReq = Request.Cookies["gunny_userid"];
            if (cookieValueFromReq != null)
            {

                string strQuery = @"SELECT [UserID], [UserName], [NickName] FROM " + dbName + ".[dbo].[Sys_Users_Detail] WHERE [UserName] = '" + Request.Cookies["gunny_username"] + "'";
                return Helper.RawSqlQuery(_context, strQuery, x => new Sys_Users_Detail { UserID = (int)x[0], UserName = (string)x[1], NickName = (string)x[2] }).ToList();
            }
            else
            {
                return new List<Sys_Users_Detail>();
            }
        }

        private Sys_Users_Detail getUserById(string dbName, int UserId)
        {
            string cookieValueFromReq = Request.Cookies["gunny_userid"];
            if (cookieValueFromReq != null)
            {

                string strQuery = @"SELECT [UserID], [UserName], [NickName] FROM " + dbName + ".[dbo].[Sys_Users_Detail] WHERE [UserID] = " + UserId;
                return Helper.RawSqlQuery(_context, strQuery, x => new Sys_Users_Detail { UserID = (int)x[0], UserName = (string)x[1], NickName = (string)x[2] }).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public class Sys_Users_Detail
        {
            public int UserID { get; set; }
            public string UserName { get; set; }
            public string NickName { get; set; }
        }
        public class JsonResult
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }

    }
}
public static class Helper
{
    public static List<T> RawSqlQuery<T>(DbContext context, string query, Func<DbDataReader, T> map)
    {
        using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            context.Database.OpenConnection();

            using (var result = command.ExecuteReader())
            {
                var entities = new List<T>();

                while (result.Read())
                {
                    entities.Add(map(result));
                }
                context.Database.CloseConnection();
                return entities;
            }
        }
    }

    public static void SqlQuery(DbContext context, string query)
    {
        using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            context.Database.OpenConnection();
            command.ExecuteReader();
            context.Database.CloseConnection();
        }
    }
}
