
<!--#include file="xmlfunction.inc"-->
<!--#include file="FileFunction.inc"-->

<script>
function changeQS(thisobj){
	location.href="analysis.aspx?QS=" + thisobj.options[thisobj.selectedIndex].value ;
}

</script>

<%

'分析使用率
'-------------------------------------------
Dim AnalysisFileName = "Analysis.xml"
Dim objdom = Server.CreateObject("Microsoft.XMLDOM")
objdom.load(server.mappath("xml/" & AnalysisFileName))
Dim objRoot = objdom.documentElement
Dim objNode = objRoot.selectNodes("VOICE")

'功能名稱
'-------------------------------------------
Dim AnalysisFileName2 = "funcName.xml"
Dim objdom5 = Server.CreateObject("Microsoft.XMLDOM")
objdom5.load(server.mappath("" & AnalysisFileName2))


if objdom5.documentElement is nothing then 
	response.write("缺少funcName.xml設定資料!")
	response.end

end if

Dim objNode5 = objdom5.documentElement.selectNodes("VOICE")
Dim j 

'組護理師XML<==table使用..
'-------------------------------------------
Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
objdom1.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
Dim objRoot1 = objdom1.documentElement

Dim objdom3 = Server.CreateObject("Microsoft.XMLDOM")
objdom3.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
Dim objRoot3 = objdom3.documentElement


Dim i
Dim objNode1
Dim objNode3

Dim thisYear1
Dim thisMonth1

Dim thisYear2 = ""
Dim thisMonth2 = ""

Dim QS = request("QS")
Dim QSitem 

if QS <> "" then

	QSitem = split(QS,"@")
	thisYear2 = QSitem(0)
	thisMonth2 = QSitem(1)

end if

for i = 0 to objNode.length-1
	'找出所有護理師
	'--------------------------------------
	objNode1 = objRoot1.selectSingleNode("VOICE[@UserID='" & objNode(i).getAttribute("UserID") & "']")
	if objNode1 is nothing then
		objNode1 = XML_addElement(objdom1,objRoot1,"VOICE","") '新增VOICE物件到VAIMA底下
		objNode1.setAttribute("UserID",objNode(i).getAttribute("UserID"))
	end if

	thisYear1 = objNode(i).getAttribute("thisYear")
	thisMonth1 = objNode(i).getAttribute("thisMonth")
	if IsDBNull(thisYear1)  then thisYear1= ""
	if IsDBNull(thisMonth1) then thisMonth1 = ""

	'找出所有年月
	'--------------------------------------
	'objNode3 = objRoot3.selectSingleNode("VOICE[@thisYear='" & thisYear1 & "' and @thisMonth='" & thisMonth1 & "']")
	objNode3 = objRoot3.selectSingleNode("VOICE[@QueryString='" & thisYear1 & "年" & thisMonth1 & "月']")
	if objNode3 is nothing then
		objNode3 = XML_addElement(objdom3,objRoot3,"VOICE","") '新增VOICE物件到VAIMA底下


		objNode3.setAttribute("QueryString",thisYear1 & "年" & thisMonth1 & "月")
		objNode3.setAttribute("thisYear",thisYear1)
		objNode3.setAttribute("thisMonth",thisMonth1)

	end if




next
	'response.write(objdom1.xml)
	response.write(objdom3.xml)

'顯示年月下拉選單
'objNode3 = objRoot3.selectSingleNode("VOICE[@thisYear='" & thisYear1 & "' and @thisMonth='" & thisMonth1 & "']")
objNode3 = objRoot3.selectNodes("VOICE")

response.write("<select name='QueryString' onchange='changeQS(this);'>")
for i = 0 to objNode3.length-1
	response.write("<option value='" & objNode3(i).getAttribute("thisYear") & "@" & objNode3(i).getAttribute("thisMonth") & "'>" & objNode3(i).getAttribute("QueryString") & "</option>")
next
response.write("</select><br><Br>")




Dim objNode2 = objRoot1.selectNodes("VOICE")


response.write("<table border=1 cellspacing=0 cellspadding=0>" & vbnewline)
response.write("<tr align='center'>" & vbnewline)
response.write("<td width=150>" & vbnewline)
response.write("帳號" & vbnewline)
response.write("</td>" & vbnewline)

for j = 0 to objNode5.length-1

response.write("<td width=100>" & vbnewline)
response.write(objNode5(j).getAttribute("ShowName") & vbnewline)
response.write("</td>" & vbnewline)

next

response.write("<td width=100>" & vbnewline)
response.write("總次數" & vbnewline)
response.write("</td>" & vbnewline)

response.write("</tr>" & vbnewline)

Dim vCount2 = 0 
for i = 0 to objNode2.length-1

	
	response.write("<tr align='center'>" & vbnewline)
	response.write("<td>" & vbnewline)
	response.write(objNode2(i).getAttribute("UserID") & vbnewline)
	response.write("</td>" & vbnewline)

	vCount2 = 0 
	for j = 0 to objNode5.length-1
		'
		if thisYear2 = "" then
		'如果沒有條件，就顯示所有資料.
		'--------------------------------

		objNode1 = objRoot.selectSingleNode("VOICE[@UserID='" & objNode2(i).getAttribute("UserID") & "' and @PackageName='" & objNode5(j).getAttribute("PackageName") & "']")
		else

		objNode1 = objRoot.selectSingleNode("VOICE[@UserID='" & objNode2(i).getAttribute("UserID") & "' and @PackageName='" & objNode5(j).getAttribute("PackageName") & "' and @thisYear=" & thisYear2 & " and @thisMonth=" & thisMonth2 & "]")

		end if


		response.write("<td width=100>" & vbnewline)
		
		if objNode1 is nothing then
			response.write("" & vbnewline)
		else
			vCount2 = vCount2 + objNode1.getAttribute("vCount")
			response.write(objNode1.getAttribute("vCount") & vbnewline)
		end if
		response.write("</td>" & vbnewline)

	next

	response.write("<td>" & vbnewline)
	response.write(vCount2 & vbnewline)
	response.write("</td>" & vbnewline)

	response.write("</tr>" & vbnewline)

next
response.write("<table>" & vbnewline)


%>