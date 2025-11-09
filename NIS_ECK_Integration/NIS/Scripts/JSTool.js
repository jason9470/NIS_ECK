
var myControl = {
    create: function (tp_inst, obj, unit, val, min, max, step) {
        $('<input class="ui-timepicker-input" value="' + val + '" style="width:50%">')
        .appendTo(obj)
        .spinner({
            min: min,
            max: max,
            step: step,
            change: function (e, ui) { // key events
                // don't call if api was used and not key press
                if (e.originalEvent !== undefined)
                    tp_inst._onTimeChange();
                tp_inst._onSelectHandler();
            },
            spin: function (e, ui) { // spin events
                tp_inst.control.value(tp_inst, obj, unit, ui.value);
                tp_inst._onTimeChange();
                tp_inst._onSelectHandler();
            }
        });
        return obj;
    },
    options: function (tp_inst, obj, unit, opts, val) {
        if (typeof (opts) == 'string' && val !== undefined)
            return obj.find('.ui-timepicker-input').spinner(opts, val);
        return obj.find('.ui-timepicker-input').spinner(opts);
    },
    value: function (tp_inst, obj, unit, val) {
        if (val !== undefined)
            return obj.find('.ui-timepicker-input').spinner('value', val);
        return obj.find('.ui-timepicker-input').spinner('value');
    }
};

// 置換特殊符號
function SymbolReplace(orgString) {
    return orgString.replace(/%/g, "%25").replace(/\&/g, "%26").replace(/\+/g, "%2B");
}

// JS 去除空白
function Trim(str) {
    var start = -1, end = str.length;
    while (str.charCodeAt(--end) < 33);
    while (str.charCodeAt(++start) < 33);
    return str.slice(start, end + 1);
}

// 驗證
function Validate(buttom_name, form_name) {

    $("input[name=" + buttom_name + "]").click(function () {

        var passFlag = true;
        $("[fieldcheck]").each(function () {
            var cValue = Trim($(this).val());
            var valResult = true;

            switch ($(this).attr("fieldcheck")) {
                case "Null":    //驗證空值
                    if (cValue == "" || cValue.length == 0 || cValue == null)
                        valResult = false;
                case "Number":  //驗證數字
                    re = /^[0-9]+(\.[0-9])?$/;
                    if (!re.test(cValue))
                        valResult = false;
                    break;
                case "Datetime":    //驗證日期

                    break;
            }
            if (!valResult) {
                $(this).css("background-color", "#FFFF77");
                passFlag = false;
            } else {
                $(this).css("background-color", "");
            }
        });

        if (!passFlag) {
            showHint("資料驗證未過");
            return false;
        } else {
            $("form[name=" + form_name + "]").submit();
        }
    });

}

// 秀出訊息
function showHint(info) {

    $("#hintinfo").html(info).stop(true, true).show(0, function () {
        $(this).fadeOut(3000)
    });
}

// 左邊補字
function padLeft(str, lenght, addStr) {
    if (str.toString().length >= lenght)
        return str;
    else
        return padLeft(addStr + str.toString(), lenght, addStr);
}

// 右邊補字
function padRight(str, lenght, addStr) {
    if (str.toString().length >= lenght)
        return str;
    else
        return padRight(str.toString() + addStr, lenght, addStr);
}



//加法
function addition(num1, num2) {
    if (num1 == null || num2 == null)
        return false;
    var r1, r2, m;
    try { r1 = num1.toString().split(".")[1].length } catch (e) { r1 = 0 }
    try { r2 = num2.toString().split(".")[1].length } catch (e) { r2 = 0 }
    m = Math.pow(10, Math.max(r1, r2));
    return (num1 * m + num2 * m) / m;
}

//乘法
function multiplication(num1, num2) {
    if (num1 == null || num2 == null)
        return;
    var m = 0, s1 = num1.toString(), s2 = num2.toString();
    //try { m += s1.split(".")[1].length } catch (e) { m += 0 }
    //try { m += s2.split(".")[1].length } catch (e) { m += 0 }
    return Number(s1.replace(".", "")) * Number(s2.replace(".", "")) / Math.pow(10, m)
}

//除法
function division(num1, num2) {
    if (num1 == null || num2 == null)
        return;
    var t1 = 0, t2 = 0, r1, r2;
    //try { t1 = num1.toString().split(".")[1].length } catch (e) { }
    //try { t2 = num2.toString().split(".")[1].length } catch (e) { }
    with (Math) {
        r1 = Number(num1.toString().replace(".", ""))
        r2 = Number(num2.toString().replace(".", ""))
        return (r1 / r2) * pow(10, t2 - t1);
    }
}

// 移動元件的scroll bar 到底(bottom)或是到頂(top)
function MoveScroll(targetObj, direction) {
    switch (direction) {
        case "top":
            $(targetObj).animate({ scrollTop: 0 }, 0);
            break;
        case "bottom":
            $(targetObj).animate({ scrollTop: 9999 }, 0);
            break;
    }
}

function textIsEmpty(str) { //判斷字串是否為空字串、空白、未定義、null
    var isEmpty = false;
    if (str == null) {
        isEmpty = true;
    } else {
        if (typeof str != "number") {
            if (str != null && typeof str != "undefined") {
                if (str.replace(/\s/g, '').length == 0) {
                    isEmpty = true;
                }
            } else {//str為 undefined 
                isEmpty = true;
            }
        }
    }
    return isEmpty; //若為空則回傳true
}

function formatDate(date) {

    if (date.getTime() < -1325664000000) {
        date = new Date(date.getTime() + (1325664352000 - 1325664000000));
    };
    
    var retStr = date.getFullYear();
    retStr += "/" + padLeft((date.getMonth() + 1).toString(), 2, "0");
    retStr += "/" + padLeft(date.getDate().toString(), 2, "0");
    retStr += " " + padLeft(date.getHours().toString(), 2, "0");
    retStr += ":" + padLeft(date.getMinutes().toString(), 2, "0");
    retStr += ":" + padLeft(date.getSeconds().toString(), 2, "0");

    return retStr;
}


function createForm( url, method){
    var form = document.createElement("form");
    form.setAttribute("method", method);
    form.setAttribute("action", url);
    return form;
}

function appendField(formObj, type, name, value) {
    var fieldObj = document.createElement("input");
    fieldObj.setAttribute("type", type);
    fieldObj.setAttribute("name", name);
    fieldObj.setAttribute("value", value);
    formObj.appendChild(fieldObj);
}

function getBrowserInfo() {
    bwObj = {
        version: null,
        type: null,
        name: null,
        desc: null
    };

    var browser_list = new Array("firefox", "msie", "chrome", "opera", "safari");
    var browser_name = new Array("FireFox", "IE", "Chrome", "Opera", "Safari");

    // 先給完整訊息
    bwObj.desc = navigator.userAgent;
    // 判斷瀏覽器別
    for (var i = 0; i <= browser_list.length - 1; i++) {
        if (navigator.userAgent.toLowerCase().match(browser_list[i]) == browser_list[i]) {
            bwObj.type = browser_list[i];
            bwObj.name = browser_name[i];
            break;
        }
    }
    // 判斷版本
    switch (bwObj.type) {
        case browser_list[0]:
            var st_idx = navigator.userAgent.toLowerCase().indexOf(browser_list[0]);
            var ver_word = navigator.userAgent.substring(st_idx, navigator.userAgent.length).split('/');
            bwObj.version = ver_word[1];
            break;
        case browser_list[1]:
            var st_idx = navigator.userAgent.toLowerCase().indexOf(browser_list[1]);
            var ver_word = navigator.userAgent.substring(st_idx, navigator.userAgent.length);
            var c_word = ver_word.split(';');
            var b_word = c_word[0].split(" ");
            bwObj.version = b_word[1];
            break;
        default:
            bwObj.version = bwObj.desc;
            break;
    }

    return bwObj;
}

function lock_ctl_n() {
    var ctrlDown = false;
    var ctrlKey = 17, nKey = 78;
    document.onkeydown = function (e) {
        if (!e) var e = window.event;
        if (e.keyCode == ctrlKey) ctrlDown = true;
        if (ctrlDown && (e.keyCode == nKey)) return false;
    }
    document.onkeyup = function (e) {
        if (!e) var e = window.event;
        if (e.keyCode == ctrlKey) ctrlDown = false;
    }
}

function close_pup_window() {
    window.onunload = function () {
        for (var i = 0; i < new_window.length; i++) {
            new_window[i].close();
        };
    };
}


function openBrowser(url, name, keys, values) {
    var newWindow = window.open(url, name);
    if (!newWindow) return false;
    var html = "";
    html += "<html><head></head><body><form id='formid' method='post' action='" + url + "'>";
    if (keys && values && (keys.length == values.length))
        for (var i = 0; i < keys.length; i++)
            html += "<input type='hidden' name='" + keys[i] + "' value='" + values[i] + "'/>";
    html += "</form>";
    html += "<script type='text/javascript'>document.getElementById(\"formid\").submit();</script></body></html>";
    newWindow.document.write(html);
    return newWindow;
}

$.fn.extend({
    insertAtCaret: function (myValue) {
        if (document.selection) {
            this.focus();
            sel = document.selection.createRange();
            sel.text = myValue;
            this.focus();
        }
        else if (this.selectionStart || this.selectionStart == '0') {
            var startPos = this.selectionStart;
            var endPos = this.selectionEnd;
            var scrollTop = this.scrollTop;
            this.value = this.value.substring(0, startPos) + myValue + this.value.substring(endPos, this.value.length);
            this.focus();
            this.selectionStart = startPos + myValue.length;
            this.selectionEnd = startPos + myValue.length;
            this.scrollTop = scrollTop;
        } else {
            this.value += myValue;
            this.focus();
        }
    }
})

//複製到剪貼簿
function copy_clip(meintext) {
    if (window.clipboardData) {
        window.clipboardData.setData("Text", meintext);
    }
    else if (window.netscape) {
        netscape.security.PrivilegeManager.enablePrivilege('UniversalXPConnect');
        var clip = Components.classes['@mozilla.org/widget/clipboard;1'].createInstance(Components.interfaces.nsIClipboard);
        if (!clip) return;
        var trans = Components.classes['@mozilla.org/widget/transferable;1'].createInstance(Components.interfaces.nsITransferable);
        if (!trans) return;
        trans.addDataFlavor('text/unicode');
        var str = new Object();
        var len = new Object();
        var str = Components.classes["@mozilla.org/supports-string;1"].createInstance(Components.interfaces.nsISupportsString);
        var copytext = meintext;
        str.data = copytext;
        trans.setTransferData("text/unicode", str, copytext.length * 2);
        var clipid = Components.interfaces.nsIClipboard;
        if (!clip) return false;
        clip.setData(trans, null, clipid.kGlobalClipboard);
    }
    return false;
}

//取得真實高與寬
(function ($) {
    var getPropIE = function (name) {
        return Math.max(
            document.documentElement["client" + name],
            document.documentElement["scroll" + name],
            document.body["scroll" + name]
        );
    }
    $.fn.extend({
        trueWidth: function () {
            return ((/msie/.test(navigator.userAgent.toLowerCase()) && this.get()[0].nodeType === 9) ? getPropIE('Width') : this.width());
        },
        trueHeight: function () {
            return ((/msie/.test(navigator.userAgent.toLowerCase()) && this.get()[0].nodeType === 9) ? getPropIE('Height') : this.height());
        }
    });
})(jQuery);

// 檢查Session是否為正確的病人
function check_session() {
    var paramdata = {};
    var now_add = moment().add(30, 'minutes').format("YYYY/MM/DD HH:mm");
    var now_reduce = moment().add(-30, 'minutes').format("YYYY/MM/DD HH:mm");
    paramdata.user_id = "";
    paramdata.fee_no = "";
    if ($("#leftMenuPanel").attr("userid") != undefined)
        paramdata.user_id = $("#leftMenuPanel").attr("userid");
    if ($(".patmain").attr("feeno") != undefined)
        paramdata.fee_no = $(".patmain").attr("feeno");
    if ($("#Guiderid").val() != "")
        paramdata.Guiderid = $("#Guiderid").val();


    $.ajax({
        url: "SessionCheck",
        type: "post", timeout: 10000,
        data: "fee_no=" + paramdata.fee_no + "&user_id=" + paramdata.user_id + "&Guiderid=" + paramdata.Guiderid
            + "&now_add=" + now_add + "&now_reduce=" + now_reduce,
        success: function (data) {
            if (data == "C") {
                $.ajax({
                    url: "SetPatient", timeout: 5000, type: "post", data: "fee_no=" + paramdata.fee_no,
                    complete: function () {
                        loadFav();
                    },
                    error: function () {
                        showHint("病人資料更新失敗");
                    }
                });
            }
            else if (data == "T") {
                logout('登入逾時');
            }
        }
    });
}

// 檢查Session是否為正確的病人
function check_userdate() {
    var now_add = moment().add(30, 'minutes').format("YYYY/MM/DD HH:mm");
    var now_reduce = moment().add(-30, 'minutes').format("YYYY/MM/DD HH:mm");

    $.ajax({
        url: "../Main/UserDateCheck",
        type: "post", timeout: 10000,
        data: "now_add=" + now_add + "&now_reduce=" + now_reduce,
        success: function (data) {
            if (data == "E") {// SERVER與本機時間是否對應
                logout('伺服器與本機時間不符，請調整後再登入');
            }
        }
    });
}
function logout(alert_str) {
    location.href = "../Main/Login?message=" + encodeURIComponent(alert_str);
}
function loop_windows(funName) {
    var dom = window;
    var old = null;
    var fun = null;
    while (!(fun = dom[funName]) && dom != old) {
        old = dom;
        dom = dom.parent;
    }

    if (fun) {
        var params = Array.prototype.slice.call(arguments); //arguments to array
        params = params.slice(1); //只抓第二個參數之後的，第一個參數是Action種類
        fun.apply(this, params);
    }
}

//將文字做HTML編碼
function htmlEncode(value) {
    //return !value ? value : String(value).replace(/&/g, "&amp;").replace(/\"/g, "&quot;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    return he.encode(value, {
        //若有名稱則用名稱，如 &nbsp;、&amp...等，以縮短字串長度
        "useNamedReferences": true
    });
}

if (!Array.prototype.distinct) {
    // 新增使得Array多了distinct功能 by 家雄
    Array.prototype.distinct = function () {
        var arr = [];
        for (var i = 0; i < this.length; i++) {
            if (!arr.includes(this[i])) {
                arr.push(this[i]);
            }
        }
        return arr;
    };
}

