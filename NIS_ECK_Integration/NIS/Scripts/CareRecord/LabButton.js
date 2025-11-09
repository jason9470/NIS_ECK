//***************程式獲取值***************//
var labDates_text = [];
var labDates_dateTime = [];
var labNames = [];
var labDateWithLabNames = [];
//***************日期往後n天***************//
function addDays(orgin_date, days) {
    orgin_date.setDate(orgin_date.getDate() + days);
    return orgin_date;
}

//***************獲取所有檢驗時間(不重複)***************//
function getAlllabDates(careRecord) {
    labDates_text = [];
    labDates_dateTime = [];

    //獲取不重複的檢驗時間
    careRecord.forEach(function (record) {
        if (labDates_text.indexOf(record.LabDate) == -1) { //檢驗時間還沒獲取
            labDates_text.push(record.LabDate);
            labDates_dateTime.push(new Date(record.LabDate));
        }
    });

    //***************快速排序法***************//
    //var arr = [85, 24, 63, 45, 17, 31, 96, 50];
    var quickSort = function (arr) {

        if (arr.length <= 1) { return arr; }

        var pivotIndex = Math.floor(arr.length / 2);

        var pivot = arr.splice(pivotIndex, 1)[0];

        var left = [];

        var right = [];

        for (var i = 0; i < arr.length; i++) {

            if (arr[i] < pivot) {

                left.push(arr[i]);

            } else {

                right.push(arr[i]);

            }

        }

        return quickSort(left).concat([pivot], quickSort(right));

    };
    //console.log(quickSort(arr));

    //所有檢驗時間(不重複DateTime格式)
    labDates_text = quickSort(labDates_text).reverse();
    labDates_dateTime = quickSort(labDates_dateTime).reverse();

    //console.log(labDates_dateTime);
    //console.log(labDates_text);
}

//***************獲取所有項目名稱(不重複)***************//
function getAllLabNames(careRecord) {
    labNames = [];

    //獲取不重複的項目名稱
    careRecord.forEach(function (record) {
        var pos = labNames.map(function (e) { return e.name; }).indexOf(record.LabName);
        if (pos == -1) { //有
            var labNameDetails_obj = getLabNameDetails(record);
            labNames.push(labNameDetails_obj);
        }
    });

    //排序項目名稱
    function compare(a, b) {
        if (a.name.toUpperCase() < b.name.toUpperCase())
            return -1;
        if (a.name.toUpperCase() > b.name.toUpperCase())
            return 1;
        return 0;
    }
    labNames.sort(compare);
}
function getLabNameDetails(record) {
    //name: "NA", specimen: "BLOOD", unit: "mEq/L"
    var obj = {
        name: record.LabName,
        specimen: record.Specimen,
        unit: record.LabValueUnit
    };
    return obj;
}

//*******獲取每個檢驗時間內的檢驗項目*******//
function getLabDateWithLabName(careRecord) {
    labDateWithLabNames = [];

    labDates_text.forEach(function (record) {

        //取得相同日期的所有紀錄
        var matchRecord = careRecord.filter(function (item, index, array) {
            return item.LabDate == record;
        });
        //console.log(matchRecord);

        //取得單一日期之下，所有檢驗項目及細節
        var recordDetails = [];
        matchRecord.forEach(function (record) {

            //同一時段有多筆同樣的項目，但數值不同
            /*var pos = recordDetails.map(function (e) { return e.name; }).indexOf(record.LabName);
            if (pos != -1) { //有
                recordDetails[pos].value = parseFloat(recordDetails[pos].value) + parseFloat(record.LabValue);

                if (record.LabErrorFlag == "Y") {
                    recordDetails[pos].errorFlag = "Y";
                }
            } else {*/ //還未有
            var recordDetail_obj = getRecordDetails(record);  //建立recordDetail_obj物件，name: "NA", specimen: "BLOOD", value: 141, unit: "mEq/L", errorFlag: "N"]
            recordDetails.push(recordDetail_obj);
            //}
        });

        var date_name_obj = getDateName(record, recordDetails);//建立date_name_obj物件
        labDateWithLabNames.push(date_name_obj);

        /***** date_name_obj 物件內容
            date_name_obj = {
                labDate: "2018-08-20T04:46:00",
                labDetail: {
                    [name: "NA", value: 141, errorFlag: "N"], 
                    [name: "K", value: 4.2, errorFlag: "Y"], 
                }
            }
        *****/
    });
    /*labDateWithLabNames.forEach(function (record) {
        console.log("labDate: " + record.labDate + ",\r\n" +
					"labDetail: {\r\n" +
					"[name: " + record.labDetail[0].name + ", " +
                    "value: " + record.labDetail[0].value + ", " +
					"errorFlag: " + record.labDetail[0].errorFlag + "], \r\n" +
					"[name: " + record.labDetail[1].name + ", " +
                    "value: " + record.labDetail[1].value + ", " +
					"errorFlag: " + record.labDetail[1].errorFlag + "], \r\n" +
					"\r\n}");
    });*/

}
function getDateName(record_labDate, recordDetail_array) {
    var obj = {
        labDate: record_labDate,
        labDetail: recordDetail_array
    };
    //document.write(obj.tubeName + "<br>");
    return obj;
}
function getRecordDetails(record) {
    //name: "NA", value: 141, errorFlag: "N"
    var obj = {
        name: record.LabName,
        value: record.LabValue,
        errorFlag: record.LabErrorFlag
    };
    return obj;
}

//***************建立檢驗資料表格***************//
function buildCareRecordTable(table_id, careRecord, hasBtn) {
    var table = document.getElementById(table_id);
    var n = 0, i = 0;

    //***************大標題***************//
    var headerRow = table.insertRow(-1);  //加入新row
    headerRow.className = "GridViewScrollHeader";

    var cell = headerRow.insertCell(0);
    cell.innerHTML = "項目";
    cell.className = "LabTitle SubTitle_name";
    //cell.style.width = "160px";

    cell = headerRow.insertCell(1);
    cell.innerHTML = "檢體";
    cell.className = "LabTitle SubTitle_specimen";
    //cell.style.width = "123px";

    cell = headerRow.insertCell(2);
    cell.innerHTML = "單位";
    cell.className = "LabTitle SubTitle_unit";
    //cell.style.width = "66px";

    var single_width = (table.clientWidth - (160 + 123 + 66)) / labDates_dateTime.length;

    //檢驗時間
    n = 3;
    labDates_dateTime.forEach(function (record) {
        var year = record.getFullYear();
        var month = record.getMonth() + 1;
        var date = record.getDate();
        var hours = record.getHours();
        var minutes = record.getMinutes();
        var secs = record.getSeconds();

        //補0
        month = month < 10 ? '0' + month : month;
        date = date < 10 ? '0' + date : date;
        hours = hours < 10 ? '0' + hours : hours;
        minutes = minutes < 10 ? '0' + minutes : minutes;
        secs = secs < 10 ? '0' + secs : secs;

        cell = headerRow.insertCell(n);
        n++;
        cell.innerHTML = year + "/" + month + "/" + date + "</br>" + hours + ":" + minutes + ":" + secs;
        cell.id = "LabDate" + i;
        i++;
        //cell.style.width = single_width + "px";
    });

    //內容
    var y = 0;
    var backgroundColor = '#ABC8E2';  //偶數 #ABC8E2，奇數 #E1E6FA

    for (i = 0; i < labNames.length; i++) {
        if (i == 0 || i % 2 == 0) {
            backgroundColor = '#E1E6FA'; //#E1E6FA 奇數行色
        } else {
            backgroundColor = '#ABC8E2'; //偶數行色
        }

        var itemRow = table.insertRow(-1);  //加入新row
        itemRow.className = "GridViewScrollItem";
        itemRow.style.backgroundColor = backgroundColor;

        //項目名稱
        cell = itemRow.insertCell(0);
        cell.innerHTML = labNames[i].name;
        //cell.style.width = "160px";
        cell.className = "SubTitle_name";

        //項目檢體
        cell = itemRow.insertCell(1);
        cell.innerHTML = labNames[i].specimen;
        //cell.style.width = "123px";
        cell.className = "SubTitle_specimen";

        //項目單位
        cell = itemRow.insertCell(2);
        cell.innerHTML = labNames[i].unit;
        //cell.style.width = "66px";
        cell.className = "SubTitle_unit";

        n = 3;
        y = 0; //日期(labDateWithLabNames)
        labDates_text.forEach(function (record) {  //找出每個日期有哪些檢驗項目
            var pos = labDateWithLabNames[y].labDetail.map(function (e) { return e.name; }).indexOf(labNames[i].name);

            if (pos != -1) {  //目前這個日期有這筆檢驗項目
                cell = itemRow.insertCell(n);
                n++;
                var labValue = labDateWithLabNames[y].labDetail[pos].value
                cell.innerHTML = labValue;
                //cell.style.width = single_width + "px";

                if (labDateWithLabNames[y].labDetail[pos].errorFlag == "Y") {
                    cell.style.color = "red";
                }
                if (hasBtn) {
                    cell.innerHTML += "<input id=\"0\" type=\"button\" value=\"帶入\" onclick=\"btn_write('" + record + "', '" + labNames[i].name + "', '" + labNames[i].unit + "', '" + labValue + "');\" />";
                }
            } else {  //目前這個日期沒有這筆檢驗項目
                cell = itemRow.insertCell(n);
                cell.innerHTML = "";
                n++;
                //cell.style.width = single_width + "px";
            }
            y++;//下一個日期(labDateWithLabNames)
        });

    }
}
//***************建立空檢驗資料表格***************//
function buildEmptyCareRecordTable(table_id) {
    var table = document.getElementById(table_id);

    //***************大標題***************//
    var headerRow = table.insertRow(-1);  //加入新row
    headerRow.className = "GridViewScrollHeader";

    var cell = headerRow.insertCell(0);
    cell.innerHTML = "項目";
    cell.style.width = "160px";

    cell = headerRow.insertCell(1);
    cell.innerHTML = "檢體";
    cell.style.width = "123px";

    cell = headerRow.insertCell(2);
    cell.innerHTML = "單位";
    cell.style.width = "66px";

    cell = headerRow.insertCell(3);
    cell.innerHTML = "";
    cell.id = "LabDate0";
    cell.style.width = "160px";

    //內容
    var backgroundColor = '#ABC8E2';  //偶數 #ABC8E2，奇數 #E1E6FA

    var itemRow = table.insertRow(-1);  //加入新row
    itemRow.className = "GridViewScrollItem";
    itemRow.style.backgroundColor = backgroundColor;

    cell = itemRow.insertCell(0);
    cell.innerHTML = "";

    cell = itemRow.insertCell(1);
    cell.innerHTML = "";

    cell = itemRow.insertCell(2);
    cell.innerHTML = "";

    cell = itemRow.insertCell(3);
    cell.innerHTML = "";
}

//***************檢驗資料處理流程***************//
function caculateCareRecord(table_id, careRecord) {
    getAlllabDates(careRecord);  //獲取所有引檢驗時間(不重複)
    getAllLabNames(careRecord);  //獲取所有引檢驗項目(不重複)
    //console.log(labNames);
    getLabDateWithLabName(careRecord);
}

