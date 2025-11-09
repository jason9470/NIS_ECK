
<!--#include file="xmlfunction.inc"-->

<%


'
Dim Desc = request("Desc")
Response.write("Desc:" & Desc & "<br>")

'登入帳號
Dim DescType = request("DescType")
'Response.write("DescType:" & DescType & "<br>")

Dim ErrDesc = request("ErrDesc")
Response.write("ErrDesc:" & ErrDesc & "<br>")


'
'-------------------------------------------
Dim LogFileName = "CorpusHide.xml"
Dim vYear = Year(Now()) & ""
Dim vMonth = Month(Now()) & ""
if vMonth.length=1 then vMonth = "0" & vMonth

Dim vDay = Day(Now()) & ""
if vDay.length=1 then vDay = "0" & vDay

Dim vHour = Hour(Now()) & ""
if vHour.length=1 then vHour = "0" & vHour

Dim LogFileName2 = "CorpusHide" & vYear & "" & vMonth & "" & vDay & vHour & ".xml"





'把檔案叫出來

Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("xml/" & LogFileName))
'response.write(objdom1.xml) 	'轉換檔案到實體的路徑(server asp)
Dim objRoot = objdom1.documentElement


if objRoot is nothing then
	
 	objdom1.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot = objdom1.documentElement

end if

Dim objNode 	= objRoot.selectSingleNode("Data[@Desc='" & replace(Desc,"'","\'") & "']")
Dim objNode2 	= objRoot.selectNodes("Data")

Dim vIndex = 0

Dim i 
for i = 0 to objNode2.length-1
	if vIndex < objNode2(i).getAttribute("NO") then
		vIndex = objNode2(i).getAttribute("NO")
	end if
next

vIndex = vIndex+1

if objNode is nothing then
	objNode = XML_addElement(objdom1,objRoot,"Data","") '新增Desc物件到VAIMA底下
	objNode.setAttribute("NO",vIndex)
	objNode.setAttribute("Desc",Desc)		'
	objNode.setAttribute("DescLength",Len(Desc))	

	
else
	objNode.setAttribute("DescLength",Len(Desc))	


	'
end if
objdom1.save(server.mappath("xml/" & LogFileName))
objdom1.save(server.mappath("xml/" & LogFileName2))



%>

