
//取下所有GET參數值
var url = location.href;
var getPara, ParaVal, queryString = "";
var aryPara = [];
var pkey = [];
var pvalue = [];
var i;
var getSearch = url.split("?"); // url?key=value&key=value
getPara = getSearch[1].split("&"); // key=value&key=value
for (i = 0; i < getPara.length; i++) {
    ParaVal = getPara[i].split("="); // key=value
    pkey[i] = ParaVal[0]; // key
    pvalue[i] = ParaVal[1]; // value
}

function getRequest(pname)
{
    for (i = 0; i <= pkey.length - 1; i++)
    {
        if (pkey[i] == pname) {
            return pvalue[i];
        }
    }
}


function returnValue(rValue)
{
    var filedSet = [];
    var cField;
    filedSet = getRequest("carryinField").split("$");
    //按照順序塞值回父表單
    for (i = 0; i <= filedSet.length - 1 ; i++) {
        cField = window.opener.document.getElementById(filedSet[i]);
        cField.value = rValue[i];
    }
    window.close();
}
