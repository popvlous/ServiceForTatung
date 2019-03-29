using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace ServiceForTatung
{
    class FileDeal
    {
        public static void logText(string Info, string filename)
        {
            try
            {
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString();

                string _path = System.Web.HttpContext.Current.Request.PhysicalApplicationPath + "\\Log\\" + year + "-" + month + "\\";

                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }

                string path = _path + DateTime.Now.ToString("yy-MM-dd") + "_" + filename + ".log";
                FileInfo f = new FileInfo(path);
                FileStream fs;
                if (f.Exists)
                {   //此文件已存在
                    fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write);
                }
                else
                {   //每天新建一個文件
                    fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
                }

                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

                sw.WriteLine(System.DateTime.Now.ToString() + " : " + Info);

                sw.Flush();
                sw.Close();
                fs.Close();
            }
            catch
            { }
        }
    }
}