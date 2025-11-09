<%

Dim UserID = request("userid")
Dim Commandline = request("commandline")

If UserID = "nis168" Then
Dim i="PPAP"
If i = "PPAP2" Then
response.write("我想要PPAP...! 字串長度是:" & len(i) & "<br>")
Else
response.write("我什麼都不想要...! 字串長度是:" & len(i) & "<br>")
End If

'
Dim oXML2, oXMLError2, ReturnValue2, y, objNode2
oXML2 = Server.CreateObject("MSXML2.DOMDocument")
oXML2.async = false
oXML2.setProperty("ServerHTTPRequest", true)
ReturnValue2 = oXML2.load(Server.MapPath("voicecontrol_vv-2.xml"))
objNode2 = oXML2.getElementsByTagName("Module_ID")
For y=objNode2.length-1 To 0 Step -1
Response.Write(objNode2.item(y).Text & " <br>")
next
'

Dim oXML, oXMLError, ReturnValue, x, objNode
oXML = Server.CreateObject("MSXML2.DOMDocument")
oXML.async = false
oXml.setProperty("ServerHTTPRequest", true)
ReturnValue = oXML.load(Server.MapPath("b.xml"))
objNode = oXML.getElementsByTagName("ProductName")
For x=objNode.length-1 To 0 Step -1
Response.Write(objNode.item(x).Text & " ")
next

objNode = oXML.selectNodes("Product/ProductCode")
For each x in objNode
Response.Write(x.Text & " ")
next
oXML = Nothing

Else
End If

Dim moduleID = "M19"
Dim objdom = Server.CreateObject("Microsoft.XMLDOM")
'objdom.async=false
objdom.loadXML("<?xml version='1.0'?><voicecontrol_vv><func type='tag' model_id='" & moduleID & "'></func></voicecontrol_vv>")

response.write(objdom.xml)


%>