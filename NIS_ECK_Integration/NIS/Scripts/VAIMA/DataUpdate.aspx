
<!--#include file="xmlfunction.inc"-->

<%


'
Dim NO = request("NO")
Response.write("NO:" & NO & "<br>")

Dim Desc = request("Desc")
Response.write("Desc:" & Desc & "<br>")

'登入帳號
Dim DescType = request("DescType")
'Response.write("DescType:" & DescType & "<br>")

Dim ErrDesc = request("ErrDesc")
Response.write("ErrDesc:" & ErrDesc & "<br>")


Dim FieldName = request("FieldName")
Response.write("FieldName:" & FieldName & "<br>")

Dim FieldValue = request("FieldValue")
Response.write("FieldValue:" & FieldValue & "<br>")

Dim FileName = request("FileName")


'-------------------------------------------
Dim LogFileName = "Corpus.xml"
Dim vYear = Year(Now()) & ""
Dim vMonth = Month(Now()) & ""
if vMonth.length=1 then vMonth = "0" & vMonth

Dim vDay = Day(Now()) & ""
if vDay.length=1 then vDay = "0" & vDay

Dim vHour = Hour(Now()) & ""
if vHour.length=1 then vHour = "0" & vHour

Dim LogFileName2 = "Corpus" & vYear & "" & vMonth & "" & vDay & vHour & ".xml"


if FileName <> "" then LogFileName = FileName & ".xml"
if FileName <> "" then LogFileName2 = FileName & vYear & "" & vMonth & "" & vDay & vHour & ".xml"



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

Dim objNode = objRoot.selectSingleNode("Data[@NO='" & NO & "']")
if not objNode is nothing then
	objNode.setAttribute(FieldName,FieldValue)
	objdom1.save(server.mappath("xml/" & LogFileName))
	objdom1.save(server.mappath("xml/" & LogFileName2))
end if


%>

