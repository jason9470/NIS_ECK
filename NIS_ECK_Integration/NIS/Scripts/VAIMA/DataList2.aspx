<!--#include file="xmlfunction.inc"-->
<script type="text/javascript">
function movetotop(){
	window.document.body.scrollTop=0;
	window.document.documentElement.scrollTop=0;
}

function importData(){
	b = document.all("Desc").value.split("\n");
	TotalIndex = b.length;
	
	CurrentIndex = 10000;

	setTimeout('importDateItem();',1500);

}
function importDateItem(){
	if (CurrentIndex<TotalIndex){
		document.all("Desc1").value = b[CurrentIndex];
		CurrentIndex = CurrentIndex + 1;
		AddNewWord();
		document.all("ShowCurrentStep").innerHTML = "<font color='red'>" + CurrentIndex + "/" + TotalIndex + "</font>";
		setTimeout('importDateItem();',1500);
	}
	
}
var b ;
var TotalIndex = 0;
var CurrentIndex = 0;

</script>   
<span id="ShowCurrentStep">AAA</span>
<%
Dim Desc = request("Desc")
Dim DescType = request("DescType")

Dim LogFileName = "CorpusA.xml"
Dim LogFileName1 = "CorpusHideA.xml"


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
'---------------------------'---------------------------
Dim objdom2 = Server.CreateObject("Microsoft.XMLDOM")
'objdom2.async=false
objdom2.load(server.mappath("xml/" & LogFileName1))
'response.write(objdom2.xml) 	'轉換檔案到實體的路徑(server asp)
Dim objRoot2 = objdom2.documentElement
if objRoot2 is nothing then
	
 	objdom2.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot2 = objdom2.documentElement

end if

Dim objNode3 = objRoot2.selectNodes("Data")
Dim k 
Dim m = 0
Dim HideStr =""
Dim vLen = 0 


'---------------------------


Dim objNode 
Dim QueryStr = ""


if Desc <> "" then
	QueryStr = "@Desc='" & replace(Desc,"'","\'") & "'"
end if
if DescType <> "" then
	if QueryStr = "" then
		QueryStr = "@DescType='" & DescType & "'"
	else
		QueryStr = QueryStr & " and @DescType='" & DescType & "'"
	end if
	
end if
'response.write(QueryStr)

if QueryStr = "" then
	objNode = objRoot.selectNodes("Data")
	response.write(objNode.length)
else
	objNode = objRoot.selectNodes("Data[" & QueryStr & "]")
end if



%>


<table width="100%">
<tr>
<td width="50%" bgcolor="lightblue" valign="top">


	<form name="f1" method="post" action="DataList.aspx">

	<table>
	<tr>
	<td colspan="2" align="center">
		語料查詢
	</td>
	</tr>
	<tr>
	<td width="100">
		語料：
	</td>
	<td>
		<textarea name="Desc" cols="60" rows="7"><%=Desc%></textarea>
	</td>
	</tr>
	<tr>
	<td>
		分類：
	</td>
	<td>
		<select name="DescType">
			<option></option>
			<option value="科別" 		<%if request("DescType") = "科別" then response.write("selected")%>>科別</option>
			<option value="人名" 		<%if request("DescType") = "人名" then response.write("selected")%>>人名</option>
			<option value="特殊.一般" 	<%if request("DescType") = "特殊.一般" then response.write("selected")%>>特殊.一般</option>
			<option value="單位" 		<%if request("DescType") = "單位" then response.write("selected")%>>單位</option>
			<option value="檢查項目" 	<%if request("DescType") = "檢查項目" then response.write("selected")%>>檢查項目</option>
			<option value="藥品" 		<%if request("DescType") = "藥品" then response.write("selected")%>>藥品</option>
			<option value="部位" 		<%if request("DescType") = "部位" then response.write("selected")%>>部位</option>
			<option value="疾病" 		<%if request("DescType") = "疾病" then response.write("selected")%>>疾病</option>
			<option value="工具" 		<%if request("DescType") = "工具" then response.write("selected")%>>工具</option>
			<option value="醫療單位" 	<%if request("DescType") = "醫療單位" then response.write("selected")%>>醫療單位</option>
			<option value="病徵" 		<%if request("DescType") = "病徵" then response.write("selected")%>>病徵</option>
			<option value="職稱" 		<%if request("DescType") = "職稱" then response.write("selected")%>>職稱</option>
			<option value="基本英文" 		<%if request("DescType") = "基本英文" then response.write("selected")%>>基本英文</option>
		</select>

	</td>
	</tr>

<%
Dim i ,vStr
Dim vStr2
Dim j 

'----------------------------------
Dim MaxValue2=0

'找出最大數
Dim DescLength
for i = 0 to objNode3.length-1
	DescLength = objNode3(i).getAttribute("DescLength")
	if isDBNull(DescLength) then DescLength = 0

	if MaxValue2 < DescLength then
		MaxValue2 = objNode3(i).getAttribute("DescLength")
	end if
next


for j = MaxValue2 to 1 step -1
	
	objNode3 = objRoot2.selectNodes("Data[@DescLength='" & j & "']")

	for k = 0 to objNode3.length-1
		HideStr =""
		vLen = objNode3(k).getAttribute("DescLength")
		if isDBNull(vLen) then vLen = 1

		for m = 1 to vLen-1
			HideStr = HideStr & "-"
		next
		Desc = replace(Desc,objNode3(k).getAttribute("Desc"),HideStr)
		'Desc = replace(Desc,objNode3(k).getAttribute("Desc"),"<span title='" & replace(objNode3(k).getAttribute("Desc"),"'","\'") & "'>" & HideStr & "</span>")
	next


next




'----------------------------------------





'標示紅色的字
Dim objNode2 = objRoot.selectNodes("Data")

Dim MaxValue=0
for i = 0 to objNode2.length-1
	DescLength = objNode2(i).getAttribute("DescLength")
	if isDBNull(DescLength) then DescLength = 0


	if MaxValue < DescLength then
		MaxValue = objNode2(i).getAttribute("DescLength")
	end if
next





for j = MaxValue to 1 step -1
	'response.write(j & "<br>")
	objNode2 = objRoot.selectNodes("Data[@DescLength='" & j & "']")
	for i = 0 to objNode2.length-1
		vStr = objNode2(i).getAttribute("Desc")
		vStr2 = objNode2(i).getAttribute("ErrDesc")
		if isDBNull(vStr2) then vStr2 = ""
		Desc = replace(Desc,vStr,"<font color='red'>" & vStr & "</font>")

		if vStr2 <> "" then
			Desc = replace(Desc,vStr2,"<strike>" & vStr2 & "</strike>" & "<font color='blue'>" & vStr & "</font>")
		end if

	next

next

Desc = replace(Desc,vbNewLine,"<br>")

%>

	<tr>
	<td>
		
	</td>
	<td>
		<input type="submit" value="查詢">
	</td>
	</tr>
	
	<tr>
	<tr>
	<td>
		
	</td>
	<td>
		<input type="button" value="匯入" onclick="importData();">
	</td>
	</tr>
	
	<tr>
	<td>
	
	</td>
	<td>
		<%=Desc%>
	<br><br>
	<input type="button" onclick="movetotop();" id="gotop" value="移到最上方">
	</td>
	</tr>

	</table>

	</form>


</td>
<td width="50%" bgcolor="lightpink" valign="top">


	<table>
	<tr>
	<td colspan="2" align="center">
		語料新增
	</td>
	</tr>
	<tr>
	<td width="100">
		語料：
	</td>
	<td>
		<textarea name="Desc1" id="Desc1" cols="60" rows="7"></textarea>
	</td>
	</tr>

	<tr>
	<td width="100">
		錯誤原字：
	</td>
	<td>
		<textarea name="Desc2" id="Desc2" cols="60" rows="7"></textarea>
	</td>
	</tr>

	<tr>
	<td>
		分類：
	</td>
	<td>
		<select name="DescType1" id="DescType1">
			<option></option>
			<option value="科別" 		<%if request("DescType") = "科別" then response.write("selected")%>>科別</option>
			<option value="人名" 		<%if request("DescType") = "人名" then response.write("selected")%>>人名</option>
			<option value="特殊.一般" 	<%if request("DescType") = "特殊.一般" then response.write("selected")%>>特殊.一般</option>
			<option value="單位" 		<%if request("DescType") = "單位" then response.write("selected")%>>單位</option>
			<option value="檢查項目" 	<%if request("DescType") = "檢查項目" then response.write("selected")%>>檢查項目</option>
			<option value="藥品" 		<%if request("DescType") = "藥品" then response.write("selected")%>>藥品</option>
			<option value="部位" 		<%if request("DescType") = "部位" then response.write("selected")%>>部位</option>
			<option value="疾病" 		<%if request("DescType") = "疾病" then response.write("selected")%>>疾病</option>
			<option value="工具" 		<%if request("DescType") = "工具" then response.write("selected")%>>工具</option>
			<option value="醫療單位" 	<%if request("DescType") = "醫療單位" then response.write("selected")%>>醫療單位</option>
			<option value="病徵" 		<%if request("DescType") = "病徵" then response.write("selected")%>>病徵</option>
			<option value="職稱" 		<%if request("DescType") = "職稱" then response.write("selected")%>>職稱</option>
			<option value="基本英文" 	<%if request("DescType") = "基本英文" then response.write("selected")%>>基本英文</option>


		</select>
	</td>
	</tr>
	<tr>
	<td>
		
	</td>
	<td>
		<input type="button" value="新增" onclick="AddNewWord();">
	</td>
	</tr>
	<tr>
	<td colspan="2" align="center">
		<iframe name="iframe1" id="iframe1" style="display:none"></iframe>
	</td>
	</tr>

	</table>

	<hr>

	<table>
	<tr>
	<td>
		不要顯示的文字
	</td>
	<td>
		<textarea name="Desc3" id="Desc3" cols="60" rows="7"></textarea>
	</td>
	</tr>
	<tr>
	<td>
		
	</td>
	<td>
		<input type="button" value="新增" onclick="HideNewWord();">
	</td>
	</tr>


	</table>













</td>
</tr>
</table>
<script>
function AddNewWord(){
	//alert(document.all("Desc1").value);
	var e = document.getElementById("DescType1");
	var strUser = e.options[e.selectedIndex].value;
	//alert(strUser);
	//window.open("DataAdd.aspx?Desc=" + document.all("Desc1").value + "&DescType=" + strUser);
	document.all("iframe1").src="DataAdd.aspx?FileName=CorpusA&Desc=" + document.all("Desc1").value + "&ErrDesc=" + document.all("Desc2").value + "&DescType=" + strUser;

}

function HideNewWord(){
	
	document.all("iframe1").src="DataHide.aspx?Desc=" + document.all("Desc3").value;

}

function changeTypevalue(thisNO,thisobj){
	var TypeValue = thisobj.options[thisobj.selectedIndex].value;
	//alert(TypeValue);
	document.all("iframe1").src="DataUpdate.aspx?NO=" + thisNO + "&DescType=" + TypeValue;
}

function changeTypevalueA(thisNO,FieldName,FieldValue){
	alert(FieldName);
	alert(FieldValue);
	document.all("iframe1").src="DataUpdate.aspx?NO=" + thisNO + "&FieldName=" + FieldName + "&FieldValue=" + FieldValue;
}


</script>


<hr>
<input type="button" value="匯出Excel" onclick="window.open('DataToExcel.aspx?FileName=CorpusA','ABC')">
<br><br>
總共有 <font color='red'><%=objNode.length%></font> 筆
<table border=1 cellpadding=0 cellspacing=0>
<tr bgcolor="lightblue">
<td width='50' align="center">
	NO
</td>
<td width='400'>
	&nbsp;關鍵字
</td>
<td width='200'>
	&nbsp;原錯字
</td>
<td width='150' align="center">
	分類
</td>
<td width='70' align="center">
	刪除
</td>
</tr>

<%
for i = objNode.length-1 to 0 step -1

	vStr = objNode(i).getAttribute("Desc")
%>
	<tr>
	<td align="center">
		<%= objNode(i).getAttribute("NO")%>
	</td>
	<td>
		&nbsp;<%=vStr%>
	</td>
	<td>
		&nbsp;<%= objNode(i).getAttribute("ErrDesc")%>
	</td>
	<td align="center">
		<select name="DescType3" onchange="changeTypevalueA('<%= objNode(i).getAttribute("NO")%>','DescType',this.options[this.selectedIndex].value);">
			<option></option>
			<option value="科別" 		<%if objNode(i).getAttribute("DescType") = "科別" then response.write("selected")%>>科別</option>
			<option value="人名" 		<%if objNode(i).getAttribute("DescType") = "人名" then response.write("selected")%>>人名</option>
			<option value="特殊.一般" 	<%if objNode(i).getAttribute("DescType") = "特殊.一般" then response.write("selected")%>>特殊.一般</option>
			<option value="單位" 		<%if objNode(i).getAttribute("DescType") = "單位" then response.write("selected")%>>單位</option>
			<option value="檢查項目" 	<%if objNode(i).getAttribute("DescType") = "檢查項目" then response.write("selected")%>>檢查項目</option>
			<option value="藥品" 		<%if objNode(i).getAttribute("DescType") = "藥品" then response.write("selected")%>>藥品</option>
			<option value="部位" 		<%if objNode(i).getAttribute("DescType") = "部位" then response.write("selected")%>>部位</option>
			<option value="疾病" 		<%if objNode(i).getAttribute("DescType") = "疾病" then response.write("selected")%>>疾病</option>
			<option value="工具" 		<%if objNode(i).getAttribute("DescType") = "工具" then response.write("selected")%>>工具</option>
			<option value="醫療單位" 	<%if objNode(i).getAttribute("DescType") = "醫療單位" then response.write("selected")%>>醫療單位</option>
			<option value="病徵" 		<%if objNode(i).getAttribute("DescType") = "病徵" then response.write("selected")%>>病徵</option>
			<option value="職稱" 		<%if objNode(i).getAttribute("DescType") = "職稱" then response.write("selected")%>>職稱</option>
			<option value="基本英文" 		<%if objNode(i).getAttribute("DescType") = "基本英文" then response.write("selected")%>>基本英文</option>
		</select>

		<%'= objNode(i).getAttribute("DescType")%>
	</td>
	<td width='100' align="center">
		<a href="DataDelete.aspx?vIndex=<%= objNode(i).getAttribute("NO")%>">刪除</a>
	</td>
	</tr>

<%
next
%>
</table>
