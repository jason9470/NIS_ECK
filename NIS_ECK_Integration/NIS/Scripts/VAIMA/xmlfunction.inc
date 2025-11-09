<script language="VB" runat="Server">

function loadXML(XMLDom)
	'把一個XML字串Load成一個XMLDOM物件
	
	Dim objdom
	objdom = CreateObject("Microsoft.XMLDOM")
	objdom.async=false
	objdom.load(XMLDom)
	loadXML = objdom
	
end function

function loadXMLDom(DOMString)
	'把一個XML字串Load成一個XMLDOM物件
	
	Dim objdom
	objdom = CreateObject("Microsoft.XMLDOM")
	objdom.async=false
	objdom.loadXML(DOMString)
	loadXMLDom = objdom
	
end function

function XML_addElement(xmlobj,nodeobj,nodeName,nodeValue)
	'新增一個節點
	on error resume next

	Dim childobj
	Dim textnode
	childobj = xmlobj.createElement(nodeName)
	textnode = xmlobj.createTextNode(nodeValue)
	childobj.appendChild(textnode)
	nodeobj.appendChild(childobj)
	XML_addElement = childobj
	childobj = nothing
	textnode = nothing

End function


function XML_PostServiceViaXmlHttp(URL,PostValue)
	'XMLHTTP 函式
	on error resume next
	Dim xmlHttpB
	xmlHttpB = Server.CreateObject("Microsoft.XMLHTTP")
	call xmlHttpB.open("POST", URL, false)
	call xmlHttpB.setRequestHeader("Content-Type", "application/x-www-form-urlencoded;charset:BIG5")
	call xmlHttpB.send(PostValue)
	XML_PostServiceViaXmlHttp = xmlHttpB.responseText
	'response.write(xmlHttpB.responseText)
end function




function reMoveChildNode(parentobj,childobj)	
	'移除子節點	
	on error resume next
	
	Call parentobj.removeChild(childobj)
	reMoveChildNode = parentobj
	
End function

</script>