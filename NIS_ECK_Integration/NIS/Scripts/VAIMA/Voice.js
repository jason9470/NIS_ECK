var VoiceThisInputObj;
function VoiceopenText(userid,fee_no,PackageName,inputobj){

	VoiceThisInputObj = inputobj ;
	window.open("../Scripts/VAIMA/selectVoiceText.aspx?userid=" + userid + "&fee_no=" + fee_no + "&PackageName="+PackageName ,"ABC", config='height=500,width=500');
}
