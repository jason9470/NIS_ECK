<!--引用xmlfuction.inc-->
<!--#include file="xmlfunction.inc"-->

<%

Dim GUID = request("GUID")

'類別
Dim CommandType = request("CommandType")

'公司代碼
Dim CompanyID = request("CompanyID")

'登入帳號
Dim LoginName = request("LoginName")

'識別結果
Dim STT_text = request("STT_text")

'音檔位置
Dim FilePath = request("FilePath")

'語音辨識回饋結果
Dim Json = request("Json")


'Json回傳參數1
Dim Param1 = request("Param1")


'Json回傳參數2
Dim Param2 = request("Param2")


'Json回傳參數3
Dim Param3 = request("Param3")


'Json回傳參數4
Dim Param4 = request("Param4")


'Json回傳參數5
Dim Param5 = request("Param5")



'Json回傳參數6
Dim Param6 = request("Param6")

'Json回傳參數7
Dim Param7 = request("Param7")


'Json回傳參數8
Dim Param8 = request("Param8")


'Json回傳參數9
Dim Param9 = request("Param9")


'Json回傳參數10
Dim Param10 = request("Param10")




'Json回傳參數10
Dim CYB_Result = request("CYB_Result")


'Json回傳參數10
Dim CYB_PathScore = request("CYB_PathScore")
'response.write(Param10 & "<br>")

'Json回傳參數10
Dim CYB_NBestIdx = request("CYB_NBestIdx")


'Json回傳參數10
Dim CYB_Rule = request("CYB_Rule")


'Json回傳參數10
Dim CYB_Tag = request("CYB_Tag")


'Json回傳參數10
Dim action = request("action")



Dim ReturnValue = request("Return")


'記錄所有Command Log檔
'-------------------------------------------
Dim LogFileName = "VoiceCommandLog.xml"


'把檔案叫出來

Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("xml/" & LogFileName))
'response.write(objdom1.xml) 轉換檔案到實體的路徑(server asp)
Dim objRoot = objdom1.documentElement
Dim objNode
Dim vIndex = 0
'找不到XML自行新增XML的物件
if objRoot is nothing then
	vIndex = 1
	objdom1.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot = objdom1.documentElement

else
	Dim objNode2 = objRoot.selectNodes("VOICE")
	vIndex = objNode2.length+1
	'response.write(vIndex)
end if
'response.write(vIndex)

'<VOICE NO='" & CommandType & "' LoginName='" & LoginName & "' STT_text='" & STT_text & "' FilePath='" & FilePath & "' Json='" & Json & "' Param1='" & Param1 & "' Param2='" & Param2 & "' Param3='" & Param3 & "' Param4='" & Param4 & "' Param5='" & Param5 & "' Param6='" & Param6 & "' Param7='" & Param7 & "' Param8='" & Param8 & "' Param9='" & Param9 & "' Param10='" & Param10 & "'></VOICE>

	objNode = XML_addElement(objdom1,objRoot,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode.setAttribute("NO",vIndex)		'GUID
	objNode.setAttribute("GUID",GUID)		'
	objNode.setAttribute("CommandType",CommandType)

	objNode.setAttribute("CompanyID",CompanyID)
	objNode.setAttribute("LoginName",LoginName)
	objNode.setAttribute("STT_text",STT_text)
	objNode.setAttribute("FilePath",FilePath)
	'objNode.setAttribute("Json",Json)
	objNode.setAttribute("Param1",Param1)
	objNode.setAttribute("Param2",Param2)
	objNode.setAttribute("Param3",Param3)
	objNode.setAttribute("Param4",Param4)
	objNode.setAttribute("Param5",Param5)
	objNode.setAttribute("Param6",Param6)
	objNode.setAttribute("Param7",Param7)
	objNode.setAttribute("Param8",Param8)
	objNode.setAttribute("Param9",Param9)
	objNode.setAttribute("Param10",Param10)
	objNode.setAttribute("SaveTime",Now.ToString("yyyy/MM/dd HH:mm:ss"))


	objNode.setAttribute("CYB_Result",CYB_Result)
	objNode.setAttribute("CYB_PathScore",CYB_PathScore)
	objNode.setAttribute("CYB_NBestIdx",CYB_NBestIdx)
	objNode.setAttribute("CYB_Rule",CYB_Rule)
	objNode.setAttribute("CYB_Tag",CYB_Tag)
	objNode.setAttribute("action",action)

	if Param1 <> "" then
		'response.write(objdom1.xml)
		objdom1.save(server.mappath("xml/" & LogFileName))
	end if



'記錄所有Command Log檔
'-------------------------------------------
Dim LogFileName1 = "VoiceCommandMain.xml"


'把檔案叫出來
Dim AddNew=0

objdom1.load(server.mappath("xml/" & LogFileName1))
'response.write(objdom1.xml) 轉換檔案到實體的路徑(server asp)
objRoot = objdom1.documentElement
vIndex = 0
'找不到XML自行新增XML的物件
if objRoot is nothing then
	'找不到檔案
	vIndex = 1
	
	objdom1.loadXML("<?xml version='1.0'?><VAIMA></VAIMA>")
	objRoot = objdom1.documentElement

	AddNew=1

else
	'有檔案
	Dim objNode3 = objRoot.selectNodes("VOICE")
	vIndex = objNode3.length+1
	
	objNode = objRoot.selectSingleNode("VOICE[@GUID='" & GUID & "']")
	if objNode is nothing then
		'找不到這個GUID
		AddNew=1

	else
		'找的到這個GUID
		if Cint(CYB_PathScore) > Cint(objNode.getAttribute("CYB_PathScore")) then
			'如果新的數值比較大，就修改.否則就略過.
			objNode.setAttribute("STT_text",STT_text)
			objNode.setAttribute("FilePath",FilePath)
			'objNode.setAttribute("Json",Json)
			objNode.setAttribute("Param1",Param1)
			objNode.setAttribute("Param2",Param2)
			objNode.setAttribute("Param3",Param3)
			objNode.setAttribute("Param4",Param4)
			objNode.setAttribute("Param5",Param5)
			objNode.setAttribute("Param6",Param6)
			objNode.setAttribute("Param7",Param7)
			objNode.setAttribute("Param8",Param8)
			objNode.setAttribute("Param9",Param9)
			objNode.setAttribute("Param10",Param10)
			objNode.setAttribute("SaveTime",Now.ToString("yyyy/MM/dd HH:mm:ss"))


			objNode.setAttribute("CYB_Result",CYB_Result)
			objNode.setAttribute("CYB_PathScore",CYB_PathScore)
			objNode.setAttribute("CYB_NBestIdx",CYB_NBestIdx)
			objNode.setAttribute("CYB_Rule",CYB_Rule)
			objNode.setAttribute("CYB_Tag",CYB_Tag)
			objNode.setAttribute("action",action)

			if Param1 <> "" then
				'response.write(objdom1.xml)
				objdom1.save(server.mappath("xml/" & LogFileName1))
			end if

		end if

	end if
end if


if AddNew = 1 then

	objNode = XML_addElement(objdom1,objRoot,"VOICE","") '新增VOICE物件到VAIMA底下
	objNode.setAttribute("NO",vIndex)		'GUID
	objNode.setAttribute("GUID",GUID)
	objNode.setAttribute("CompanyID",CompanyID)
	objNode.setAttribute("CommandType",CommandType)
	objNode.setAttribute("LoginName",LoginName)
	objNode.setAttribute("STT_text",STT_text)
	objNode.setAttribute("FilePath",FilePath)
	'objNode.setAttribute("Json",Json)
	objNode.setAttribute("Param1",Param1)
	objNode.setAttribute("Param2",Param2)
	objNode.setAttribute("Param3",Param3)
	objNode.setAttribute("Param4",Param4)
	objNode.setAttribute("Param5",Param5)
	objNode.setAttribute("Param6",Param6)
	objNode.setAttribute("Param7",Param7)
	objNode.setAttribute("Param8",Param8)
	objNode.setAttribute("Param9",Param9)
	objNode.setAttribute("Param10",Param10)
	objNode.setAttribute("SaveTime",Now.ToString("yyyy/MM/dd HH:mm:ss"))


	objNode.setAttribute("CYB_Result",CYB_Result)
	objNode.setAttribute("CYB_PathScore",CYB_PathScore)
	objNode.setAttribute("CYB_NBestIdx",CYB_NBestIdx)
	objNode.setAttribute("CYB_Rule",CYB_Rule)
	objNode.setAttribute("CYB_Tag",CYB_Tag)
	objNode.setAttribute("action",action)

	if Param1 <> "" then
		
		objdom1.save(server.mappath("xml/" & LogFileName1))
	end if


end if


'<VOICE NO='" & CommandType & "' LoginName='" & LoginName & "' STT_text='" & STT_text & "' FilePath='" & FilePath & "' Json='" & Json & "' Param1='" & Param1 & "' Param2='" & Param2 & "' Param3='" & Param3 & "' Param4='" & Param4 & "' Param5='" & Param5 & "' Param6='" & Param6 & "' Param7='" & Param7 & "' Param8='" & Param8 & "' Param9='" & Param9 & "' Param10='" & Param10 & "'></VOICE>

	
if ReturnValue = "" or  ReturnValue = "0" or  ReturnValue = "2" then


	response.write(GUID & "<br>")
	response.write(CommandType & "<br>")
	response.write(CompanyID & "<br>")
	response.write(LoginName & "<br>")
	response.write(STT_text & "<br>")
	response.write(FilePath & "<br>")
	response.write(Json & "<br>")
	response.write(Param1 & "<br>")
	response.write(Param2 & "<br>")
	response.write(Param3 & "<br>")
	response.write(Param4 & "<br>")
	response.write(Param5 & "<br>")
	response.write(Param6 & "<br>")
	response.write(Param7 & "<br>")
	response.write(Param8 & "<br>")
	response.write(Param9 & "<br>")
	response.write(Param10 & "<br>")
	response.write(CYB_Result & "<br>")
	response.write(Param10 & "<br>")
	response.write(Param10 & "<br>")
	response.write(CYB_Tag & "<br>")
	response.write(action & "<br>")
	response.write(vIndex)
	response.write(objdom1.xml)

end if 

response.write("ReturnValue:" & ReturnValue & "<br>")
response.write("Param1:" & Param1)
if ReturnValue = "1"  or ReturnValue = 1 then

	Dim objdom2 = Server.CreateObject("Microsoft.XMLDOM")
	'objdom2.async=false
	Dim checkReturn = 0

	if Param1 = "新增行程" then
		checkReturn = 1
		objdom2.load(server.mappath("xml/NewSchedule.xml"))
	end if

	if Param1 = "給我今天行程" then
		checkReturn = 1
		objdom2.load(server.mappath("xml/TodaySchedule.xml"))
	end if

	if Param1 = "查詢今天行程" then
		checkReturn = 1
		objdom2.load(server.mappath("xml/TodaySchedule.xml"))
	end if

	if Param1 = "今天行程" then
		checkReturn = 1
		objdom2.load(server.mappath("xml/TodaySchedule.xml"))
	end if

	if checkReturn = 0 then
		checkReturn = 1
		objdom2.load(server.mappath("xml/ResultCommon.xml"))
	end if

	response.write (objdom2.xml)

end if

%>