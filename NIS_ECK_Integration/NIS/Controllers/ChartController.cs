using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Data;
using System.Drawing;
using NIS.Models;
using Com.Mayaminer;
using System.Data.OleDb;

namespace NIS.Controllers
{
    public class ChartController : Controller
    {

        private BaseController baseC = new BaseController();
        private DBConnector link = new DBConnector();
        public FileResult Tpr_Chart(string starttime, string endtime, string feeno, string indate, string cont,string str_HISVIEW="")
        {
            byte[] bytes = null;
            using (
            System.IO.MemoryStream ms = CreatTprImage(starttime.Substring(0, starttime.IndexOf("_")), endtime.Substring(0, endtime.IndexOf("_")), feeno, indate, cont)
            ){
                bytes = ms.ToArray();
            }
            return File(bytes, @"image/gif");
        }

        //宣告 Bitmap & Graphics
        public System.IO.MemoryStream CreatTprImage(string starttime, string endtime, string feeno, string indate, string cont, string str_HISVIEW = "")
        {
            //宣告一天幾格
            int OneDayCount = 6;

            System.Drawing.Bitmap image = new System.Drawing.Bitmap(850, 600);
            Graphics g = Graphics.FromImage(image);
            //宣告 正方形(最外層)
            Rectangle rect = new Rectangle(0, 0, 850, 600);
            g.DrawRectangle(new Pen(Color.Silver), rect);
            //清除背景_已白色取代
            g.Clear(Color.White);
            SetXAxis(ref g, OneDayCount);
            SetYAxis(ref g, true, 5, 8);
            DrawBody(ref g, starttime, endtime, feeno, indate, cont, OneDayCount, str_HISVIEW);
            //存入記憶體
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms;
        }

        #region Graphics Function

        private void DrawFillRectangle(ref Graphics objGraphics, Pen aPen, Rectangle rect)
        {
            objGraphics.DrawRectangle(aPen, rect);
            objGraphics.FillRectangle(new SolidBrush(Color.Black), rect);
        }

        private void DrawFillTriangle(ref Graphics objGraphics, Pen aPen, float sideLength, PointF startPoint, string part, string vs_item)
        {
            float val = (float)Math.Pow(sideLength, 2.0) - (float)Math.Pow((sideLength / 4), 2.0);

            float height = (float)Math.Sqrt(val);

            PointF p1 = startPoint,
                   p2 = new PointF(p1.X - (sideLength / 2), p1.Y + height),
                   p3 = new PointF(p1.X + (sideLength / 2), p1.Y + height);

            PointF[] trianglePoints = { p1, p2, p3 };

            objGraphics.DrawPolygon(aPen, trianglePoints);
            if (vs_item == "bp")
                objGraphics.FillPolygon(new SolidBrush(aPen.Color), trianglePoints);
            else if (part != "心尖脈")
                objGraphics.FillPolygon(new SolidBrush(Color.Red), trianglePoints);
        }

        private void DrawFillInvertedTriangle(ref Graphics objGraphics, Pen aPen, float sideLength, PointF startPoint, string part, string vs_item)
        {
            float val = (float)Math.Pow(sideLength, 2.0) - (float)Math.Pow((sideLength / 4), 2.0);

            float height = (float)Math.Sqrt(val);

            startPoint.Y = startPoint.Y + height;

            PointF p1 = startPoint,
                   p2 = new PointF(p1.X - (sideLength / 2), p1.Y - height),
                   p3 = new PointF(p1.X + (sideLength / 2), p1.Y - height);

            PointF[] trianglePoints = { p1, p2, p3 };

            objGraphics.DrawPolygon(aPen, trianglePoints);
            if (vs_item == "bp")
                objGraphics.FillPolygon(new SolidBrush(aPen.Color), trianglePoints);
            else if (part != "心尖脈")
                objGraphics.FillPolygon(new SolidBrush(Color.Red), trianglePoints);
        }

        private void DrawFillCircle(ref Graphics objGraphics, Pen aPen, PointF centerPoint, float radius, float x, float y, string part, string type)
        {
            if (part == "腋溫")
            {
                objGraphics.DrawLine(aPen, centerPoint.X, centerPoint.Y, centerPoint.X + (x / 3), centerPoint.Y + y);
                objGraphics.DrawLine(aPen, centerPoint.X + (x / 3), centerPoint.Y, centerPoint.X, centerPoint.Y + y);
            }
            else
            {
                objGraphics.DrawEllipse(aPen, centerPoint.X, centerPoint.Y, radius, radius);
                if (part != "肛溫" && type == "")
                    objGraphics.FillEllipse(aPen.Brush, centerPoint.X, centerPoint.Y, radius, radius);
            }
        }

        private void DrawFillCircle2(ref Graphics objGraphics, Pen aPen, PointF centerPoint, float radius, float x, float y, string part, string type)
        {
            aPen.Color = Color.Blue;
            if (part == "腋溫(停用)")
            {
                //centerPoint.X = centerPoint.X + 5.0f;
                //centerPoint.Y = centerPoint.Y + 3.0f;
                objGraphics.DrawLine(aPen, centerPoint.X - (x * 60), centerPoint.Y - (y / 2), centerPoint.X + (x * 60), centerPoint.Y + (y / 2));
                objGraphics.DrawLine(aPen, centerPoint.X - (x * 60), centerPoint.Y + (y / 2), centerPoint.X + (x * 60), centerPoint.Y - (y / 2));
            }
            else
            {
                if (type == "Y" || part.IndexOf("心尖脈|") != -1)
                    aPen.Color = Color.Red;
                objGraphics.DrawEllipse(aPen, centerPoint.X, centerPoint.Y, radius, radius);
                if (part != "肛溫" && type != "Y" && part.IndexOf("心尖脈|") == -1)
                    objGraphics.FillEllipse(aPen.Brush, centerPoint.X, centerPoint.Y, radius, radius);
            }
        }

        /// <summary>
        /// 劃出X軸刻度
        /// </summary>
        private void SetXAxis(ref Graphics objGraphics, int OneDayCount)
        {
            float x1 = 0f, y1 = 0f;
            float xSlice = 850 / (OneDayCount * 5f); //X軸刻度寬度
            Pen new_pen = new Pen(new SolidBrush(Color.Gainsboro), 1);
            new_pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            Pen black_pen = new Pen(new SolidBrush(Color.Black), 1);
            black_pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            for (int i = 0; i < OneDayCount * 5; i++)
            {
                if ((i + 1) % OneDayCount == 0 && i > 0)
                    objGraphics.DrawLine(black_pen, x1 + xSlice, y1, x1 + xSlice, y1 + 600);
                else
                    objGraphics.DrawLine(new_pen, x1 + xSlice, y1, x1 + xSlice, y1 + 600);
                x1 += xSlice;
            }
        }

        /// <summary>
        /// 劃出Y軸刻度
        /// </summary>
        /// <param name="Solid">是否要實線</param>
        /// <param name="Interval">實現間格刻度</param>
        /// <param name="Num">幾條實線</param>
        private void SetYAxis(ref Graphics objGraphics, bool Solid, int Interval, int Num)
        {
            float x1 = 0.0f, y1 = 0.0f;
            float ySlice = 600 / 45.225f; //Y軸刻度寬度
            Pen new_pen_dash = new Pen(new SolidBrush(Color.Gainsboro), 1);
            new_pen_dash.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            Pen new_pen_line = new Pen(new SolidBrush(Color.Black), 1);
            new_pen_line.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            for (int i = 1; i < 45; i++)
            {
                PointF drawPoint = new PointF(x1, y1 + ySlice - 5);
                if (Solid)
                {
                    if (i >= (45 - Num * Interval) && (i % Interval == 0))
                        objGraphics.DrawLine(new_pen_line, x1, y1 + ySlice, 850, y1 + ySlice);
                    else
                        objGraphics.DrawLine(new_pen_dash, x1, y1 + ySlice, 850, y1 + ySlice);
                }
                else
                    objGraphics.DrawLine(new_pen_dash, x1, y1 + ySlice, 850, y1 + ySlice);
                y1 += ySlice;
            }
        }

        #endregion

        #region other_function
        /// <summary>
        /// 換算溫度
        /// </summary>
        protected float temperature(float temp)
        {
            float key = 0;
            key = (temp <= 34) ? 0 : (temp - 34) * 5;
            return key;
        }

        /// <summary>
        /// 換算心跳
        /// </summary>
        protected float pluse(float plu)
        {
            float key = 0;
            key = (plu <= 20) ? 0 : (plu / 2 - 10) / 2;
            return key;
        }

        /// <summary>
        /// 換算呼吸
        /// </summary>
        protected float Tran_breath(float breath)
        {
            float key = 0;
            key = (breath <= 0) ? 0 : (breath / 2);
            return key;
        }

        protected bool check_date(string time, string checktime)
        {
            DateTime CheckTime = Convert.ToDateTime(checktime), Time = Convert.ToDateTime(time);
            if (CheckTime >= Time.AddHours(-2) && CheckTime < Time.AddHours(2))
                return true;
            return false;
        }

        public DataTable sel_assess_data(string feeno, string natype)
        {
            DataTable dt = new DataTable();
            try
            {
                //DBConnector link = new DBConnector();                
                string sql = "SELECT * FROM ASSESSMENTDETAIL WHERE TABLEID = ( ";
                sql += "SELECT TABLEID FROM (SELECT * FROM ASSESSMENTMASTER WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "' ";
                if (natype != "")
                    sql += "AND NATYPE = '" + natype + "' ";
                sql += "AND STATUS IN('insert','update') AND NATYPE <> 'Z' ORDER BY CREATETIME DESC) WHERE rownum <= 1)";

                link.DBExecSQL(sql, ref dt);
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(), ex);
            }
            finally
            {
                this.link.DBClose();
            }

            return dt;
        }

        /// <summary>
        /// 取得評估項目的值
        /// </summary>
        /// <param name="id">欲搜尋ID</param>
        public string sel_data(DataTable dt, string id)
        {
            string value = "";
            foreach (DataRow r in dt.Rows)
            {
                if (r["ITEMID"].ToString() == id)
                    value = r["ITEMVALUE"].ToString();
            }
            return value;
        }

        public string get_sepcial_event(string feeno, string typeid, string colname, string starttime, string endtime)
        {
            string time = "";
            try
            {
                //DBConnector link = new DBConnector();
                string sql = "SELECT * FROM (";
                sql += "SELECT " + colname + " FROM NIS_SPECIAL_EVENT_DATA WHERE 0 = 0 ";
                if (feeno != "")
                    sql += "AND FEENO = '" + feeno + "'";
                if (typeid != "")
                    sql += "AND TYPE_ID = '" + typeid + "'";
                if (starttime != "")
                {
                    sql += "AND CREATTIME BETWEEN to_date('" + Convert.ToDateTime(starttime).AddMinutes(-1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                    sql += "AND to_date('" + Convert.ToDateTime(endtime).AddMinutes(1).ToString("yyyy/MM/dd HH:mm:ss") + "','yyyy/mm/dd hh24:mi:ss') ";
                }
                sql += "ORDER BY CREATTIME DESC ) WHERE rownum <= 1";

                DataTable Dt = link.DBExecSQL(sql);
                if (Dt.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        time = Dt.Rows[i][0].ToString().Trim();
                    }
                }

                return time;
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
                return time;
            }
            finally
            {
                this.link.DBClose();
            }
        }
        
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\t行號: #" + GetLineNumber(ex).ToString() + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
        }

        //回傳error行號
        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }
        #endregion

        private void DrawBody(ref Graphics objGraphics, string starttime, string endtime, string feeno, string indate, string StartCount, int OneDayCount, string str_HISVIEW = "")
        {
            #region 宣告劃點參數
            //宣告畫筆
            Pen myPen_temp = new Pen(Color.Blue, 2), myPen_plu = new Pen(Color.Red, 2), myPen_breath = new Pen(Color.Black, 2), myPen_sbp = new Pen(Color.Orange, 2), myPen_dbp = new Pen(Color.Olive, 2);
            //宣告畫圖中心點
            PointF srcPoint = new PointF(0f, 0f), destPoint = new PointF(0f, 0f);
            //宣告間隔
            float m_XSlice = 850 / 30f, m_YSlice = 600 / 45.225f;
            //宣告座標
            float x_temp = new float(), x_plu = new float(), x_breath = new float(), x_sbp = new float(), x_dbp = new float(), last_x_temp = -1f, last_x_plu = -1f, last_x_breath = -1f, last_x_sbp = -1f, last_x_dbp = -1f;
            float y_temp = new float(), y_plu = new float(), y_breath = new float(), y_sbp = new float(), y_dbp = new float(), last_y_temp = -1f, last_y_plu = -1f, last_y_breath = -1f, last_y_sbp = -1f, last_y_dbp = -1f;
            //取得住院日期
            DataTable dt_assess = sel_assess_data(feeno, "");
            string temp_date = sel_data(dt_assess, "param_tube_date") + " " + sel_data(dt_assess, "param_tube_time");
            //如果有填入院護理評估 將Webserivce的入院日期替換掉
            DateTime InDate = (temp_date.Trim() == "") ? Convert.ToDateTime(indate.Replace("_", " ")) : Convert.ToDateTime(temp_date);
            //宣告需寫入項目
            DataTable dt_mark = new DataTable();
            dt_mark.Columns.Add("X");
            dt_mark.Columns.Add("Y");
            dt_mark.Columns.Add("CONTENT");

            string content_height = "";
            string content_low = "";
            //取得特殊註記
            /*
            string Trans_time = get_sepcial_event(feeno, "1", "CREATTIME", starttime, endtime);
            string Diliverde_time = get_sepcial_event(feeno, "2", "CREATTIME", starttime, endtime);
            string Trans_room = get_sepcial_event(feeno, "1", "CONTENT", starttime, endtime);
            string Send_time = get_sepcial_event(feeno, "0", "CREATTIME", starttime, endtime);
            string Dischage_time = get_sepcial_event(feeno, "5", "to_char(CREATTIME,'YYYY/MM/DD hh24:mi:ss') || '|' || CONTENT", starttime, endtime);
            */
            #endregion

            //設定寫字_字型
            Font fontChinese = new Font("新細明體", 8f);
            SolidBrush drawBrush = new SolidBrush(Color.Red);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat(StringFormatFlags.DirectionVertical);

            if (Session["TPR"] != null)
            {
                if ((string)Session["TPR"] == "HISVIEW")
                {
                    str_HISVIEW = "HISVIEW";
                }
            }

            //取得畫圖資料
            VitalSign vs_m = new VitalSign();
            List<VitalSignDataList> dt_temp = vs_m.sel_vital(feeno, Convert.ToDateTime(starttime), Convert.ToDateTime(endtime), "bt,mp,bf", str_HISVIEW);
            List<VitalSignDataList> dt = new List<VitalSignDataList>();
            foreach (var item in dt_temp)
            {
                if (item.DataList.Exists(x => x.vs_item == "bt" && x.vs_record != "")
                    || item.DataList.Exists(x => x.vs_item == "mp" && x.vs_record != "")
                    || item.DataList.Exists(x => x.vs_item == "bf" && x.vs_record != ""))
                {
                    dt.Add(item);
                }
            }
            VitalSignData Temp = null;
            DateTime S = Convert.ToDateTime(starttime);
            List<int> ShowNumInOneDay = new List<int>();
            for (DateTime s = S; s <= Convert.ToDateTime(endtime); s = s.AddDays(1))
            {
                int cont = dt.FindAll(x => Convert.ToDateTime(x.recordtime).ToString("yyyy/MM/dd") == s.ToString("yyyy/MM/dd")).Count;
                if (cont > 0)
                {
                    for (int i = cont; i > 0; i = (i - OneDayCount))
                    {
                        if (i >= OneDayCount)
                            ShowNumInOneDay.Add(OneDayCount);
                        else
                            ShowNumInOneDay.Add(i);
                    }
                }
                else
                {
                    ShowNumInOneDay.Add(0);
                }
            }
            try
            {
                int Flag = 0;
                if (int.Parse(StartCount) > 0)
                {
                    for (int i = 0; i < int.Parse(StartCount); i++)
                    {
                        Flag += ShowNumInOneDay[i];
                    }
                }
                for (int i = int.Parse(StartCount), LineCount = 0, n = 0; i < int.Parse(StartCount) + 5; i++, LineCount++, n = (LineCount * OneDayCount) + 0)
                {
                    if (i < ShowNumInOneDay.Count)
                    {
                        for (int j = 0; j < ShowNumInOneDay[i]; j++, Flag++, n++)
                        {
                            content_height = "";
                            content_low = "";
                            if (dt[Flag].DataList[0].vs_record.ToString().IndexOf('#') > -1)
                                dt[Flag].DataList[0].vs_record = dt[Flag].DataList[0].vs_record.ToString().Trim().Substring(1, dt[Flag].DataList[0].vs_record.ToString().Trim().Length - 1);

                            #region 畫點_畫線
                            //劃體溫的點
                            if (dt[Flag].DataList.Exists(x => x.vs_item == "bt"))
                            {
                                Temp = dt[Flag].DataList.Find(x => x.vs_item == "bt");
                                myPen_temp.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                                myPen_temp.Color = Color.Blue;
                                if (Temp.vs_record.ToString() != "")
                                {
                                    float temp = float.Parse(Temp.vs_record.ToString());
                                    if (Temp.vs_type != "Y")
                                    {
                                        if (temp > 0)
                                        {
                                            x_temp = (n * m_XSlice) + (m_XSlice / 2);
                                            y_temp = (45 - temperature(temp)) * m_YSlice;
                                        }
                                        if (last_x_temp >= 0)
                                        {
                                            srcPoint = new PointF(last_x_temp, last_y_temp);
                                            destPoint = new PointF(x_temp, y_temp);
                                            objGraphics.DrawLine(myPen_temp, srcPoint, destPoint);
                                        }
                                        if (temp > 0)
                                        {
                                            PointF centerPoint = new PointF(x_temp - 2.0f, y_temp - 4.0f);
                                            DrawFillCircle2(ref objGraphics, myPen_temp, centerPoint, 5.0f, m_XSlice, m_YSlice, Temp.vs_part, Temp.vs_type);
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("HH"), fontChinese, drawBrush, new PointF(x_temp, y_temp - 26.0f));
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("mm"), fontChinese, drawBrush, new PointF(x_temp, y_temp - 16.0f));
                                        }
                                        last_x_temp = x_temp;
                                        last_y_temp = y_temp;
                                    }
                                    else
                                    {
                                        if (temp > 0)
                                        {
                                            if (last_x_temp > 0)//如果未來需要將虛點移至格子中間，可將這個if判斷拿掉
                                            {
                                                x_temp = (n * m_XSlice) + (m_XSlice / 2);
                                            }
                                            else
                                            {
                                                if (j != 0)//處置點為第一筆時(異常點未包含在時間區間)，不帶入last_x_temp(預設為-1)
                                                {
                                                    last_x_temp = x_temp;
                                                }
                                            }

                                            y_temp = (45 - temperature(temp)) * m_YSlice;
                                        }
                                        if (last_x_temp > 0)
                                        {
                                            myPen_temp.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                            myPen_temp.Color = Color.Red;
                                            srcPoint = new PointF(last_x_temp - 2.0f, last_y_temp);
                                            destPoint = new PointF(x_temp - 2.0f, y_temp);
                                            objGraphics.DrawLine(myPen_temp, srcPoint, destPoint);
                                        }
                                        if (temp > 0)
                                        {
                                            PointF centerPoint = new PointF(x_temp - 2.0f, y_temp - 4.0f);
                                            DrawFillCircle2(ref objGraphics, myPen_temp, centerPoint, 5.0f, m_XSlice, m_YSlice, Temp.vs_part, Temp.vs_type);
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("HH"), fontChinese, drawBrush, new PointF(x_temp - 8.0f, y_temp - 26.0f));
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("mm"), fontChinese, drawBrush, new PointF(x_temp - 8.0f, y_temp - 16.0f));
                                        }
                                    }

                                    if (temperature(temp) >= 45)
                                    {
                                        if (Temp.vs_part != "" && Temp.vs_part == "肛溫")
                                            content_height += "體溫" + temp.ToString() + "℃ 肛" + " ";
                                        else if (Temp.vs_part != "" && Temp.vs_part == "腋溫")
                                            content_height += "體溫" + temp.ToString() + "℃ 腋" + " ";
                                        else
                                            content_height += "體溫" + temp.ToString() + "℃ ";
                                    }
                                    else if (temp > 0 && temperature(temp) <= 0)
                                    {
                                        if (Temp.vs_part != "" && Temp.vs_part == "肛溫")
                                            content_low += "體溫" + temp.ToString() + "℃ 肛" + " ";
                                        else if (Temp.vs_part != "" && Temp.vs_part == "腋溫")
                                            content_low += "體溫" + temp.ToString() + "℃ 腋" + " ";
                                        else
                                            content_low += "體溫" + temp.ToString() + "℃ ";
                                    }
                                    
                                }

                                if (Temp.vs_reason != "" && Temp.vs_reason != "無" && Temp.vs_reason != "無,無")
                                    content_height += Temp.vs_reason + " ";
                                if (Temp.vs_other_memo.Replace("|", "").Trim() != "" && Temp.vs_other_memo != "藥品")
                                {
                                    string tmp_memo = string.Empty;
                                    if (Temp.vs_other_memo.Split('|').GetValue(0).ToString() == "99")
                                    {
                                        for (int q = 1; q < Temp.vs_other_memo.Split('|').Length; q++)
                                        {
                                            tmp_memo += Temp.vs_other_memo.Split('|').GetValue(q).ToString();
                                        }
                                        tmp_memo = tmp_memo.Replace("-1", "").Replace("|", " ").Trim() + " ";
                                        content_height += tmp_memo;
                                    }
                                    else
                                    {
                                        content_height += Temp.vs_other_memo.Replace("-1", "").Replace("|", " ").Trim() + " ";
                                    }
                                }
                            }
                            //劃心跳的點
                            if (dt[Flag].DataList.Exists(x => x.vs_item == "mp"))
                            {
                                Temp = dt[Flag].DataList.Find(x => x.vs_item == "mp");
                                if (Temp.vs_record.ToString() != "")
                                {
                                    float plu = float.Parse(Temp.vs_record.ToString());
                                    if (plu > 0)
                                    {
                                        x_plu = (n * m_XSlice) + (m_XSlice / 2);
                                        y_plu = (45 - pluse(plu)) * m_YSlice;
                                    }
                                    if (last_x_plu > 0)
                                    {
                                        srcPoint = new PointF(last_x_plu, last_y_plu);
                                        destPoint = new PointF(x_plu, y_plu);
                                        objGraphics.DrawLine(myPen_plu, srcPoint, destPoint);
                                    }
                                    if (plu > 0)
                                    {
                                        if(Temp.vs_part.IndexOf("心尖脈|") != -1)
                                        {
                                            PointF centerPoint = new PointF(x_plu, y_plu - 5.0f);
                                            DrawFillCircle2(ref objGraphics, myPen_plu, centerPoint, 5.0f, m_XSlice, m_YSlice, Temp.vs_part, Temp.vs_type);
                                        }
                                        else
                                        {
                                            PointF triangleStartPoint = new PointF(x_plu, y_plu - 5.0f);
                                            DrawFillTriangle(ref objGraphics, myPen_plu, 8, triangleStartPoint, Temp.vs_part, Temp.vs_item);
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("HH"), fontChinese, drawBrush, new PointF(x_plu - 8.0f, y_plu - 26.0f));
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("mm"), fontChinese, drawBrush, new PointF(x_plu - 8.0f, y_plu - 16.0f));
                                        }
                                        
                                    }

                                    if (pluse(plu) >= 45)
                                    {
                                        if (Temp.vs_part != "" && Temp.vs_part == "心尖脈")
                                            content_height += "心跳" + plu.ToString() + "次/分 心尖 ";
                                        else
                                            content_height += "心跳" + plu.ToString() + "次/分 ";
                                    }
                                    else if (plu > 0 && pluse(plu) <= 0)
                                    {
                                        if (Temp.vs_part != "" && Temp.vs_part == "心尖脈")
                                            content_low += "心跳" + plu.ToString() + "次/分 心尖 ";
                                        else
                                            content_low += "心跳" + plu.ToString() + "次/分 ";
                                    }

                                    last_x_plu = x_plu;
                                    last_y_plu = y_plu;
                                }
                            }
                            //劃呼吸的點
                            if (dt[Flag].DataList.Exists(x => x.vs_item == "bf"))
                            {
                                Temp = dt[Flag].DataList.Find(x => x.vs_item == "bf");
                                if (Temp.vs_record.ToString() != "")
                                {
                                    float breath = float.Parse(Temp.vs_record.ToString());
                                    if (breath >= 0)
                                    {
                                        x_breath = (n * m_XSlice) + (m_XSlice / 2);
                                        y_breath = (45 - Tran_breath(breath)) * m_YSlice;
                                    }
                                    if (last_x_breath > 0)
                                    {
                                        srcPoint = new PointF(last_x_breath, last_y_breath);
                                        destPoint = new PointF(x_breath, y_breath);
                                        objGraphics.DrawLine(myPen_breath, srcPoint, destPoint);
                                    }
                                    if (breath >= 0)
                                    {
                                        Rectangle rect = new Rectangle((int)x_breath - 3, (int)y_breath - 4, 8, 8);
                                        DrawFillRectangle(ref objGraphics, new Pen(Color.Black, 2), rect);
                                        //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("HH"), fontChinese, drawBrush, new PointF(x_breath - 8.0f, y_breath - 26.0f));
                                        //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("mm"), fontChinese, drawBrush, new PointF(x_breath - 8.0f, y_breath - 16.0f));
                                    }

                                    if (Tran_breath(breath) >= 45)
                                        content_height += "呼吸" + breath.ToString() + "次/分 ";
                                    else if (breath > 0 && Tran_breath(breath) <= 0)
                                        content_low += "呼吸" + breath.ToString() + "次/分 ";

                                    last_x_breath = x_breath;
                                    last_y_breath = y_breath;
                                }
                            }
                            //畫血壓的點
                            if (dt[Flag].DataList.Exists(x => x.vs_item == "bp"))
                            {
                                Temp = dt[Flag].DataList.Find(x => x.vs_item == "bp");
                                if (Temp.vs_record.ToString() != "")
                                {
                                    if (Temp.vs_record.ToString().Split('|').GetValue(0).ToString() != "")
                                    {
                                        float sbp = float.Parse(Temp.vs_record.ToString().Split('|').GetValue(0).ToString());
                                        if (sbp > 0)
                                        {
                                            x_sbp = (n * m_XSlice) + (m_XSlice / 2);
                                            y_sbp = (45 - pluse(sbp)) * m_YSlice;
                                        }
                                        if (sbp > 0)
                                        {
                                            PointF triangleStartPoint = new PointF(x_sbp, y_sbp - 5.0f);
                                            DrawFillTriangle(ref objGraphics, myPen_sbp, 8, triangleStartPoint, Temp.vs_part, Temp.vs_item);
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("HH"), fontChinese, drawBrush, new PointF(x_sbp - 8.0f, y_sbp - 26.0f));
                                            //objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("mm"), fontChinese, drawBrush, new PointF(x_sbp - 8.0f, y_sbp - 16.0f));
                                        }

                                        if (pluse(sbp) >= 45)
                                            content_height += "收縮壓" + sbp.ToString() + "mmHg ";
                                        else if (sbp > 0 && pluse(sbp) <= 0)
                                            content_low += "收縮壓" + sbp.ToString() + "mmHg ";

                                        last_x_sbp = x_sbp;
                                        last_y_sbp = y_sbp;
                                    }

                                    if (Temp.vs_record.ToString().Split('|').GetValue(1).ToString() != "")
                                    {
                                        float dbp = float.Parse(Temp.vs_record.ToString().Split('|').GetValue(1).ToString());
                                        if (dbp > 0)
                                        {
                                            x_dbp = (n * m_XSlice) + (m_XSlice / 2);
                                            y_dbp = (45 - pluse(dbp)) * m_YSlice;
                                        }
                                        if (dbp > 0)
                                        {
                                            PointF triangleStartPoint = new PointF(x_dbp, y_dbp - 5.0f);
                                            DrawFillInvertedTriangle(ref objGraphics, myPen_dbp, 8, triangleStartPoint, Temp.vs_part, Temp.vs_item);
                                            objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("HH"), fontChinese, drawBrush, new PointF(x_dbp - 8.0f, y_dbp - 26.0f));
                                            objGraphics.DrawString(Convert.ToDateTime(Temp.create_date).ToString("mm"), fontChinese, drawBrush, new PointF(x_dbp - 8.0f, y_dbp - 16.0f));
                                        }

                                        if (pluse(dbp) >= 45)
                                            content_height += "舒張壓" + dbp.ToString() + "mmHg ";
                                        else if (dbp > 0 && pluse(dbp) <= 0)
                                            content_low += "舒張壓" + dbp.ToString() + "mmHg ";

                                        last_x_dbp = x_dbp;
                                        last_y_dbp = y_dbp;
                                    }
                                }
                            }
                            #endregion

                            //string xpoint = (cir_minute(Convert.ToDateTime(starttime), Convert.ToDateTime(dt[Flag].DataList[0].create_date)) * m_XSlice).ToString();
                            string xpoint = ((n * m_XSlice) + (m_XSlice / 2)).ToString();
                            if (content_height.Trim() != "")
                                set_mark(ref dt_mark, xpoint, "0", content_height);
                            else
                                content_height = "";
                            if (content_low.Trim() != "")
                                set_mark(ref dt_mark, xpoint, (m_YSlice * 43).ToString(), content_low);
                            else
                                content_low = "";
                        }
                    }
                }

                #region 寫字
                /** 20160523 更改為獨立呈現
                set_mark(ref dt_mark, (cir_minute(Convert.ToDateTime(starttime), Convert.ToDateTime(InDate)) * m_XSlice).ToString(), "0", "Admitted at " + InDate.ToString(" HH:mm") + " ");

                if (Trans_time != "")
                {
                    string content = "Transfer to " + Trans_room + " at " + Convert.ToDateTime(Trans_time).ToString("HH:mm") + " ";
                    set_mark(ref dt_mark, (cir_minute(Convert.ToDateTime(starttime), Convert.ToDateTime(Trans_time)) * m_XSlice).ToString(), "0", content);
                }

                if (Diliverde_time != "")
                {
                    string content = "Delivered at " + Convert.ToDateTime(Diliverde_time).ToString("HH:mm") + " ";
                    set_mark(ref dt_mark, (cir_minute(Convert.ToDateTime(starttime), Convert.ToDateTime(Diliverde_time)) * m_XSlice).ToString(), "0", content);
                }

                if (Send_time != "")
                {
                    string content = "Sent p't to OR at " + Convert.ToDateTime(Send_time).ToString("HH:mm") + " ";
                    set_mark(ref dt_mark, (cir_minute(Convert.ToDateTime(starttime), Convert.ToDateTime(Send_time)) * m_XSlice).ToString(), "0", content);
                }

                if (Dischage_time != "")
                {
                    string content = "";
                    string[] leave_content = Dischage_time.Split('|');
                    if (leave_content[1] == "6")
                    { content = "Expired at " + Convert.ToDateTime(leave_content[0]).ToString("HH:mm") + " "; }
                    else if (leave_content[1] == "3" || leave_content[1] == "4")
                    { content = "Discharge AMA at " + Convert.ToDateTime(leave_content[0]).ToString("HH:mm") + " "; }
                    else
                    { content = "Discharge at " + Convert.ToDateTime(leave_content[0]).ToString("HH:mm") + " "; }
                    set_mark(ref dt_mark, (cir_minute(Convert.ToDateTime(starttime), Convert.ToDateTime(leave_content[0])) * m_XSlice).ToString(), "0", content);
                }
                
                foreach (DataRow r in dt_mark.Rows)
                {
                    PointF stringPoint = new PointF(float.Parse(r["X"].ToString()), float.Parse(r["Y"].ToString()));
                    if (r["Y"].ToString() == "0")
                        objGraphics.DrawString(r["CONTENT"].ToString(), fontChinese, drawBrush, stringPoint, drawFormat);
                    else
                        objGraphics.DrawString(r["CONTENT"].ToString(), fontChinese, drawBrush, stringPoint);
                }
                **/
                #endregion
            }
            catch (Exception ex)
            {
                LogTool log = new LogTool();
                log.saveLogMsg(ex.Message.ToString() + "畫圖 " + feeno + "," + starttime + "," + endtime, "DBExecInsert");
            }
        }

        private string get_sql_by_interval(string feeno, DateTime starttime, DateTime endtime)
        {
            string sql = "SELECT * FROM DATA_VITALSIGN WHERE 0 = 0 ";
            if (feeno != "")
                sql += "AND FEE_NO = '" + feeno + "' ";
            if (starttime != null)
            {
                sql += "AND CREATE_DATE BETWEEN to_date('" + Convert.ToDateTime(starttime).ToString("yyyy/MM/dd HH:mm") + "','yyyy/MM/dd hh24:mi:ss') ";
                sql += "AND to_date('" + Convert.ToDateTime(endtime).ToString("yyyy/MM/dd HH:mm") + "','yyyy/MM/dd hh24:mi:ss') ";
            }
            sql += "AND VS_ITEM IN('bt','mp','bf') ORDER BY CREATE_DATE";

            return sql;
        }

        private int cir_minute(DateTime starttime, DateTime point)
        {
            int value = 0;
            starttime = Convert.ToDateTime(starttime.ToString("yyyy/MM/dd 00:00:0"));
            TimeSpan time = point - starttime;
            value = time.Days * 60 * 24 + time.Hours * 60 + time.Minutes;
            return value;
        }

        private void set_mark(ref DataTable dt_mark, string x, string y, string content)
        {
            bool mark = false;
            if (dt_mark.Rows.Count > 0)
            {
                for (int i = 0; i < dt_mark.Rows.Count; i++)
                {
                    if (dt_mark.Rows[i]["X"].ToString() == x && dt_mark.Rows[i]["Y"].ToString() == y)
                    {
                        dt_mark.Rows[i]["CONTENT"] = dt_mark.Rows[i]["CONTENT"].ToString() + content;
                        mark = false;
                    }
                    else
                        mark = true;
                }
            }
            else
                mark = true;
            if (mark)
            {
                DataRow dt_mark_new_r = dt_mark.NewRow();
                dt_mark_new_r["X"] = x;
                dt_mark_new_r["Y"] = y;
                dt_mark_new_r["CONTENT"] = content;
                dt_mark.Rows.Add(dt_mark_new_r);
                mark = false;
            }
        }

    }
}