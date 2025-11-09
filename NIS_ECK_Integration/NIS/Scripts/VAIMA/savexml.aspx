

<%
'取值
'--------------------------

Dim UserID = request("userid")
response.write(UserID & "<br>")

Dim moduleID = request("moduleID")
response.write(moduleID & "<br>")

Dim PageName = request("PageName")
response.write(PageName & "<br>")



'取得XML設定檔.
Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("voicecontrol.xml"))
'response.write(objdom1.xml)
Dim objNode = objdom1.documentElement.selectNodes("func")
for i = 0 to objNode.length-1

	if objNode(i).text = PageName then
		'從PageName取得model_id
		moduleID = objNode(i).getAttribute("model_id")
	end if

next



if moduleID  <> "" then

	'準備產生XML
	'--------------------------
	Dim objdom = Server.CreateObject("Microsoft.XMLDOM")
	'objdom.async=false
	objdom.loadXML("<?xml version='1.0'?><voicecontrol_vv><func type='tag' model_id='" & moduleID & "'></func></voicecontrol_vv>")

	response.write(objdom.xml)

	objdom.save(server.mappath("" & UserID & ".xml"))


end if






%>
sddd


