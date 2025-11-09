
<!--#include file="xmlfunction.inc"-->

<%


'
Dim vIndex = request("vIndex")
Response.write("NO:" & vIndex & "<br>")


'
'-------------------------------------------
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
Dim objNode = objRoot.selectSingleNode("Data[@NO='" & vIndex & "']")

if not objNode is nothing then
	Call objRoot.removeChild(objNode)
end if
response.write(objdom1.xml)

objdom1.save(server.mappath("xml/" & LogFileName))

response.redirect("DataList.aspx")

%>

<script>
location.href="DataList.aspx";
</script>