using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Com.Mayaminer;
using System.Data;
using System.Web.Mvc;
using NIS.Controllers;

namespace NIS.Models
{
    /// <summary>
    /// 家系圖資料物件
    /// </summary>
    public class FamilyTreeDataObj
    {
 
        public string chr_no { set; get; }
        public string family_id { set; get; }
        public string family_relation { set; get; }
        public string family_rel_desc { set; get; }
        public string family_age { set; get; }
        public string family_name { set; get; }
        public string family_gender { set; get; }
        public string family_alive { set; get; }
        public int level { set; get; }
    }

    public class FamilyTree : DrawTool
    {
        private BaseController baseC = new BaseController();
        private DBConnector link;
        public List<FamilyTreeDataObj> fto;
        private Size ObjSize = new Size(25, 25);
        private bool[] levelFlag = new bool[3]; 

        /// <summary> 建構式 </summary>
        public FamilyTree(int weight = 640, int height = 480) : base(weight, height)
        {
            this.link = new DBConnector();
            this.fto = new List<FamilyTreeDataObj>();
        }

        #region Family Tree Tool

        /// <summary> 讀取家系圖資料 </summary>
        public void loadFamilyTreeObj(string chr_no)
        {
            try
            {
                string sqlstr = string.Empty;
                sqlstr = " SELECT * FROM NIS_DATA_FAMILY_TREE ";
                sqlstr += " WHERE CHR_NO = '" + chr_no + "' ";
                sqlstr += " ORDER BY TO_NUMBER(REPLACE(FAMILY_RELATION,'S','0')) ASC,TO_NUMBER(FAMILY_AGE) DESC ";
                
                DataTable Dt = link.DBExecSQL(sqlstr);
                if (Dt.Rows.Count > 0)
                {
                    for (int d = 0; d < Dt.Rows.Count; d++)
                    {
                        this.fto.Add(new FamilyTreeDataObj()
                        {
                            chr_no = Dt.Rows[d]["chr_no"].ToString().Trim(),
                            family_id = Dt.Rows[d]["family_id"].ToString().Trim(),
                            family_relation = Dt.Rows[d]["family_relation"].ToString().Trim(),
                            family_name = Dt.Rows[d]["family_name"].ToString().Trim(),
                            family_gender = Dt.Rows[d]["family_gender"].ToString().Trim(),
                            family_age = Dt.Rows[d]["family_age"].ToString().Trim(),
                            family_alive = Dt.Rows[d]["family_alive"].ToString().Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                //寫入 write_logMsg(controllerName, loginID, actionName, errorMsg)     
                string tmp_controller = baseC.ControllerContext.RouteData.Values["controller"].ToString();
                string tmp_action = baseC.ControllerContext.RouteData.Values["action"].ToString();
                write_logMsg(tmp_controller, baseC.userinfo.EmployeesNo, tmp_action, ex.ToString(),ex);
            }
        }


        #region 寫ErrorLog (write_logMsg)
        //寫入 write_logMsg
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg, string err_folder = "DBExecSQL", string strSQL = "", Exception ex = null)
        {
            string tmp_msg = "", tmpfolder = "";
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
        public static void write_logMsg(string controllerName, string loginID, string actionName, string err_msg)
        {
            string tmp_msg = "", tmpfolder = "DBExecSQL", strSQL = "", err_folder = "";
            if (err_folder != "")
                tmpfolder = err_folder;
            else
                tmpfolder = "DBExecSQL";
            tmp_msg = "loginID: " + loginID + ",\tcontrollerName: " + controllerName + "\t,ActionName: " + actionName + "\n";
            LogTool log = new LogTool();
            if (strSQL != "")
            {
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
                log.saveLogMsg(tmp_msg + ", SQL= \t" + strSQL, tmpfolder);
            }
            else
                log.saveLogMsg(tmp_msg + ", err_msg= \t" + err_msg, tmpfolder);
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

        /// <summary> 設定圖片資訊 </summary>
        private void setPicInfo()
        {
            this.levelFlag[0] = false;
            this.levelFlag[1] = false;
            this.levelFlag[2] = false;

            for (int i = 0; i <= this.fto.Count - 1; i++)
            {
                switch (this.fto[i].family_relation)
                {
                    case "1":
                    case "2":
                        this.levelFlag[0] = true;
                        this.fto[i].level = 1;
                        break;
                    case "S":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                        this.levelFlag[1] = true;
                        this.fto[i].level = 2;
                        break;
                    case "9":
                    case "10":
                        this.levelFlag[2] = true;
                        this.fto[i].level = 3;
                        break;
                }
            }
        }

        // 取得關係ID
        private List<string> findRelation(string rel_id)
        {

            List<string> idList = new List<string>();
            for (int i = 0; i < this.fto.Count - 1; i++)
            {
                if (this.fto[i].family_relation == rel_id)
                {
                    idList.Add(this.fto[i].family_id);
                }
            }
            return idList;
        }

        /// <summary> 開始畫圖 </summary>
        public void drawFamily()
        {
            try
            {
                // 設定圖片資訊
                this.setPicInfo();

                // 取得高度間隔
                int blockHeight = (int)Math.Round((double)this.B.Height / (double)(this.levelFlag.Where(c => c.Equals(true)).Count() + 1));

                // 取得寬度間隔
                int pointX = 0, pointY = 0;
                int blockWidth = 0;
                int preLevelCount = 0;

                //跑三個LEVEL
                for (int i = 1; i <= 3; i++)
                {
                    List<FamilyTreeDataObj> levelObj = this.getLevelObj(i);
                    int levelCount = levelObj.Count;
                    this.setBrush("#000000");
                    this.setPen("#000000");

                    //計算行高
                    pointY = blockHeight * i;
                    blockWidth = (int)Math.Round((double)this.B.Width / ((double)levelCount + 1));

                    switch (i)
                    {
                        case 1:
                            for (int j = 0; j <= levelObj.Count - 1; j++)
                            {
                                pointX = blockWidth * (j + 1);
                                Point cpoint = new Point(pointX - (ObjSize.Width / 2), pointY - (ObjSize.Height / 2));
                                if (levelObj[j].family_gender == "M")
                                {
                                    this.drawRecent(cpoint, this.ObjSize, levelObj[j].family_relation.Equals("S"));
                                }
                                else
                                {
                                    this.drawCircle(cpoint, this.ObjSize, levelObj[j].family_relation.Equals("S"));
                                }
                                if (levelObj[j].family_alive == "N")
                                    setDie(cpoint);
                                cpoint.X -= ObjSize.Width + 3;
                                cpoint.Y += ObjSize.Height;
                                this.drawText(levelObj[j].family_name + "(" + levelObj[j].family_age.ToString() + ")", cpoint, "#0000FF");
                            }
                            pointX = blockWidth;
                            this.drawLine(new Point(pointX + (ObjSize.Width / 2) + 1, pointY), new Point(pointX + blockWidth - (ObjSize.Width / 2), pointY));
                            int x = ((pointX + (ObjSize.Width / 2) + 1) + (pointX + blockWidth - (ObjSize.Width / 2))) / 2;
                            this.drawLine(new Point(x, pointY), new Point(x, pointY + (blockHeight / 2)));
                            preLevelCount = levelCount;
                            break;
                        case 2:
                            if (preLevelCount >= 2)
                                blockWidth = (int)Math.Round((double)this.B.Width / ((double)levelCount + 1) / (preLevelCount / 2));
                            else
                                blockWidth = (int)Math.Round((double)this.B.Width / ((double)levelCount + 1));
                            for (int j = 0; j <= levelObj.Count - 1; j++)
                            {
                                pointX = blockWidth * (j + 1);
                                Point cpoint = new Point(pointX - (ObjSize.Width / 2), pointY - (ObjSize.Height / 2));
                                if (levelObj[j].family_gender == "M")
                                {
                                    this.drawRecent(cpoint, this.ObjSize, levelObj[j].family_relation.Equals("S"));
                                }
                                else
                                {
                                    this.drawCircle(cpoint, this.ObjSize, levelObj[j].family_relation.Equals("S"));
                                }
                                if (levelObj[j].family_alive == "N")
                                    setDie(cpoint);
                                if (this.levelFlag[0] == true && levelObj[j].family_relation != "4")
                                    this.drawLine(new Point(cpoint.X + (this.ObjSize.Width / 2), cpoint.Y - (blockHeight / 2) + (this.ObjSize.Height / 2)), new Point(cpoint.X + (this.ObjSize.Width / 2), cpoint.Y));

                                cpoint.X -= ObjSize.Width + 3;
                                cpoint.Y += ObjSize.Height;
                                this.drawText(levelObj[j].family_name + "(" + levelObj[j].family_age.ToString() + ")", cpoint, "#0000FF");
                            }

                            // 如果有爸爸才會畫這條線
                            if (this.levelFlag[0] == true)
                            {
                                int wifeCt = 0;
                                try
                                {
                                    wifeCt = levelObj.Where(c => c.family_relation.Equals("4")).Count();
                                }
                                catch
                                {
                                    wifeCt = 0;
                                }
                                if ((levelObj.Count - wifeCt) >= 2)
                                {
                                    this.drawLine(new Point(blockWidth, pointY - (blockHeight / 2)), new Point(blockWidth * levelCount, pointY - (blockHeight / 2)));
                                }
                                else
                                {
                                    this.drawLine(new Point(blockWidth, pointY - (blockHeight / 2)), new Point(blockWidth + (blockWidth / 2), pointY - (blockHeight / 2)));
                                }

                            }
                            pointX = blockWidth;
                            // 夫妻線
                            if (levelObj.Find(c => c.family_relation.Equals("4")) != null)
                            {
                                this.drawLine(new Point(pointX + (ObjSize.Width / 2) + 1, pointY), new Point(pointX + blockWidth - (ObjSize.Width / 2), pointY));
                            }
                            // 如果有兒子才有這條線
                            if (this.levelFlag[2] == true)
                            {
                                int x2 = ((pointX + (ObjSize.Width / 2) + 1) + (pointX + blockWidth - (ObjSize.Width / 2))) / 2;
                                this.drawLine(new Point(x2, pointY), new Point(x2, pointY + (blockHeight / 2)));
                            }
                            preLevelCount = levelCount;
                            break;
                        case 3:

                            if (preLevelCount >= 2)
                                blockWidth = (int)Math.Round((double)this.B.Width / ((double)levelCount + 1) / (double)((double)preLevelCount / 2));
                            else
                                blockWidth = (int)Math.Round((double)this.B.Width / ((double)levelCount + 1));

                            for (int j = 0; j <= levelObj.Count - 1; j++)
                            {
                                pointX = blockWidth * (j + 1);
                                Point cpoint = new Point(pointX - (ObjSize.Width / 2), pointY - (ObjSize.Height / 2));
                                if (levelObj[j].family_gender == "M")
                                {
                                    this.drawRecent(cpoint, this.ObjSize, levelObj[j].family_relation.Equals("S"));
                                }
                                else
                                {
                                    this.drawCircle(cpoint, this.ObjSize, levelObj[j].family_relation.Equals("S"));
                                }
                                if (levelObj[j].family_alive == "N")
                                    setDie(cpoint);
                                if (this.levelFlag[0] == true && levelObj[j].family_relation != "4")
                                    this.drawLine(new Point(cpoint.X + (this.ObjSize.Width / 2), cpoint.Y - (blockHeight / 2) + (this.ObjSize.Height / 2)), new Point(cpoint.X + (this.ObjSize.Width / 2), cpoint.Y));

                                cpoint.X -= ObjSize.Width + 3;
                                cpoint.Y += ObjSize.Height;
                                this.drawText(levelObj[j].family_name + "(" + levelObj[j].family_age.ToString() + ")", cpoint, "#0000FF");
                            }
                            // 如果有爸爸才會畫這條線
                            if (this.levelFlag[1] == true)
                                this.drawLine(new Point(blockWidth, pointY - (blockHeight / 2)), new Point(blockWidth * levelCount, pointY - (blockHeight / 2)));
                            break;
                    }

                }
            }
            catch (Exception ex) {

                this.drawText(ex.Message, new Point( 0, 0), "#000000");
                string[] stStr = ex.StackTrace.ToString().Split('\n');
                for (int i = 0; i <= stStr.Length - 1;i++ )
                {
                    int lineY = (i + 1) * 14;
                    this.drawText(stStr[i], new Point(0, lineY), "#000000");
                }
            }
        }

        private void setDie(Point loc) 
        {
            this.setPen("#FF0000");
            this.drawLine(loc, new Point(loc.X + this.ObjSize.Width, loc.Y + this.ObjSize.Height));
            this.drawLine(new Point(loc.X, loc.Y + this.ObjSize.Height), new Point(loc.X + this.ObjSize.Width, loc.Y));
            this.setPen("#000000");
        }

        /// <summary>
        /// 返回相對應LEVEL的物件
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private List<FamilyTreeDataObj> getLevelObj(int level)
        {
            List<FamilyTreeDataObj> retObj = new List<FamilyTreeDataObj>(); 
            for (int i = 0; i <= this.fto.Count - 1;i++ )
            {
                if (this.fto[i].level == level) {
                    retObj.Add(this.fto[i]);
                }
            }
            return retObj;
        }

        #endregion

    }
}