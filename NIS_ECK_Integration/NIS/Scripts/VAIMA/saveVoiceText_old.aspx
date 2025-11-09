<%

'類別
Dim CommandType = request("CommandType")
response.write(CommandType & "<br>")

'登入帳號
Dim LoginName = request("LoginName")
response.write(LoginName & "<br>")

'識別結果
Dim STT_text = request("STT_text")
response.write(STT_text & "<br>")

'音檔位置
Dim FilePath = request("FilePath")
response.write(FilePath & "<br>")

'語音辨識回饋結果
Dim Json = request("Json")
response.write(Json & "<br>")

'Json回傳參數1
Dim Param1 = request("Param1")
response.write(Param1 & "<br>")

'Json回傳參數2
Dim Param2 = request("Param2")
response.write(Param2 & "<br>")

'Json回傳參數3
Dim Param3 = request("Param3")
response.write(Param3 & "<br>")

'Json回傳參數4
Dim Param4 = request("Param4")
response.write(Param4 & "<br>")

'Json回傳參數5
Dim Param5 = request("Param5")
response.write(Param5 & "<br>")

'Json回傳參數6
Dim Param6 = request("Param6")
response.write(Param6 & "<br>")

'Json回傳參數7
Dim Param7 = request("Param7")
response.write(Param7 & "<br>")

'Json回傳參數8
Dim Param8 = request("Param8")
response.write(Param8 & "<br>")

'Json回傳參數9
Dim Param9 = request("Param9")
response.write(Param9 & "<br>")

'Json回傳參數10
Dim Param10 = request("Param10")
response.write(Param10 & "<br>")

'產生XML檔案
Dim objdom = Server.CreateObject("Microsoft.XMLDOM")
objdom.loadXML("<?xml version='1.0'?><VAIMA><VOICE NO='" & CommandType & "' LoginName='" & LoginName & "' STT_text='" & STT_text & "' FilePath='" & FilePath & "' Json='" & Json & "' Param1='" & Param1 & "' Param2='" & Param2 & "' Param3='" & Param3 & "' Param4='" & Param4 & "' Param5='" & Param5 & "' Param6='" & Param6 & "' Param7='" & Param7 & "' Param8='" & Param8 & "' Param9='" & Param9 & "' Param10='" & Param10 & "'></VOICE></VAIMA>")
response.write(objdom.xml)
objdom.save(server.mappath("xml/saveVoiceText.xml"))


%>