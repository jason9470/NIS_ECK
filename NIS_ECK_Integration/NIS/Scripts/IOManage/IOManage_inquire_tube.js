//***************程式獲取值***************//
var inquire_tubes = [];  //所有引流管陣列
var inquire_tubes_ID = [];  //所有引流管ID陣列
var timeBlocks = [];     //所有時間區間
var timeBlocks_total_index = [];  //小計在timeBlocks[]中的索引值
var timeBlocks_start_index = [0];  //班別第一區間在timeBlocks[]中的索引值
var time_tubes = [];     //所有時間區間內，每個引流管流量

var D_txt = "";  //白班小計
var E_txt = "";  //小夜班小計
var N_txt = "";  //大夜班小計
var DEN_txt = "";  //大夜班小計

//***************解決IE8 ForEach無法使用問題***************//
//if (typeof Array.prototype.forEach != 'function') {
//    Array.prototype.forEach = function (callback) {
//        for (var i = 0; i < this.length; i++) {
//            callback.apply(this, [this[i], i, this]);
//        }
//    };
//}

//***************日期往後n天***************//
function addDays(orgin_date, days) {
    orgin_date.setDate(orgin_date.getDate() + days);
    return orgin_date;
}

//***************獲取所有引流管名稱、ID(不重複)***************//
function getAllInquireTubes(inquire_tubes_record) {
    inquire_tubes = [];
    inquire_tubes_ID = [];

    for (i = 0; i < inquire_tubes_record.length; i++) {
        //console.log(inquire_tubes_record[i].TUBE_CONTENT);
        if ($.inArray(inquire_tubes_record[i].TUBE_CONTENT, inquire_tubes) == -1) { //引流管名稱還沒獲取
            inquire_tubes.push(inquire_tubes_record[i].TUBE_CONTENT);
            //console.log(inquire_tubes);  //["2-way Foley尿道左#39", "2-way Foley膀胱左#40", ..., "Rubber Tube口腔左#70"]
        }

        if ($.inArray(inquire_tubes_record[i].ITEMID, inquire_tubes_ID) == -1) { //引流管ID還沒獲取
            inquire_tubes_ID.push(inquire_tubes_record[i].ITEMID);
        }
    }
}
/*inquire_tubes.forEach(function(inquire_tube) {
	console.log(inquire_tube + ' ');
});*/
//***************獲取所有時間區間***************//
function getAllTimeBlocks(shiftClassD, shiftClassE, shiftClassN, timeBlock_hours) {
    timeBlocks = [];
    timeBlocks_total_index = [];
    timeBlocks_start_index = [0];

    var n = 0;
    var shiftClassAll = shiftClassD.concat(shiftClassE).concat(shiftClassN);
    var x = shiftClassAll.length - timeBlock_hours;  //迴圈只跑到最後一個區間的開始小時

    for (i = 0; i <= x; i = i + timeBlock_hours) {
        if (i == x) {
            timeBlocks.push(shiftClassAll[i] + "-" + shiftClassAll[0]);  //最後一個區間
        } else {
            timeBlocks.push(shiftClassAll[i] + "-" + shiftClassAll[i + timeBlock_hours]);
        }
        n++;
        //該區間最後時段，後面加上小計
        if (shiftClassAll[i + timeBlock_hours] == shiftClassE[0] || shiftClassAll[i + timeBlock_hours] == shiftClassN[0] || (i == x)) {
            timeBlocks.push("小計");
            timeBlocks_total_index.push(n);

            if (i != x) {
                timeBlocks_start_index.push(n + 1);
            }
            n++;
        }
    }
    //console.log(timeBlocks_start_index);  // [0, 5, 10]
    //console.log(timeBlocks_total_index);  // [4, 9, 14]
    //console.log(timeBlocks);  // ["07-09", "09-11", "11-13", "13-15", "小計", "15-17", "17-19", "19-21", "21-23", "小計", "23-01", "01-03", "03-05", "05-07", "小計"]
}

//*******計算三個班別內及所有班別，每個引流管流量(單日)*******//
function caculateDENAmount(shiftClassD, shiftClassE, shiftClassN, inquire_tubes_record, inputDate_arr, unit) {
    //var inputDate_arr = inputDate.split("/");
    var start_D_DateTime, end_D_DateTime; //白班的開頭、結尾DateTime
    var start_E_DateTime, end_E_DateTime; //小夜班的開頭、結尾DateTime
    var start_N_DateTime, end_N_DateTime; //大夜班的開頭、結尾DateTime

    start_D_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], shiftClassD[0]);
    end_D_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], shiftClassD[7], 59, 59);
    //console.log(start_D_DateTime);
    //console.log(end_D_DateTime);

    start_E_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], shiftClassE[0]);
    end_E_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], shiftClassE[7], 59, 59);
    //console.log(start_E_DateTime);
    //console.log(end_E_DateTime);

    start_N_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], shiftClassN[0]);
    end_N_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], shiftClassN[7], 59, 59);
    end_N_DateTime = addDays(end_N_DateTime, 1); //隔天(大夜)
    //console.log(start_N_DateTime);
    //console.log(end_N_DateTime);

    var job_tubes = [];

    jQuery.each(inquire_tubes ,function (index,inquire_tube) {  //所有引流管(不重複的引流管陣列)

        var D_amount = 0.0;  //白班流量
        var E_amount = 0.0;  //小夜
        var N_amount = 0.0;  //大夜
        var All_amount = 0.0;  //全部

        var D_hasLoss = false; //白班是否有Loss流量
        var E_hasLoss = false; //小夜是否有Loss流量
        var N_hasLoss = false; //大夜是否有Loss流量
        var All_hasLoss = 0.0;  //全部

        var tube_DENs = [];

        jQuery.each(inquire_tubes_record,function (index,record) {  //所有引流管紀錄

            var record_DateTime = new Date(record.CREATETIME);

            if (inquire_tube == record.TUBE_CONTENT) {

                var temp_amount = record.AMOUNT;
                if (record.AMOUNT == null) {
                    temp_amount = 0;
                }
                //白班
                if (record_DateTime >= start_D_DateTime && record_DateTime <= end_D_DateTime) {
                    D_amount += parseFloat(temp_amount);
                    if (record.REASON == "1") {
                        D_hasLoss = true;
                    }
                } else if (record_DateTime >= start_E_DateTime && record_DateTime <= end_E_DateTime) {
                    //小夜班
                    E_amount += parseFloat(temp_amount);
                    if (record.REASON == "1") {
                        E_hasLoss = true;
                    }
                } else if (record_DateTime >= start_N_DateTime && record_DateTime <= end_N_DateTime) {
                    //大夜班
                    N_amount += parseFloat(temp_amount);
                    if (record.REASON == "1") {
                        N_hasLoss = true;
                    }
                }

                All_amount += parseFloat(temp_amount);
                if (record.REASON == "1") {
                    All_hasLoss = true;
                }

            }
        });
        var tube_DEN_obj = getTubeDEN("D", D_amount, D_hasLoss); //白
        tube_DENs.push(tube_DEN_obj);

        tube_DEN_obj = getTubeDEN("E", E_amount, E_hasLoss); //小夜
        tube_DENs.push(tube_DEN_obj);

        tube_DEN_obj = getTubeDEN("N", N_amount, N_hasLoss);//大夜
        tube_DENs.push(tube_DEN_obj);

        tube_DEN_obj = getTubeDEN("All", All_amount, All_hasLoss);//全部
        tube_DENs.push(tube_DEN_obj);

        var job_tube_obj = getTube(inquire_tube, tube_DENs);
        /***** job_tube_obj 物件內容
			job_tube_obj = {
				tubeName: 2-way Foley尿道左#39,
				DEN: {
					[jobBlock: D, amount: 95, hasLoss: false], 
					[jobBlock: E, amount: 0, hasLoss: false], 
					[jobBlock: N, amount: 0, hasLoss: false], 
					[jobBlock: All, amount: 95, hasLoss: 0]
				}
			}
		*****/
        job_tubes.push(job_tube_obj);

        /*console.log("tubeName: " + job_tube_obj.tubeName + ",\r\n" + 
					"DEN: {\r\n" + 
					"[jobBlock: " + job_tube_obj.DEN[0].jobBlock + ", " +
					"amount: " + job_tube_obj.DEN[0].amount + ", " + 
					"hasLoss: " + job_tube_obj.DEN[0].hasLoss + "], \r\n" +
					"[jobBlock: " + job_tube_obj.DEN[1].jobBlock + ", " +
					"amount: " + job_tube_obj.DEN[1].amount + ", " + 
					"hasLoss: " + job_tube_obj.DEN[1].hasLoss + "], \r\n" +
					"[jobBlock: " + job_tube_obj.DEN[2].jobBlock + ", " +
					"amount: " + job_tube_obj.DEN[2].amount + ", " + 
					"hasLoss: " + job_tube_obj.DEN[2].hasLoss + "], \r\n" +
					"[jobBlock: " + job_tube_obj.DEN[3].jobBlock + ", " +
					"amount: " + job_tube_obj.DEN[3].amount + ", " + 
					"hasLoss: " + job_tube_obj.DEN[3].hasLoss + "]" +
					"\r\n}");*/
    });

    //彙整 四個班別內，每個引流管流量
    D_txt = "";
    E_txt = "";
    N_txt = "";
    DEN_txt = "";

    jQuery.each(job_tubes, function (index,tube) {
        //console.log(tube.tubeName);

        jQuery.each(tube.DEN, function (index,record) {
            //console.log(record.jobBlock + ", " + record.amount + ", " + record.hasLoss);

            if (record.amount > 0) {
                if (record.jobBlock == "D") {
                    D_txt = addTubeToTotal(tube.tubeName, record.amount, record.hasLoss, D_txt, unit);
                } else if (record.jobBlock == "E") {
                    E_txt = addTubeToTotal(tube.tubeName, record.amount, record.hasLoss, E_txt, unit);
                } else if (record.jobBlock == "N") {
                    N_txt = addTubeToTotal(tube.tubeName, record.amount, record.hasLoss, N_txt, unit);
                } else if (record.jobBlock == "All") {
                    DEN_txt = addTubeToTotal(tube.tubeName, record.amount, record.hasLoss, DEN_txt, unit);
                }
            }

        });

    });
    //console.log(D_txt);
    //console.log(E_txt);
    //console.log(N_txt);
    //console.log(DEN_txt);
}

//*******輸出四個引流管總計(輸出array, 長度4, 白班,小夜,大夜,全部)*******//
function getDENTotalTxt(shiftClassD, shiftClassE, shiftClassN, inquire_tubes_record, inputDate, unit) {
    var inputDate_arr = inputDate.split("/");

    getAllInquireTubes(inquire_tubes_record);
    caculateDENAmount(shiftClassD, shiftClassE, shiftClassN, inquire_tubes_record, inputDate_arr, unit);

    var str = [];
    str.push(D_txt);
    str.push(E_txt);
    str.push(N_txt);
    str.push(DEN_txt);

    return str;
}

function getTube(record_tubeName, record_DEN) {
    var obj = {
        tubeName: record_tubeName,
        DEN: record_DEN,
    };
    //document.write(obj.tubeName + "<br>");
    return obj;
}

function getTubeDEN(record_jobBlock, record_amount, record_hasLoss) {
    var obj = {
        jobBlock: record_jobBlock,
        amount: record_amount,
        hasLoss: record_hasLoss
    };
    //document.write(obj.tubeName + "<br>");
    return obj;
}

function addTubeToTotal(tube, sum, loss, total_txt, unit) {
    if (total_txt === "") {
        if (loss) {
            total_txt = tube + "：" + sum + unit + "+Loss";
        } else {
            total_txt = tube + "：" + sum + unit;
        }
    } else {
        if (loss) {
            total_txt += "； " + tube + "：" + sum + unit + "+Loss";
        } else {
            total_txt += "； " + tube + "：" + sum + unit;
        }
    }
    return total_txt;
}

//*******計算所有時間區間內，每個引流管流量(單日)*******//
function getTimeTubesArray(inquire_tubes_record, inputDate_arr) {
    time_tubes = [];
    jQuery.each(timeBlocks, function (index,timeBlock) {  //所有時間區間
        var tubes = []; //符合的引流管紀錄  陣列

        if (timeBlock != "小計") {
            var timeBlock_Hour = timeBlock.split("-");  //時間區間
            var start_hour = timeBlock_Hour[0];  //時間區間，開始小時
            var end_hour = timeBlock_Hour[1];    //時間區間，結束小時

            var start_timeBlock_DateTime, end_timeBlock_DateTime;  //時間區間，開始及結束DateTime
            var hasLoss = false;
            var totalAmount = 0.0;

            if (parseInt(start_hour) < 7) { //大夜(隔天)
                start_timeBlock_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], start_hour);
                start_timeBlock_DateTime = addDays(start_timeBlock_DateTime, 1); //隔天(大夜)
                end_timeBlock_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], end_hour);
                end_timeBlock_DateTime = addDays(end_timeBlock_DateTime, 1); //隔天(大夜)	
            } else {
                if (end_hour > start_hour) {
                    start_timeBlock_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], start_hour);
                    end_timeBlock_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], end_hour);

                } else if (start_hour > end_hour) { //大夜23點
                    start_timeBlock_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], start_hour);
                    end_timeBlock_DateTime = new Date(inputDate_arr[0], parseInt(inputDate_arr[1]) - 1, inputDate_arr[2], end_hour);
                    end_timeBlock_DateTime = addDays(end_timeBlock_DateTime, 1); //隔天(大夜)
                }
            }
            //console.log(timeBlock);
            //console.log(start_timeBlock_DateTime);
            //console.log(end_timeBlock_DateTime);

            jQuery.each(inquire_tubes_record, function (index,record) {  //所有引流管紀錄

                var record_DateTime = new Date(record.CREATETIME);

                var tubes_obj = {};

                if (record_DateTime >= start_timeBlock_DateTime && record_DateTime < end_timeBlock_DateTime) {
                    //在時間內的引流管紀錄
                    var temp_amount = record.AMOUNT;
                    if (temp_amount == null) {
                        temp_amount = 0;
                    }

                    if (record.REASON == "1") {
                        hasLoss = true;  //該時間區間是否有Loss
                    }
                    totalAmount += parseFloat(temp_amount);  //該時間區間合計


                    //同一時段有多筆該引流管紀錄
                    //var alreadyInArray = tubes.filter(tube => tube.tubeName === record.TUBE_CONTENT); //陣列中是否已有該引流管
                    var pos = $.inArray(record.TUBE_CONTENT, tubes.map(function (e) { return e.tubeName; }));

                    if (pos != -1) { //有
                        tubes[pos].amount = parseFloat(tubes[pos].amount) + parseFloat(temp_amount);

                        if (record.REASON == 1) {
                            tubes[pos].reason = 1;
                        }
                    } else { //還未有
                        var tubes_obj = getMatchTube(record.TUBE_CONTENT, temp_amount, record.REASON);  //REASON = 1 表示有Loss
                        tubes.push(tubes_obj); //將符合的引流管紀錄加進陣列
                    }

                    //console.log(tubes_obj.tubeName + ", " + tubes_obj.amount + ", " + tubes_obj.reason);
                }

            });
            var time_tubes_obj = getTimeTube(timeBlock, tubes, totalAmount, hasLoss);
            time_tubes.push(time_tubes_obj);
            /*****
			time_tubes_obj = {
				timeBlock: 07-09,
				tubes: {
					[tubeName: 2-way Foley膀胱左#40, amount: 21.64, reason: 0], 
					[tubeName: ESD鎖骨下上#43, amount: 24, reason: 0]}, 
				totalAmount: 45.64,
				hasLoss: false
			}
			time_tubes_obj = {
				timeBlock: 19-21,
				tubes: {}, 
				totalAmount: 0,
				hasLoss: false
			}
			****/

            /*console.log("timeBlock: " + time_tubes_obj.timeBlock + ",\r\n" + 
					"tubes: {\r\n" + 
						"[tubeName: " + time_tubes_obj.tubes[0].tubeName + ", " +
						"amount: " + time_tubes_obj.tubes[0].amount + ", " + 
						"reason: " + time_tubes_obj.tubes[0].reason + "], \r\n" +
						"[tubeName: " + time_tubes_obj.tubes[1].tubeName + ", " +
						"amount: " + time_tubes_obj.tubes[1].amount + ", " + 
						"reason: " + time_tubes_obj.tubes[1].reason + "]}, \r\n" +
					"totalAmount: " + time_tubes_obj.totalAmount + ",\r\n" + 
					"hasLoss: " + time_tubes_obj.hasLoss + "\r\n");*/
        }
    });

    //列出time_tubes(所有時間區間內，每個引流管流量)陣列
    /*time_tubes.forEach(function(element) {
		console.log(element.timeBlock);
		console.log(element.totalAmount);
		console.log(element.hasLoss);

		element.tubes.forEach(function(tube) {
			console.log(tube.tubeName + ", " + tube.amount + ", " + tube.reason);
		});
	  
	});*/
}

function getTimeTube(record_timeBlock, record_tubes, record_totalAmount, record_hasLoss) {
    var obj = {
        timeBlock: record_timeBlock,
        tubes: record_tubes,
        totalAmount: record_totalAmount,
        hasLoss: record_hasLoss
    };
    return obj;
}

function getMatchTube(record_tubeName, record_amount, record_reason) {
    var obj = {
        tubeName: record_tubeName,
        amount: record_amount,
        reason: record_reason
    };
    //document.write(obj.tubeName + "<br>");
    return obj;
}

//***************建立表格***************//
function buildTable(table_id, inputDate, jobBlocks) {
    var table = document.getElementById(table_id);
    var inputDate_arr = inputDate.split("/");

    //***************大標題***************//
    var headerRow = table.insertRow(-1);  //加入新row
    headerRow.className = "GridViewScrollHeader";

    var cell = headerRow.insertCell(0);
    cell.innerHTML = "引流管";
    //cell.color = "#85A5CC";
    cell.className = "HideCell";
    cell.colSpan = 2; //水平合併

    cell = headerRow.insertCell(1);
    cell.innerHTML = "引流管";
    cell.colSpan = inquire_tubes.length; //水平合併
    cell.id = "InquireTubeTitle";

    //合計欄位
    cell = headerRow.insertCell(2);
    cell.rowSpan = 2; //垂直合併
    cell.innerHTML = "合計";
    cell.style.width = "72px";
    cell.id = "timeblockTotalTitle";


    //***************次標題***************//
    headerRow = table.insertRow(-1);  //加入新row
    headerRow.className = "GridViewScrollHeader";

    cell = headerRow.insertCell(0);
    cell.innerHTML = "班別";
    cell.style.width = "70px";
    ////cell.style.width = "10%";
    cell.style.height = "20px";
    cell.className = "jobBlocksTitle";

    cell = headerRow.insertCell(1);
    cell.innerHTML = "時間\\管路";
    //cell.style.width = "10%";
    cell.style.width = "53px";
    cell.className = "timeBlocksTitle";


    //所有引流管
    for (i = 0; i < inquire_tubes.length; i++) {
        cell = headerRow.insertCell(i + 2);
        cell.innerHTML = inquire_tubes[i] +
		"<input type='button' value='明細' style='font-size: 12px;margin-left: 10px;' onclick='pup_window(\"Detail?show_by=tube&from_func=IO_Tube&itemid=" + inquire_tubes_ID[i] + "&day=" + inputDate + "\", \"1000\", \"500\");'>";  //inquire_tubes_ID[i]
        cell.id = "inquire_tube" + i;
        cell.className = "inquire_tube";
        cell.style.textAlign = "center";
    }

    //內容
    var jobi = 0;
    var y = 0;
    var backgroundColor = '#ABC8E2';  //偶數
    //#E1E6FA 奇數

    for (i = 0; i < timeBlocks.length; i++) {
        if (i == 0 || i % 2 == 0) {
            backgroundColor = '#E1E6FA'; //#E1E6FA 奇數行色
        } else {
            backgroundColor = '#ABC8E2'; //偶數行色
        }
        //新一行
        var itemRow = table.insertRow(-1);  //加入新row
        itemRow.className = "GridViewScrollItem";
        itemRow.style.backgroundColor = backgroundColor;
        var n = 0;//橫的第幾個欄位 cell = itemRow.insertCell(n);

        //班別	
        if (i == timeBlocks_start_index[0]) { //白班
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = inputDate + "<br>" + jobBlocks[jobi] +
			"<br><input type='button' value='帶入護理記錄' style='word-break:break-all;overflow:hidden;font-size: 12px;margin-top: 10px;  width: 70px; height: 45px; white-space: pre-wrap;' onclick='Take_CareRecord(\"D\", \"" + inputDate + "\",\"2\",\"白班\")'>";
            cell.rowSpan = timeBlocks_total_index[0] + 1; //垂直合併
            cell.style.textAlign = "center";
            cell.style.width = "70px";
            cell.style.backgroundColor = "#85A5CC";
            jobi++;

        } else if (i == timeBlocks_start_index[1]) { //小夜
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = inputDate + "<br>" + jobBlocks[jobi] +
			"<br><input type='button' value='帶入護理記錄' style='word-break:break-all;overflow:hidden;font-size: 12px;margin-top: 10px;width: 70px; height: 45px; white-space: pre-wrap;' onclick='Take_CareRecord(\"E\", \"" + inputDate + "\",\"2\",\"小夜班\")'>";
            cell.rowSpan = timeBlocks_total_index[0] + 1; //垂直合併
            cell.style.textAlign = "center";
            cell.style.width = "70px";
            cell.style.backgroundColor = "#85A5CC";

            jobi++;

        } else if (i == timeBlocks_start_index[2]) { //大夜
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = inputDate + "<br>" + jobBlocks[jobi] +
			"<br><input type='button' value='帶入護理記錄' style='word-break:break-all;overflow:hidden;font-size: 12px;margin-top: 10px;width: 70px; height: 45px; white-space: pre-wrap;' onclick='Take_CareRecord(\"N\", \"" + inputDate + "\",\"2\",\"大夜班\")'>";
            cell.rowSpan = timeBlocks_total_index[0] + 1; //垂直合併
            cell.style.textAlign = "center";
            cell.style.width = "70px";
            cell.style.backgroundColor = "#85A5CC";
            jobi++;

        }

        //時間區間
        cell = itemRow.insertCell(n);
        n++;
        cell.innerHTML = timeBlocks[i];
        //cell.style.width = "10%";
        cell.style.width = "53px";

        var totalAmount = 0;

        //引流管數值&小計
        if (i == timeBlocks_total_index[0]) {  //白班小計
            cell = itemRow.insertCell(n);
            n++;
            if (D_txt != "") {
                cell.innerHTML = "白班內：" + D_txt;
            } else {
                cell.innerHTML = "";
            }
            cell.style.textAlign = "left";
            cell.colSpan = inquire_tubes.length + 1;  //水平合併
            cell.id = "subtotal_by_TempClass_D";

        } else if (i == timeBlocks_total_index[1]) {  //小夜班小計
            cell = itemRow.insertCell(n);
            n++;
            if (E_txt != "") {
                cell.innerHTML = "小夜班內：" + E_txt;
            } else {
                cell.innerHTML = "";
            }
            cell.style.textAlign = "left";
            cell.colSpan = inquire_tubes.length + 1;  //水平合併
            cell.id = "subtotal_by_TempClass_E";

        } else if (i == timeBlocks_total_index[2]) {  //大夜班小計
            cell = itemRow.insertCell(n);
            n++;
            if (N_txt != "") {
                cell.innerHTML = "大夜班內：" + N_txt;
            } else {
                cell.innerHTML = "";
            }
            cell.style.textAlign = "left";
            cell.colSpan = inquire_tubes.length + 1;  //水平合併
            cell.id = "subtotal_by_TempClass_N";

        } else {
            //console.log(time_tubes[y].timeBlock);

            //引流管在該時間區間的流量
            if (time_tubes[y].totalAmount > 0 || (time_tubes[y].totalAmount == 0 && time_tubes[y].hasLoss == true)) {
                var tmp_tubes = [];
                var tmp_amounts = [];
                var tmp_hasLoss = [];

                jQuery.each(time_tubes[y].tubes,function (index,tube) {
                    tmp_tubes.push(tube.tubeName);
                    tmp_amounts.push(tube.amount);
                    tmp_hasLoss.push(tube.reason);
                    //console.log(tube.tubeName + ' ' + tube.amount + ' ' + tube.reason);
                });

                var j = 0;

                jQuery.each(inquire_tubes,function (index,element) {

                    cell = itemRow.insertCell(n);
                    n++;
                    //cell.style.width = header_width[j];
                    if (element == tmp_tubes[j]) {
                        if (tmp_hasLoss[j] == 1) {
                            cell.innerHTML = tmp_amounts[j] + "+Loss";
                        } else {
                            cell.innerHTML = tmp_amounts[j];
                        }
                        j++;
                    } else {
                        cell.innerHTML = "";
                    }

                });

                //合計
                cell = itemRow.insertCell(n);
                //cell.style.width = "60px";
                n++;
                cell.style.color = "red";
                cell.id = "timeblockTotal" + i;
                if (time_tubes[y].hasLoss) {
                    cell.innerHTML = time_tubes[y].totalAmount + "+Loss";
                } else {
                    cell.innerHTML = time_tubes[y].totalAmount;
                }

            } else {

                for (j = 0; j < inquire_tubes.length; j++) {
                    cell = itemRow.insertCell(n);
                    n++;
                    cell.innerHTML = "";
                }

                //合計
                cell = itemRow.insertCell(n);
                //cell.style.width = "60px";
                n++;
                cell.style.color = "red";
                cell.style.width = "72px";
                //if (time_tubes[y].totalAmount == 0 && time_tubes[y].hasLoss == true) {
                //    cell.innerHTML = "0+Loss";
                //} else {
                //    cell.innerHTML = "0";
                //}
                cell.innerHTML = "0";
            }
            y++; //下一個時間區間(因為time_tubes沒有"小計"，所以要另外用變數)
        }
    }

    //輸出量總計 (所有班別總計)
    //新一行
    if (backgroundColor == '#E1E6FA') {
        backgroundColor = '#ABC8E2';
    } else {
        backgroundColor = '#E1E6FA';
    }

    var itemRow = table.insertRow(-1);  //加入新row
    itemRow.className = "GridViewScrollItem";
    itemRow.style.backgroundColor = backgroundColor;
    cell = itemRow.insertCell(0);
    cell.colSpan = 2;  //水平合併
    cell.style.textAlign = "center";
    cell.innerHTML = "輸出量總計" + "<br><input type='button' value='帶入護理記錄' style='word-break:break-all;overflow:hidden;font-size: 12px;margin-top: 10px;white-space: pre-wrap;' onclick='Take_CareRecord(\"\", \"" + inputDate + "\",\"2\",\"\")'>";

    cell = itemRow.insertCell(1);
    cell.innerHTML = "Record day total 引流管輸出量： " + DEN_txt;
    cell.style.textAlign = "left";
    cell.colSpan = inquire_tubes.length + 1;  //水平合併
    cell.id = "AllTotal";
}
//***************建立空表格***************//
function buildEmptyTable(table_id, inputDate, jobBlocks) {
    var table = document.getElementById(table_id);
    var inputDate_arr = inputDate.split("/");

    //***************大標題***************//
    var headerRow = table.insertRow(-1);  //加入新row
    headerRow.className = "GridViewScrollHeader";

    var cell = headerRow.insertCell(0);
    cell.innerHTML = "引流管";
    //cell.color = "#85A5CC";
    cell.className = "HideCell";
    cell.colSpan = 2; //水平合併

    cell = headerRow.insertCell(1);
    cell.innerHTML = "引流管";
    cell.id = "InquireTubeTitle";

    //合計欄位
    cell = headerRow.insertCell(2);
    cell.rowSpan = 2; //垂直合併
    cell.innerHTML = "合計";
    cell.style.width = "72px";
    cell.id = "timeblockTotalTitle";


    //***************次標題***************//
    headerRow = table.insertRow(-1);  //加入新row
    headerRow.className = "GridViewScrollHeader";

    cell = headerRow.insertCell(0);
    cell.innerHTML = "班別";
    cell.style.width = "70px";
    //cell.style.width = "10%";
    //cell.style.height = "20px";  //空表格的次標題高度不要設置，不然IE顯示有問題
    cell.className = "jobBlocksTitle";

    cell = headerRow.insertCell(1);
    cell.innerHTML = "時間\\管路";
    //cell.style.width = "10%";
    cell.style.width = "53px";
    cell.className = "timeBlocksTitle";

    //所有引流管
    cell = headerRow.insertCell(2);
    cell.innerHTML = "";
    cell.style.textAlign = "center";
    cell.id = "inquire_tube" + 0;

    //內容
    var jobi = 0;
    var backgroundColor = '#ABC8E2';  //偶數
    //#E1E6FA 奇數

    for (i = 0; i < timeBlocks.length; i++) {
        if (i == 0 || i % 2 == 0) {
            backgroundColor = '#E1E6FA'; //#E1E6FA 奇數行色
        } else {
            backgroundColor = '#ABC8E2'; //偶數行色
        }

        //新一行
        var itemRow = table.insertRow(-1);  //加入新row
        itemRow.className = "GridViewScrollItem";
        itemRow.style.backgroundColor = backgroundColor;
        var n = 0;//橫的第幾個欄位 cell = itemRow.insertCell(n);

        //班別	
        if (i == timeBlocks_start_index[0]) { //白班
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = inputDate + "<br>" + jobBlocks[jobi];
            cell.rowSpan = timeBlocks_total_index[0] + 1; //垂直合併
            cell.style.textAlign = "center";
            cell.style.width = "70px";
            cell.style.backgroundColor = "#85A5CC";
            cell.id = "ClassD";
            jobi++;

        } else if (i == timeBlocks_start_index[1]) { //小夜
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = inputDate + "<br>" + jobBlocks[jobi];
            cell.rowSpan = timeBlocks_total_index[0] + 1; //垂直合併
            cell.style.textAlign = "center";
            cell.style.width = "70px";
            cell.style.backgroundColor = "#85A5CC";
            cell.id = "ClassE";
            jobi++;

        } else if (i == timeBlocks_start_index[2]) { //大夜
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = inputDate + "<br>" + jobBlocks[jobi];
            cell.rowSpan = timeBlocks_total_index[0] + 1; //垂直合併
            cell.style.textAlign = "center";
            cell.style.width = "70px";
            cell.style.backgroundColor = "#85A5CC";
            cell.id = "ClassN";
            jobi++;

        }

        //時間區間
        cell = itemRow.insertCell(n);
        n++;
        cell.innerHTML = timeBlocks[i];
        //cell.style.width = "10%";
        cell.style.width = "53px";
        cell.style.backgroundColor = backgroundColor;

        var totalAmount = 0;

        //引流管數值&小計
        if (i == timeBlocks_total_index[0]) {  //白班小計
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = "";
            cell.style.textAlign = "left";
            cell.colSpan = 6;  //水平合併
            cell.id = "subtotal_by_TempClass_D";

        } else if (i == timeBlocks_total_index[1]) {  //小夜班小計
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = "";
            cell.style.textAlign = "left";
            cell.colSpan = 6;  //水平合併
            cell.id = "subtotal_by_TempClass_E";

        } else if (i == timeBlocks_total_index[2]) {  //大夜班小計
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = "";
            cell.style.textAlign = "left";
            cell.colSpan = 6;  //水平合併
            cell.id = "subtotal_by_TempClass_N";

        } else {
            //空引流管
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = "";
            cell.style.textAlign = "center";

            //合計
            cell = itemRow.insertCell(n);
            n++;
            cell.innerHTML = "";
            cell.style.color = "red";
            cell.style.textAlign = "center";
            cell.style.width = "72px";
            cell.id = "timeblockTotal" + i;
        }
    }

    //輸出量總計 (所有班別總計)
    //新一行
    if (backgroundColor == '#E1E6FA') {
        backgroundColor = '#ABC8E2';
    } else {
        backgroundColor = '#E1E6FA';
    }

    var itemRow = table.insertRow(-1);  //加入新row
    itemRow.className = "GridViewScrollItem";
    itemRow.style.backgroundColor = backgroundColor;
    cell = itemRow.insertCell(0);
    cell.colSpan = 2;  //水平合併
    cell.style.textAlign = "center";
    cell.innerHTML = "輸出量總計";

    cell = itemRow.insertCell(1);
    cell.innerHTML = "";
    cell.style.textAlign = "left";
    cell.colSpan = 6;  //水平合併
    cell.id = "AllTotal";
}

//***************引流管每日攝出page計算***************//
function caculateInquireTubes(table_id, inquire_tubes_record, shiftClassD, shiftClassE, shiftClassN, timeBlock_hours, inputDate, unit, hasData) {
    var inputDate_arr = inputDate.split("/");
    if (hasData) {
        getAllInquireTubes(inquire_tubes_record);//獲取所有引流管名稱、ID(不重複)
        getAllTimeBlocks(shiftClassD, shiftClassE, shiftClassN, timeBlock_hours); //獲取所有時間區間
        caculateDENAmount(shiftClassD, shiftClassE, shiftClassN, inquire_tubes_record, inputDate_arr, unit);   //計算三個班別內及所有班別，每個引流管流量(單日)
        getTimeTubesArray(inquire_tubes_record, inputDate_arr);  //計算所有時間區間內，每個引流管流量(單日)
    } else {
        getAllTimeBlocks(shiftClassD, shiftClassE, shiftClassN, timeBlock_hours); //獲取所有時間區間
    }
}


//caculateInquireTubes(myObj, shiftClassD, shiftClassE, shiftClassN, timeBlock_hours, inputDate);//引流管處理
//buildTable("gvMain", inputDate); //建立表格

//var result = getDENTotalTxt(shiftClassD, shiftClassE, shiftClassN, myObj, inputDate, "All", unitStr[0]);//輸出四個引流管總計(白班D,小夜E,大夜N,只有總計Total, 全部All)
//console.log(result);