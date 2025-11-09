
<!--#include file="xmlfunction.inc"-->
<script>

function ChangeValue(thisobj){
	var thisValue = window.opener.VoiceThisInputObj.value;

	window.opener.VoiceThisInputObj.value = thisValue + thisobj.innerHTML ;
	window.close();
}
</script>

<%
Dim userid = request("userid")
Dim fee_no = request("fee_no")
Dim PackageName = request("PackageName")
Dim CheckFeeNOneed = 0

if PackageName = "PhraseSS" then CheckFeeNOneed = 1
if PackageName = "PhraseS" then CheckFeeNOneed = 1
if PackageName = "PhraseO" then CheckFeeNOneed = 1
if PackageName = "PhraseI" then CheckFeeNOneed = 1
if PackageName = "PhraseE" then CheckFeeNOneed = 1
if PackageName = "PhraseG" then CheckFeeNOneed = 1


Dim LogFileName = "NISVoiceCommand-" & PackageName & ".xml"
'response.write(LogFileName)
'把檔案叫出來
Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("xml/" & LogFileName))
'response.write(objdom1.xml) 				'轉換檔案到實體的路徑(server asp)
Dim objRoot = objdom1.documentElement
Dim objNode
Dim i
%>

<iframe id="f1" style="display:none;">
</iframe>

<table border=1 cellspacing='0' cellspadding='0'>
<tr bgcolor="lightblue">
<td>
	語音內容
</td>
<%if PackageName = "SpecialHandOver" then %>
<td>
	音檔
</td>
<%end if%>
<td>
	時間
</td>
<td>
	分數
</td>
</tr>




<%
Dim URL = ""
Dim Score
Dim j
if not objRoot is nothing then
	if CheckFeeNOneed = 1 then
		objNode = objRoot.selectNodes("VOICE[@userid='" & userid & "']")
	else
		objNode = objRoot.selectNodes("VOICE[@userid='" & userid & "' && @fee_no='" & fee_no & "']")
	end if
	'response.write(objNode.length)
	for i = objNode.length-1 to 0 step -1
		response.write("<tr>" & vbnewline)
		response.write("<td>" & vbnewline)
			response.write("<span onclick='ChangeValue(this);' style='cursor:hand'>" & objNode(i).getAttribute("STT") & "</span>" & vbnewline)
		response.write("</td>" & vbnewline)




		if PackageName = "SpecialHandOver" then 
		response.write("<td width='150'>" & vbnewline)
			URL = objNode(i).getAttribute("URL")

			'<embed src='your.mid' autostart=false>


			'if URL <> "" then response.write("<a href='" & URL & "' target='new" & i & "'>" & vbnewline)
			if URL <> "" then response.write("<embed id='new" & i & "' src='" & URL & "' autostart='false' height='50'>" & vbnewline)
			
			if URL <> "" then response.write("</a>" & vbnewline)
		response.write("</td>" & vbnewline)
		end if


		response.write("<td width='150'>" & vbnewline)
			response.write("" & objNode(i).getAttribute("SaveTime") & "" & vbnewline)
		response.write("</td>" & vbnewline)
		response.write("<td width='150'>" & vbnewline)
			Score = objNode(i).getAttribute("Score")
			
			if isDBNull(Score) then Score=""
			if Score = "" then Score=0

			response.write("<select name='' onchange=""UpdateValue('" & PackageName & "','" & objNode(i).getAttribute("NO") & "',this)"">" & vbnewline)
			response.write("<option value=''></option>" & vbnewline)
			for j = 10 to 1 step -1
				response.write("<option value='" & j & "'")
				if j = Score then response.write(" selected")
				response.write(">" & j & "</option>" & vbnewline)
			next
			response.write("</select>" & vbnewline)
			'response.write("" & objNode(i).getAttribute("Score") & "" & vbnewline)
		response.write("</td>" & vbnewline)
		response.write("</tr>" & vbnewline)
		
	next

end if
%>
</table>
<br><br>
<input type="button" value="關閉視窗" onclick="window.close();">

<script>
function UpdateValue(PackageName,vIndex,thisobj){
	var Score = thisobj.options[thisobj.selectedIndex].text;
	//alert(Score);
	document.all("f1").src="UpdateLongVoiceText.aspx?PackageName=" + PackageName + "&vIndex=" + vIndex + "&Score=" + Score;
}
</script>





