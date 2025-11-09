<!--#include file="xmlfunction.inc"-->

<%

Dim objdom1 = Server.CreateObject("Microsoft.XMLDOM")
'objdom1.async=false
objdom1.load(server.mappath("xml/Result.xml"))
response.write (objdom1.xml)


%>