using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data;
using System.IO;
using System.Net;
using System.Text;

namespace ServiceForTatung
{
    /// <summary>
    /// Summary description for ServiceReceiveTatung
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class ServiceReceiveTatung : System.Web.Services.WebService
    {

        private static string actionType = "pt";
        HTYService.HTYWebService service = new HTYService.HTYWebService();

        //1.EC已拋單
        //2.大同Tatung反饋接收成功
        //3.大同Tatung反饋接收失敗
        //4.EC拋單失敗
        //5.大同Tatung人工更新單號
        //6.大同Tatung主動拋單

        [WebMethod(Description = "建單")]
        public string NewOrder(string account, string password, string adate, string TatungID, string cusname, string custele,
            string cusaddr, string prodnoc, string model, string manfno, string baddesc,
            string buydate, string memo, string station)
        {
            string result = "", pid = "", productnameid = "";


            if (account != "TATUNG" || password != "Tatung24826")
                return "E001";
            DateTime dt = new DateTime();
            if (!DateTime.TryParse(adate, out dt))
                return "E003";
            if (TatungID.Trim() == "")
                return "E004";
            if (cusname.Trim() == "")
                return "E005";
            if (custele.Trim() == "")
                return "E006";
            if (prodnoc.Trim() == "")
                return "E007";
            else
            {
                //用型號找出對應的專案pid跟產品分類名稱
                DataSet ds_p = service.GetProjectByProduct(prodnoc, actionType);
                if (ds_p.Tables[0].Rows.Count > 0)
                {
                    pid = ds_p.Tables[0].Rows[0]["pid"].ToString();
                    productnameid = ds_p.Tables[0].Rows[0]["id"].ToString();
                }
                else
                {
                    DataSet ds_po = service.GetProjectByProduct("其他", actionType);
                    pid = ds_po.Tables[0].Rows[0]["pid"].ToString();
                    productnameid = ds_po.Tables[0].Rows[0]["id"].ToString();
                }
                //return "E007";
            }
            if (baddesc.Trim() == "")
                return "E008";
            DataSet ds = new DataSet();
            //獲取TatunID資訊判斷是否有重複的單號
            ds = service.GetTatungInfo(TatungID, actionType);
            if (ds.Tables[0].Rows.Count > 0)
                return "E002";

            try
            {
                string cid = "-1";


                //保存來電案件
                cid = service.SaveQuestionMessage(dt, "", cusname, custele, dt, "Transfer", baddesc, memo, "Tatung", dt, "Tatung", dt, "Repair",
                    "", dt, "106", "已受理", 1, "", 0, "2", "N", "N", "N", "122", "", "", pid, "", "", "", model, "", "");


                if (cid != "-1" && cid != null && cid != "")
                {
                    //add Tatung info    6 主動拋單
                    service.saveTatungInfo(cid, TatungID, "", "", "", cusaddr, manfno, buydate, adate, station, "6", productnameid, "Tatung", actionType);

                    //add by rubyzhang  來電多選擇單獨存至 來電類型選擇子表
                    service.InsertInboundTypeForTatung(cid, "Tatung", adate, pid, actionType);


                    //添加半小時統計報表資料,添加uid
                    service.insertLog("Tatung", "Tatung", custele, "Tatung", pid, model, cid);

                    //     來電記錄索引即時更新
                    try
                    {
                        service.updateCallLogIndex(cid);
                    }
                    catch
                    { }

                    result = cid;
                }
                else
                    result = "E009";
            }
            catch (Exception ex)
            {
                result = "E009";
                FileDeal.logText("NewOrder-tatungid:" + TatungID + "   ;" + ex.ToString(), "Exception");
            }
            FileDeal.logText("adate:" + adate + ";TatungID:" + TatungID + ";name:" + cusname + ";tele:" + custele + ";cusaddr:" + cusaddr
                + ";prodnoc:" + prodnoc + ";model:" + model + ";manfno:" + manfno + ";baddesc:" + baddesc
                + ";pdate:" + baddesc + ";memo:" + memo + ";station:" + station + ";result:" + result, "ReceiveInfo-Neworder");
            return result;
        }


        [WebMethod(Description = "已有單據處理進度更新")]
        public string NewProcess(string account, string password, string adate, string sioID, string memo)
        {
            string result = "";
            int tid = 0;
            //HTYService.HTYWebService service = new HTYService.HTYWebService();

            if (account != "TATUNG" || password != "Tatung24826")
                return "E001";
            DateTime dt = new DateTime();
            if (!DateTime.TryParse(adate, out dt))
                return "E003";
            if (int.TryParse(sioID, out tid))
            {
                DataSet ds_i = new DataSet();
                ds_i = service.GetSioInfo(sioID, actionType);
                if (ds_i.Tables[0].Rows.Count == 0)
                    return "E010";
            }
            else
                return "E010";
            if (memo.Trim() == "")
                return "E011";

            try
            {
                string pid = "-1";

                pid = service.AddTheCustomerCallInMessage(tid, memo, "已受理", dt, "Tatung", dt, "", "106");


                if (pid != "-1" && pid != null && pid != "")
                    result = "E000";
                else
                    result = "E009";

            }
            catch (Exception ex)
            {
                FileDeal.logText("NewProcess-sioid:" + sioID + "   ;" + ex.ToString(), "Exception");
                result = "E009";
            }
            FileDeal.logText("adate:" + adate + ";sioID:" + sioID + ";memo:" + memo + ";result:" + result, "ReceiveInfo-NewProcess-Tatung");
            return result;
        }


        [WebMethod(Description = "台灣夏普拋轉單據回寫大同單號")]
        public string UpdateScode(string account, string password, string sioID, string tatungID)
        {
            string result = "";
            int tid = 0;
            // HTYService.HTYWebService service = new HTYService.HTYWebService();

            if (account != "TATUNG" || password != "Tatung24826")
                return "E001";
            if (tatungID.Trim() == "")
                return "E004";
            if (int.TryParse(sioID, out tid))
            {
                DataSet ds_i = new DataSet();
                ds_i = service.GetSioInfo(sioID, actionType);
                if (ds_i.Tables[0].Rows.Count == 0)
                    return "E010";
            }
            else
                return "E010";

            DataSet ds = new DataSet();
            ds = service.GetTatungInfo(tatungID, actionType);
            if (ds.Tables[0].Rows.Count > 0)
                return "E002";

            try
            {
                string pid = "-1";

                pid = service.UpdateTatungID(sioID, tatungID, actionType);


                if (pid != "-1" && pid != null && pid != "")
                    result = "E000";
                else
                    result = "E009";

            }
            catch (Exception ex)
            {
                FileDeal.logText("UpdateScode-sioid:" + sioID + "   ;" + ex.ToString(), "Exception");
                result = "E009";
            }
            FileDeal.logText("sioID:" + sioID + ";tatungID:" + tatungID + ";result:" + result, "ReceiveInfo-UpdateScode-Tatung");
            return result;
        }


        //[WebMethod(Description = "文件上傳")]
        public string UploadFile(string account, string password, string sioID, byte[] fs, string fileName)
        {
            string result = "", fileExtension = "";
            int tid = 0, len = 0;
            if (account != "TATUNG" || password != "Tatung24826")
                return "E001";
            if (!int.TryParse(sioID, out tid))
                return "E010";


            fileExtension = fileName.Substring(fileName.IndexOf(".")).ToLower();
            len = fs.Length;
            if (len <= 0)
                return "E012";
            if ((fileExtension == ".mp4" && len / 1024 <= 1024 * 5) || (fileExtension == ".jpg" && len / 1024 <= 1024 * 3))
            {

            }
            else
                return "E012";

            try
            {
                //获取上传案例图片路径
                string path = FilePath(sioID, fileName);
                //定义并实例化一个内存流，以存放提交上来的字节数组。
                MemoryStream m = new MemoryStream(fs);
                //定义实际文件对象，保存上载的文件。
                FileStream f = new FileStream(path, FileMode.Create);
                //把内内存里的数据写入物理文件
                m.WriteTo(f);
                m.Close();
                f.Close();
                f = null;
                m = null;
                string fileurl = path.Replace(System.Configuration.ConfigurationManager.AppSettings["upFilePath"].ToString(), "");
                //HTYService.HTYWebService service = new HTYService.HTYWebService();
                service.InsertFileInfo(sioID, "Tatungtest", fileurl);
                result = "E000";
            }
            catch (Exception ex)
            {
                FileDeal.logText("UploadFile-sioid:" + sioID + "   ;" + ex.ToString(), "Exception");
                result = "E009";
            }
            FileDeal.logText("sioID:" + sioID + ";fileName:" + fileName + ";result:" + result, "ReceiveInfo-UploadFile-Tatung");
            return result;
        }


        private string FilePath(string id, string filename)
        {
            string path = "";
            try
            {
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString();


                string _path = System.Configuration.ConfigurationManager.AppSettings["upFilePath"].ToString() + "HTYUpload\\" + "TATUNG\\" + year + "-" + month + "\\";
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
                path = _path + id + "_" + filename;
                FileInfo f = new FileInfo(path);
                for (int i = 1; f.Exists; i++)
                {   //此文件已存在
                    path = _path + id + "_" + i.ToString() + "_" + filename;
                    f = new FileInfo(path);

                }
            }
            catch
            { }

            return path;
        }


        [WebMethod(Description = "文件URL更新")]
        public string UpdateFileURL(string account, string password, string sioID, string fileUrl)
        {
            string result = "";
            int tid = 0;
            if (account != "TATUNG" || password != "Tatung24826")
                return "E001";
            if (int.TryParse(sioID, out tid))
            {
                DataSet ds_i = new DataSet();
                ds_i = service.GetSioInfo(sioID, actionType);
                if (ds_i.Tables[0].Rows.Count == 0)
                    return "E010";
            }
            else
                return "E010";



            try
            {

                //HTYService.HTYWebService service = new HTYService.HTYWebService();
                service.InsertFileInfo(sioID, "Tatung", fileUrl);
                result = "E000";
            }
            catch (Exception ex)
            {
                FileDeal.logText("UpdateFileURL:" + sioID + "   ;" + ex.ToString(), "Exception");
                result = "E009";
            }
            FileDeal.logText("sioID:" + sioID + ";fileUrl:" + fileUrl + ";result:" + result, "ReceiveInfo-UpdateFileURL-Tatung");
            return result;
        }

        [WebMethod(Description = "調用大同接口")]
        public string TransferToTatung(string account, string password, string centrecode, string adate, string scode, string cusname, string custele1,
                string cusaddr, string prodnoc, string model, string manfno, string baddesc,
                string buydate, string memo, string app2, string station)
        {
            string result = "";
            if (account != "TATUNG" || password != "Tatung24826")
                return "";
            //密碼加密
            string code = "70788522";
            string code_md5 = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(code, "MD5").ToLower();
            password = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile("SHARP" + code_md5 + scode, "MD5").ToLower();
            //string url = "http://service.sampo.com.tw/SharpToSampoApi/Api/CallServices/";

            /*WebApi 正式區網址	http://139.223.3.34:9081/SalesWeb/Sharp_WSService/WEB-INF/wsdl/SharpWSService.wsdl				
            WebApi 測試區網址	http://139.223.15.249:9081/SalesWeb/Sharp_WSService/WEB-INF/wsdl/SharpWSService.wsdl				
            WebApi 內部開發網址	http://139.223.20.158:9080/SalesWeb/Sharp_WSService/WEB-INF/wsdl/SharpWSService.wsdl
            */

            /*
            WebApi 正式區網址	http://139.223.3.34:9081/SalesWeb/Restful/Sharp/NewOrder			
            WebApi 測試區網址	http://139.223.15.249:9081/SalesWeb/Restful/Sharp/NewOrder			
            WebApi 內部開發網址	http://139.223.20.158:9080/SalesWeb/Restful/Sharp/NewOrder
            */

            //SharpWSService.Sharp_WSDelegateClient swsd = new SharpWSService.Sharp_WSDelegateClient();

            //string url = "http://139.223.3.34:9081/SalesWeb/Restful/Sharp/NewOrder";//正式區

            string TatungApi = System.Configuration.ConfigurationManager.AppSettings["TatungApi"].ToString();

            if (TatungApi == "")
                result = "Tatung Api is null at webconfig";
            


            string AdateDt = "";
            string POdateDt = "";
            AdateDt = Convert.ToDateTime(adate).ToString("yyyy-MM-dd HH:mm:ss");

            if (buydate != "")
            {
                POdateDt = Convert.ToDateTime(buydate).ToString("yyyy-MM-dd HH:mm:ss");
            }



            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"ACCOUNT\": \"SHARP\",");
            sb.Append("\"PASSWORD\": \"" + password + "\",");
            sb.Append("\"CENTERCODE\":\"" + centrecode + "\",");
            sb.Append("\"ADATE\":\"" + AdateDt + "\",");
            sb.Append("\"SCODE\": \"" + scode + "\",");
            sb.Append("\"CUSNAME\":\"" + cusname + "\",");
            sb.Append("\"CUSTEL1\":\"" + custele1 + "\",");
            sb.Append("\"CUSADDR\":\"" + cusaddr + "\",");
            sb.Append("\"PRODNOC\":\"" + prodnoc + "\",");
            sb.Append("\"MODEL\":\"" + model + "\",");
            sb.Append("\"MANFNO\":\"" + manfno + "\",");
            sb.Append("\"BADDESC\":\"" + baddesc + "\",");
            sb.Append("\"BUYDATE\":\"" + POdateDt + "\",");
            sb.Append("\"MEMO\":\"" + memo + "\",");
            sb.Append("\"APP2\":\"" + app2 + "\",");
            sb.Append("\"STATION\":\"" + station + "\"");
            sb.Append("}");


            //swsd.SendOrder(sb.ToString());


            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(TatungApi);
                req.Method = "POST";
                req.ContentType = "application/json";
                #region 添加Post 参数

                byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                #endregion
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                result = ex.ToString();

            }
            FileDeal.logText("centrecode:" + centrecode + ";adate:" + AdateDt + ";scode:" + scode + ";cusname:" + cusname + ";custele1:" + custele1 + ";cusaddr:" + cusaddr
          + ";prodnoc:" + prodnoc + ";model:" + model + ";manfno:" + manfno + ";baddesc:" + baddesc
          + ";buydate:" + POdateDt + ";memo:" + memo + ";app2:" + app2 + ";station:" + station + ";result:" + result, "SendInfo-TransferToTatung");
            return result;
        }
    }
}
