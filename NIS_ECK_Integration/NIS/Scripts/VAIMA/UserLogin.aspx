
<!--#include file="xmlfunction.inc"-->

<%


'公司名稱
Dim CompanyID = request("CompanyID")
'Response.write("CompanyID:" & CompanyID)

'登入帳號
Dim UserID = request("UserID")
'Response.write("UserID:" & UserID)

'密碼
Dim PWD = request("PWD")
'Response.write("PWD:" & PWD)




'
'-------------------------------------------
Dim LogFileName = "Users.xml"


'把檔案叫出來

Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("xml/" & LogFileName))
'response.write(objdom1.xml) 	'轉換檔案到實體的路徑(server asp)
Dim objRoot = objdom1.documentElement
Dim objNode = objRoot.selectSingleNode("User[@UserID='" & UserID & "' and @PWD='" & PWD & "' and @CompanyID= '" & CompanyID & "' ]")
'response.write("User[@UserID='" & UserID & "' and @PWD='" & PWD & "']")
if not objNode is nothing then
	Dim objdom2 = Server.CreateObject("Microsoft.XMLDOM")
 	objdom2.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	Dim objRoot2 = objdom2.documentElement

	Dim objNode2 = XML_addElement(objdom2,objRoot2,"User","") '新增VOICE物件到VAIMA底下
	
	objNode2.setAttribute("CompanyID",CompanyID)		'
	objNode2.setAttribute("UserID",UserID)
	objNode2.setAttribute("UserName",objNode.getAttribute("UserName"))

	response.write(objdom2.xml)
else
	response.write("公司代碼帳號或密碼錯誤!")
end if



%>