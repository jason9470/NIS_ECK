
<!--#include file="xmlfunction.inc"-->
<!--#include file="FileFunction.inc"-->

<%
'on error resume next

Dim userid = request("userid")
Dim fee_no = request("fee_no")
Dim PackageName = request("PackageName")
Dim PatientName = request("PatientName")
Dim STT = request("STT")
Dim URL1 = request("URL1")
'response.write(request("URL1"))

Dim thisYear = Year(Now())
Dim thisMonth = Month(Now())
Dim thisDay = Day(Now())


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

end if


Dim objNode41 = objRoot4.selectSingleNode("VOICE[@UserID='" & userid & "']")

if objNode41 is nothing then

	objNode41 = XML_addElement(objdom4,objRoot4,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode41.setAttribute("UserID",userid)
	'objdom4.save(server.mappath("xml/" & AnalysisFileName2))

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
	objNode3 = objRoot3.selectNodes("VOICE")
	vIndex3 = objNode3.length+1

end if

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

'記錄所有Command Log檔
'-------------------------------------------
Dim LogFileName = "NISVoiceCommand-" & PackageName & ".xml"


'把檔案叫出來

Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("xml/" & LogFileName))
'response.write(objdom1.xml) 轉換檔案到實體的路徑(server asp)
Dim objRoot = objdom1.documentElement
Dim objNode
Dim vIndex = 0

checkNull = 0


'找不到XML自行新增XML的物件
if objRoot is nothing then
	vIndex = 1
	objdom1.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot = objdom1.documentElement
	checkNull = 1

else
	Dim objNode2 = objRoot.selectNodes("VOICE")
	vIndex = objNode2.length+1
	'response.write(vIndex)
end if
'response.write(vIndex)

	objNode = XML_addElement(objdom1,objRoot,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode.setAttribute("NO",vIndex)
	objNode.setAttribute("userid",userid)
	objNode.setAttribute("fee_no",fee_no)
	objNode.setAttribute("PackageName",PackageName)
	objNode.setAttribute("PatientName",PatientName)
	objNode.setAttribute("OriSTT",STT)
	objNode.setAttribute("Score","")

	STT = replaceStringA(STT)
	objNode.setAttribute("STT",STT)
	objNode.setAttribute("URL",URL1)
	objNode.setAttribute("SaveTime",Now.ToString("yyyy/MM/dd HH:mm:ss"))

	if checkNull = 0 then
		objdom1.save(server.mappath("xml/" & LogFileName))
	end if


'--------------------------------------------------------------

'記錄所有Command Log檔
'-------------------------------------------
LogFileName = "NISVoiceCommand-" & PackageName & "-" & userid & ".xml"
'response.write(LogFileName )

'把檔案叫出來

objdom1.load(server.mappath("xml/" & LogFileName))
'response.write(objdom1.xml) 轉換檔案到實體的路徑(server asp)
objRoot = objdom1.documentElement

vIndex = 0

checkNull = 0


'找不到XML自行新增XML的物件
if objRoot is nothing or objdom1 is nothing then
	vIndex = 1
	objdom1.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot = objdom1.documentElement
	checkNull = 1
'	response.write(1)
else
	Dim objNode2 = objRoot.selectNodes("VOICE")
	vIndex = objNode2.length+1
	'response.write(vIndex)
'	response.write(2)
end if
'response.write(vIndex)

	objNode = XML_addElement(objdom1,objRoot,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode.setAttribute("NO",vIndex)
	objNode.setAttribute("userid",userid)
	objNode.setAttribute("fee_no",fee_no)
	objNode.setAttribute("PackageName",PackageName)
	objNode.setAttribute("PatientName",PatientName)
	objNode.setAttribute("OriSTT",STT)
	objNode.setAttribute("Score","")

	STT = replaceStringA(STT)
	objNode.setAttribute("STT",STT)
	objNode.setAttribute("URL",URL1)
	objNode.setAttribute("SaveTime",Now.ToString("yyyy/MM/dd HH:mm:ss"))

	'if checkNull = 0 then
		objdom1.save(server.mappath("xml/" & LogFileName))
	'end if
	'response.write(3)

'--------------------------------------------------------------

%>

<script>
window.close();
</script>