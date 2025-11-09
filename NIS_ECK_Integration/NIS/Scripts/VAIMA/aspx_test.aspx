
<!--#include file="xmlfunction.inc"-->
<!--#include file="FileFunction.inc"-->

<%

on error resume next

Dim UserID = request("userid")
'response.write(UserID & "<br>")

Dim moduleID = request("moduleID")
response.write(moduleID & "<br>")

Dim PageName = request("PageName")
'response.write(PageName & "<br>")

Dim PatientName = request("PatientName")
'response.write(PatientName & "<br>")

'區別開啟與查詢功能
Dim Func = request("func")
'response.write(Func & "<br>")

'病人參數
Dim Fee_no = request("fee_no")
'response.write(Fee_no & "<br>")

Dim ptno = request("ptno")
'response.write(ptno & "<br>")

'查詢日期參數
Dim SDate = request("SDate")
'response.write(SDate & "<br>")

Dim EDate = request("EDate")
'response.write(EDate & "<br>")

'查詢時間參數
Dim STime = request("STime")
'response.write(STime & "<br>")

Dim ETime = request("ETime")
'response.write(ETime & "<br>")

'功能
Dim KeyWord = request("KeyWord")
'response.write(KeyWord & "<br>")

'正確產生xml檔案
Dim Check_XML = ""




Dim thisYear = Year(Now())
Dim thisMonth = Month(Now())
Dim thisDay = Day(Now())




Dim PackageName = Func

'分析使用率
'-------------------------------------------

Dim AnalysisFileName = "Analysis.xml"
Dim AnalysisFileName1 = "Analysis" & thisYear & thisMonth & thisDay & ".xml"


'Add by Ray 2020/7/31    紀錄有多少人
'----------------------------
Dim AnalysisFileName2 = "AnalysisMain.xml"
Dim objdom4 = Server.CreateObject("Microsoft.XMLDOM")
objdom4.load(server.mappath("xml/" & AnalysisFileName2))
Dim objRoot4 = objdom4.documentElement
Dim objNode4
Dim vIndex4 = 0
if objRoot4 is nothing then
	vIndex4 = 1
	objdom4.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot4 = objdom4.documentElement


else
	objNode4 = objRoot4.selectNodes("VOICE")
	vIndex4 = objNode4.length+1

	Dim objNode41 = objRoot4.selectSingleNode("VOICE[@UserID='" & UserID & "']")

	if objNode41 is nothing then

		objNode41 = XML_addElement(objdom4,objRoot4,"VOICE","") '新增VOICE物件到VAIMA底下
		objNode41.setAttribute("UserID",UserID)
		'objdom4.save(server.mappath("xml/" & AnalysisFileName2))
	
	end if
	

end if




'Add by Ray 2020/7/31  每個人存一個檔
'----------------------------
Dim AnalysisFileName3 = "Analysis-" & userid & ".xml"

Dim objdom5 = Server.CreateObject("Microsoft.XMLDOM")
objdom5.load(server.mappath("xml/" & AnalysisFileName3))
Dim objRoot5 = objdom5.documentElement
Dim objNode5
Dim vIndex5 = 0
'找不到XML自行新增XML的物件

if objRoot5 is nothing then
	vIndex5 = 1
	objdom5.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot5 = objdom5.documentElement


else
	objNode5 = objRoot5.selectNodes("VOICE")
	vIndex5 = objNode5.length+1

end if

Dim objNode51 = objRoot5.selectSingleNode("VOICE[@UserID='" & userid & "' and @PackageName='" & PackageName & "' and @thisYear='" & thisYear & "' and @thisMonth='" & thisMonth & "']")

if objNode51 is nothing then

	objNode51 = XML_addElement(objdom5,objRoot5,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode51.setAttribute("NO",vIndex5)
	objNode51.setAttribute("thisYear",thisYear)
	objNode51.setAttribute("thisMonth",thisMonth)
	objNode51.setAttribute("vCount",1)


else

	objNode51.setAttribute("vCount",objNode51.getAttribute("vCount")+1)
end if
	
	
	objNode51.setAttribute("UserID",userid)
	objNode51.setAttribute("PackageName",PackageName)

	objdom5.save(server.mappath("xml/" & AnalysisFileName3))



'----------------------------




Dim objdom3 = Server.CreateObject("Microsoft.XMLDOM")
objdom3.load(server.mappath("xml/" & AnalysisFileName))
Dim objRoot3 = objdom3.documentElement
Dim objNode3
Dim vIndex3 = 0
'找不到XML自行新增XML的物件
Dim checkNull = 0	'檢查是否讀取失敗.

if objRoot3 is nothing then
	vIndex3 = 1
	objdom3.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot3 = objdom3.documentElement
	checkNull = 1

else
	objNode3 = objRoot3.selectNodes("VOICE[@UserID='" & userid & "' and @PackageName='" & PackageName & "']")
	vIndex3 = objNode3.length+1

end if

'Dim objNode31 = objRoot3.selectSingleNode("VOICE[@UserID='" & userid & "' and @PackageName='" & PackageName & "']")
Dim objNode31 = objRoot3.selectSingleNode("VOICE[@UserID='" & userid & "' and @PackageName='" & PackageName & "' and @thisYear='" & thisYear & "' and @thisMonth='" & thisMonth & "']")

if objNode31 is nothing then

	objNode31 = XML_addElement(objdom3,objRoot3,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode31.setAttribute("NO",vIndex3)
	objNode31.setAttribute("thisYear",thisYear)
	objNode31.setAttribute("thisMonth",thisMonth)
	objNode31.setAttribute("vCount",1)
else

	objNode31.setAttribute("vCount",objNode31.getAttribute("vCount")+1)
end if
	
	objNode31.setAttribute("UserID",userid)
	objNode31.setAttribute("PackageName",PackageName)

	if checkNull = 0 or CheckFileExist(server.mappath("xml/" & AnalysisFileName))=0 then
		objdom3.save(server.mappath("xml/" & AnalysisFileName))
		objdom3.save(server.mappath("xml/" & AnalysisFileName1))
	end if
'-------------------------------------------

















'取得voicecontrol_vvXML設定檔.
Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("voicecontrol_vv.xml"))
'response.write(replace(objdom1.xml,"<","&lgt;")) '轉換檔案到實體的路徑(server asp)
Dim objNode = objdom1.documentElement.selectNodes("func")

'辨別是func=page還是func=query
Dim Func_Check = 0

for i = 0 to objNode.length-1

    if (objNode(i).text = PageName) then
        if(objNode(i).getAttribute("type") = Func)
            '從PageName取得model_id
            moduleID = objNode(i).getAttribute("model_id")
            'response.write("抓到的moduleID = " & objNode(i).getAttribute("model_id") & "這是我要的ID<br>")
        else
            'response.write("抓到的moduleID = " & objNode(i).getAttribute("model_id") & "這不是我要的ID<br>")
        end if
    end if

next

'取得nis168_vv.xml設定檔.
'if Func = "delete" and UserID = "nis168" then
if Func = "delete" and UserID = userid then
	response.write("開始刪除nis168_vv的Func節點" & "<br>")
Dim objdom2 = Server.CreateObject("Microsoft.XMLDOM")
objdom2.loadXML("<?xml version='1.0'?><" & UserID & "></" & UserID & ">")
	response.write(objdom2.xml)
objdom2.save(server.mappath("xml/" & UserID & "_vv.xml"))
	response.write("已移除" & userid & "的Func節點" & "<br>")
end if

'Dim objdom2 = Server.CreateObject("Microsoft.XMLDOM")
'objdom2.load(server.mappath("nis168_vv.xml"))
'Dim objNode2 = objdom2.documentElement.selectNodes("func")

'先判斷是開啟還是查詢 peter 2019/05/17
Dim a = "我是字串"
if a = "我是字串" then
    'response.write(a)
else
    'response.write("我不是字串")
end if

'<> 不等於 ""() 空直
'if moduleID  <> "" and Fee_no <> "" then
if moduleID  <> "" then

    'response.write("有進到moduleID的判斷式裡面")
    'response.write("moduleID = " & moduleID)
	'準備產生XMLload參數去read command
	'呼叫server程式去刪除
	Dim objdom = Server.CreateObject("Microsoft.XMLDOM")
	'--------------------------
	'objdom.async=false
	objdom.loadXML("<?xml version='1.0'?><voicecontrol_vv><func type='tag' model_id='" & moduleID & "' fee_no='" & Fee_no & "' SDate='" & SDate & "' EDate='" & EDate & "' STime='" & STime & "' ETime='" & ETime & "' KeyWord='" & KeyWord & "' PageName='" & PageName & "' Func='" & Func & "' ptno='" & ptno & "'></func></voicecontrol_vv>")

    '產生XML檔案
	'response.write(objdom.xml)

	objdom.save(server.mappath("xml/" & UserID & "_vv.xml"))

    Check_XML = "Check_XML = true"
    response.write("<br>" & Check_XML)

else

    Check_XML = "Check_XML = false"
    response.write("<br>" & Check_XML)

end if



'Dim xml,objNode,objAtr,nCntChd,nCntAtr
'Set xml=Server.CreateObject("Microsoft.XMLDOM")
'xml.Async=False
'xml.Load(Server.MapPath("test.xml"))
'Set objNode=xml.documentElement
'nCntChd=objNode.ChildNodes.length-1


%>