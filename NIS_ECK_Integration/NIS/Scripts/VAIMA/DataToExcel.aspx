<%
	Response.Charset = "UTF-8"
	Response.AddHeader("content-disposition", "attachment;filename=aa.xls")
	Response.contentType = "application/vnd.ms-excel" 
%> 


<!--#include file="xmlfunction.inc"-->
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<%


Dim LogFileName = "Corpus.xml"

Dim FileName = request("FileName")
if FileName <> "" then LogFileName = FileName & ".xml"



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
Dim i

Dim objNode = objRoot.selectNodes("Data")


Response.write("<table border=1 cellpadding=0 cellspacing=0>")
Response.write("<tr>")
Response.write("<td bgcolor='lightblue' width='400'>")
Response.write("關鍵字")
Response.write("</td>")
Response.write("<td bgcolor='lightblue' width='200'>")
Response.write("原錯字")
Response.write("</td>")
Response.write("<td bgcolor='lightblue' width='200'>")
Response.write("分類")
Response.write("</td>")
Response.write("</tr>")


Dim vStr
Dim vErr
for i = 0 to objNode.length-1

	vStr = objNode(i).getAttribute("Desc")
	vErr = objNode(i).getAttribute("ErrDesc")
	
	Response.write("<tr>")
	Response.write("<td>")
	Response.write(vStr)
	Response.write("</td>")
	Response.write("<td>")
	Response.write(vErr)
	Response.write("</td>")
	Response.write("<td>")
	Response.write(objNode(i).getAttribute("DescType"))
	Response.write("</td>")
	Response.write("</tr>")


next
Response.write("</table>")
%>

