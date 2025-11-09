var currObj;
var regex = /<br\s*[\/]?>/gi;
var preObj;
var x = 0;

//取得Input屬性
$.fn.getType = function () { return this[0].tagName == "INPUT" ? this[0].type.toLowerCase() : this[0].tagName.toLowerCase(); }

//設定顏色
function color_setting(obj) {
    $(obj).gridColor();
}

//移動到下一個選項
function next_tab() {
    var cIndex = parseInt($(currObj).attr("tabindex")) + 1;
    currObj = $("input[tabindex='" + cIndex + "']");
    if (!$(currObj).is(":hidden")) {
        if ($(currObj).length != 0) {
            if (preObj != $(currObj).parent("td").parent("tr").parent("tbody").attr("id")) {
                var top = parseFloat($(currObj).parent("td").parent("tr").parent("tbody").offset().top) - 15;
                $(".dataArea").animate({ scrollTop: top }, 0, "swing", function () {
                    $(currObj).focus();
                });
            } else {
                $(currObj).focus();
            }
            preObj = $(currObj).parent("td").parent("tr").parent("tbody").attr("id");
        } else {
            $(currObj).focus();
        }
    } else {
        next_tab();
    }
}

//欄位帶入
function carry_in_info(obj) {
    var lfn = $(obj).attr("lo_check");
    var hfn = $(obj).attr("hi_check");
    var current_model_name = $(obj).attr("cr_model");
    //var now_item = encodeURIComponent($("input[name='" + current_model_name + "_memo']").attr("title").replace(/<br \/>/g, "|"));
    var other_memo = null;
    //if ($("input[name='" + current_model_name + "_other_memo']").length != 0) {
    //    other_memo = encodeURIComponent($("input[name='" + current_model_name + "_other_memo']").attr("title").replace(/<br \/>/g, "|"));
    //}
    var now_value = parseFloat($(obj).val());

    //var paramStr = "lfn=" + lfn + "&hfn=" + hfn + "&cvalue=" + now_value + "&model_name=" + current_model_name + "&now_item=" + now_item;
    var paramStr = "lfn=" + lfn + "&hfn=" + hfn + "&cvalue=" + now_value + "&model_name=" + current_model_name + "&now_item=";
    if (other_memo != null) {
        paramStr += "&other_memo=" + other_memo
    }

    $.ajax({
        url: "../VitalSign/VitalSignPrompt",
        type: "post",
        timeout: 5000,
        data: paramStr,
        success: function (data) {
           
           
            if (Trim(data) != "") {
                $('#VS_plan_' + current_model_name).prop('checked', true);
                $("#vs_prompt").html(data);
                var keybordHeight = $("#vs_prompt").outerHeight();
                var keybordWidth = $("#vs_prompt").outerWidth();
                var top = $(currObj).position().top + $(currObj).height() + 10;
                var left = $(currObj).position().left;
                if ((top + keybordHeight) > $(window).outerHeight())
                    top = $(obj).position().top - keybordHeight - 5;
                if ((left + keybordWidth) > $(window).outerWidth())
                    left = $(obj).position().left - (keybordWidth - $(obj).outerWidth());
              
                $("#vs_prompt").css({
                    "top": top,
                    "left": left
                }).slideDown(350);
            } else {
               $('#VS_plan_' + current_model_name).prop('checked', false);
                next_tab();
            }
        },
        error: function () {
            showHint("資料擷取失敗，請聯絡資訊室");
        }
    });
}

// 將資料帶入處置
function carrin_data(objname) {
    var cr_obj = $("input[name='" + objname + "_memo']");
    var cr_other_obj = $("input[name='" + objname + "_other_memo']")
    var cr_str = "", cr_other_str = "";
    $("#vs_prompt #main_memo").find("input[type=checkbox]:checked").each(function () {
        cr_str += $(this).val() + "，";
    });
    if ($("#" + objname + "_memo").val() != "")
        $("#" + objname + "_memo").val($("#" + objname + "_memo").val() + ',' + cr_str.substring(0, cr_str.length - 1));
    else
        $("#" + objname + "_memo").val($("#" + objname + "_memo").val() + cr_str.substring(0, cr_str.length - 1));
    if (typeof (cr_other_obj) !== 'undifined') {
        $("#vs_prompt #other_memo").find("input[type=checkbox]:checked").each(function () {
            cr_other_str += $(this).val() + "<br />";
        });
        $(cr_other_obj).attr("title", cr_other_str);
    }
    $("#vs_prompt").slideUp(350);
    next_tab();
}

function setToolTip() {
    // 設定tooltip    
    $(".tooltip").prop("readonly", true).tooltip({
        content: function () { return $(this).attr("title"); }
    });
}

function mt_select_setting() {
    $("input[relate_field]").each(function () {
        var rel_list = $(this).attr("relate_field").split('|');
        var self_obj = $(this);       
        for (var i = 0; i <= rel_list.length - 1; i++) {            
            $("input[name='" + rel_list[i] + "']").unbind('click').click(function () {               
                var c_value = new Array();
                for (var j = 0; j <= rel_list.length - 1; j++) {                  
                    c_value[j] = $("input[name='" + rel_list[j] + "']:checked").val();
                }
                $(self_obj).val(c_value.join('|'));
                //showHint($(self_obj).val());    
               
            });
        }
    });

    //$("input[relate_field]").each(function () {
    //    var rel_list = $(this).attr("relate_field").split('|');
    //    var self_obj = $(this);
    //    for (var i = 0; i <= rel_list.length - 1; i++) {
    //        $("input:checkbox[name='" + rel_list[i] + "'],input:radio[name='" + rel_list[i] + "']").unbind('click').click(function () {

    //            var c_value = new Array();
    //            for (var j = 0; j <= rel_list.length - 1; j++) {
    //                var input_type = $("input[name='" + rel_list[j] + "']").getType();
    //                if (input_type == "text") {
    //                    c_value[j] = $("input[name='" + rel_list[j] + "']").val();
    //                } else {
    //                    c_value[j] = $("input[name='" + rel_list[j] + "']:checked").val();
    //                }
    //            }
    //            $(self_obj).val(c_value.join('|'));
    //            //showHint($(self_obj).val());    
    //        });
    //        $("input:text[name='" + rel_list[i] + "']").unbind('keyup').keyup(function () {
    //            var c_value = new Array();
    //            for (var j = 0; j <= rel_list.length - 1; j++) {
    //                var input_type = $("input[name='" + rel_list[j] + "']").getType();
    //                if (input_type == "text") {
    //                    c_value[j] = $("input[name='" + rel_list[j] + "']").val();
    //                } else {
    //                    c_value[j] = $("input[name='" + rel_list[j] + "']:checked").val();
    //                }
    //            }
    //            $(self_obj).val(c_value.join('|'));
    //            //showHint($(self_obj).val());    

    //        });
    //    }
    //});
}

function met_check()
{
    var message = "";
    var met_score = 0;
    var tmp_score = 0;
    var sbp = "";
    var hr = "";
    var rr = "";  
    var t = $('#bph_text').val();
    var f = $('#bf_text').val();
    if ($('#bph_text').val() != "" && $('#bph_text').val() > 89 && $('#bph_text').val() < 100) {
        tmp_score = 1;
        if (tmp_score > met_score) {
            met_score = tmp_score;
        }
        sbp = "SBP: " + $('#bph_text').val() + " mmHg,";
    }
    if ($('#bph_text').val() != "" && (($('#bph_text').val() > 79 && $('#bph_text').val() < 90) || $('#bph_text').val() > 180)) {
        tmp_score = 2;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        sbp = "SBP: " + $('#bph_text').val() + " mmHg,";
    }
    if ($('#bph_text').val() != "" && $('#bph_text').val() > 69 && $('#bph_text').val() < 80) {
        tmp_score = 3;
        if(tmp_score > met_score)
        {
            met_score = tmp_score;            
        }
        sbp = "SBP: " + $('#bph_text').val() + " mmHg,";
    }
    if ($('#bph_text').val() != "" && $('#bph_text').val() < 70) {
        tmp_score = 4;
        if (tmp_score >= met_score) {
            met_score = tmp_score;            
        }
        sbp = "SBP: " + $('#bph_text').val() + " mmHg,";
    }
    if ($('#bf_text').val() != "" && $('#bf_text').val() < 26 && $('#bf_text').val() > 20) {
        tmp_score = 1;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        rr = "RR: " + $('#bph_text').val() + " 次/分,";
    }
    if ($('#bf_text').val() != "" && $('#bf_text').val() < 31 && $('#bf_text').val() > 25) {
        tmp_score = 2;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        rr = "RR: " + $('#bph_text').val() + " 次/分,";
    }
    if ($('#bf_text').val() != "" && (($('#bf_text').val() < 9 && $('#bf_text').val() > 4) || ($('#bf_text').val() < 36 && $('#bf_text').val() > 30))) {
        tmp_score = 3;
        if (tmp_score >= met_score) {
            met_score = tmp_score;            
        }
        rr = "RR: " + $('#bph_text').val() + " 次/分,";
    }
    if ($('#bf_text').val() != "" && ($('#bf_text').val() < 5 || $('#bf_text').val() > 35)) {
        tmp_score = 4;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        rr = "RR: " + $('#bph_text').val() + " 次/分,";
    }
    if ($('#mp_text').val() != "" && $('#mp_text').val() < 121 && $('#mp_text').val() > 100) {
        tmp_score = 1;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        hr = "HR: " + $('#mp_text').val() + " 次/分,";
    }
    if ($('#mp_text').val() != "" && $('#mp_text').val() < 141 && $('#mp_text').val() > 120) {
        tmp_score = 2;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        hr = "HR: " + $('#mp_text').val() + " 次/分,";
    }
    if ($('#mp_text').val() != "" && (($('#mp_text').val() < 50 && $('#mp_text').val() > 39) || ($('#mp_text').val() < 161 && $('#mp_text').val() > 140))) {
        tmp_score = 3;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        hr = "HR: " + $('#mp_text').val() + " 次/分,";
    }
    if ($('#mp_text').val() != "" && ($('#mp_text').val() < 40 || $('#mp_text').val() > 160)) {
        tmp_score = 4;
        if (tmp_score >= met_score) {
            met_score = tmp_score;
        }
        hr = "HR: " + $('#mp_text').val() + " 次/分,";
    }
    if (met_score == 1)
    {
        message += "早期警示評分：" + met_score + " 分\n";
        message += hr + sbp + rr + "\n";
        message += "依醫囑觀察病人生命徵象。";
    }
    if (met_score == 2) {
        message += "早期警示評分：" + met_score + " 分\n";
        message += hr + sbp + rr + "\n";
        message += "1. 依醫囑觀察病人生命徵象。\n";
        message += "2. 通知第一線醫師/專科護理師。";
    }
    if (met_score == 3) {
        message += "早期警示評分：" + met_score + " 分\n";
        message += hr + sbp + rr + "\n";
        message += "1. 依醫囑觀察病人生命徵象。\n";
        message += "2. 通知第一線醫師/專科護理師。\n";
        message += "3. 系統發出簡訊通知主治醫師。";
    }
    if (met_score == 4) {
        message += "早期警示評分：" + met_score + " 分\n";
        message += hr + sbp + rr + "\n";
        message += "1. 依醫囑觀察病人生命徵象。\n";
        message += "2. 通知第一線醫師/專科護理師。\n";
        message += "3. 系統發出簡訊通知主治醫師及 MET 小組。";
    }
    return message;
}

//-----2016/05/27 Vanda Add
function CheckComa() {
    var HasValue = false;
    if ($('#gc').is(':visible')) {
        for (var i = 1; i <= 3; i++) {
            if ($('input[name=gc_r' + i.toString() + ']:checked').val() != null) {
                HasValue = true;
                break;
            }
        }
        if (HasValue) {
            for (var j = 1; j <= 3; j++) {
                if ($('input[name=gc_r' + j.toString() + ']:checked').val() == null) {
                    return false;
                }
            }
        }
    }
    return true;
}
//疼痛檢查
function CheckPain() {
    var HasValue = false;
    var PainTool = $('input[name=ps_assess]:checked').val();
    var PainItemNum = 0; //評估數量
    switch (PainTool) {
        case 'CPOT評估(加護單位)':
            PainItemNum = 4;
            break;
        default:
            PainItemNum = 5;
            break;
    }
    if ($('#ps').is(':visible')) {
        for (var i = 1; i <= PainItemNum; i++) {
            if ($('input[name=ps_r' + i.toString() + ']:checked').val() != null) {
                HasValue = true;
                break;
            }
        }
        if (HasValue) {
            for (var j = 1; j <= PainItemNum; j++) {
                if ($('input[name=ps_r' + j.toString() + ']:checked').val() == null) {
                    return false;
                }
            }
        }
    }
    return true;
}
//疼痛檢查
function CheckPain() {
    var HasValue = false;
    var PainTool = $('input[name=ps_assess]:checked').val();
    var PainItemNum = 0; //評估數量
    switch (PainTool) {
        case 'CPOT評估(加護單位)':
            PainItemNum = 4;
            break;
        default:
            PainItemNum = 5;
            break;
    }
    if ($('#ps').is(':visible')) {
        for (var i = 1; i <= PainItemNum; i++) {
            if ($('input[name=ps_r' + i.toString() + ']:checked').val() != null) {
                HasValue = true;
                break;
            }
        }
        if (HasValue) {
            for (var j = 1; j <= PainItemNum; j++) {
                if ($('input[name=ps_r' + j.toString() + ']:checked').val() == null) {
                    return false;
                }
            }
        }
    }
    return true;
}

//瞳孔反應	
function pupils(type) {
    switch (type) {
        case 'L1':
            $('#pupils_l_num').show();
            $('#pupils_l_record').hide().val('');
            break;
        case 'L2':
            $('#pupils_l_num').show();
            $('#pupils_l_record').hide().val('');
            break;
        case 'L3':
            $('#pupils_l_num').show();
            $('#pupils_l_record').hide().val('');
            break;
        case 'L4':
            $('#pupils_l_num').hide().find('input[type=radio]').prop('checked', false);
            $('#pupils_l_record').hide().val('');
            break;
        case 'L5':
            $('#pupils_l_num').hide().find('input[type=radio]').prop('checked', false);
            $('#pupils_l_record').show();
            break;
        case 'L6':
            $('#pupils_l_num').hide().find('input[type=radio]').prop('checked', false);
            $('#pupils_l_record').hide().val('');
            break;
        case 'R1':
            $('#pupils_r_num').show();
            $('#pupils_r_record').hide().val('');
            break;
        case 'R2':
            $('#pupils_r_num').show();
            $('#pupils_r_record').hide().val('');
            break;
        case 'R3':
            $('#pupils_r_num').show();
            $('#pupils_r_record').hide().val('');
            break;
        case 'R4':
            $('#pupils_r_num').hide().find('input[type=radio]').prop('checked', false);
            $('#pupils_r_record').hide().val('');
            break;
        case 'R5':
            $('#pupils_r_num').hide().find('input[type=radio]').prop('checked', false);
            $('#pupils_r_record').show();
            break;
        case 'R6':
            $('#pupils_r_num').hide().find('input[type=radio]').prop('checked', false);
            $('#pupils_r_record').hide().val('');
            break;
        default:
    }
}
//瞳孔大小其他項目  (已拿掉)
//function pupils_size(type, direction) {
//    switch (direction) {
//        case 'R':
//            switch (type) {
//                case '其他':
//                    $('#pupils_size_r_record').show();
//                    break;
//                default:
//                    $('#pupils_size_r_record').hide().val('');
//                    break;
//            }
//            break;
//        case 'L':
//            switch (type) {
//                case '其他':
//                    $('#pupils_size_l_record').show();
//                    break;
//                default:
//                    $('#pupils_size_l_record').hide().val('');
//                    break;
//            }
//            break;
//    }
//}
//--------

//肌肉強度
//function msPower(type, value) {
//    switch (type) {
//        case 'UP_L':
//            if (value == '其他') {
//                $('#msPower_upper_l_record').show().val('');
//            } else {
//                $('#msPower_upper_l_record').hide().val('');
//            }
//            break;
//        case 'UP_R':
//            if (value == '其他') {
//                $('#msPower_upper_r_record').show().val('');
//            } else {
//                $('#msPower_upper_r_record').hide().val('');
//            }
//            break;
//        case 'LOW_L':
//            if (value == '其他') {
//                $('#msPower_lower_l_record').show().val('');
//            } else {
//                $('#msPower_lower_l_record').hide().val('');
//            }
//            break;
//        case 'LOW_R':
//            if (value == '其他') {
//                $('#msPower_lower_r_record').show().val('');
//            } else {
//                $('#msPower_lower_r_record').hide().val('');
//            }
//            break;
//        default:
//            break;
//    }
//}
//-----

$(document).ready(function () {

    // 該藏的藏起來
    $("#vs_table tbody[hide='Y']").each(function () {
        try {
            $(this).find("input[type=hidden][name=vs_item]").prop("disabled", true);
            $(this).hide();
        } catch (err) {
            showHint(err);
        }
    });

    // 自動指定tabIndex
    $(".dataArea input").autoTabIndex();

    // 設定tooltip    
    setToolTip();

    //設定數字驗證
    $('input[setindex=Y]').each(function () {
        $(this).attr('onchange', 'check_num(this);');
    });

    // 設定文字對齊
    $(".dataArea tr").find("td:first, td:last").each(function () {
       $(this).css({ "text-align": "center", "vertical-align": "middle" });
    });
    $("#gas td").css({ "text-align": "left", "vertical-align": "middle" });
    $("#gas td:first").css({ "text-align": "center", "vertical-align": "middle" });
    $("#gas td:last").css({ "text-align": "center", "vertical-align": "middle" });
    $(".dataArea tr").find("td:eq(1)").each(function () {
        $(this).css({ "vertical-align": "top" });
    });

    // 帶入隱藏欄位
    $("input[vafield]").each(function () {
        var target = $(this);
        var vafield = $(this).attr("vafield").split("|");
        for (var i = 0; i <= vafield.length - 1; i++) {
            $("input[name='" + vafield[i] + "']").change(function () {
                var val = "";
                for (var j = 0; j <= vafield.length - j; i++) {
                    val += $("input[name='" + vafield[j] + "']").val();
                }
                $(target).val(val);
            });
        }

    });

    // 上方功能列表
    $(".func_box button").click(function () {        
        var sctop = $(".dataArea").scrollTop();    
        switch ($(this).html().replace(regex, "")) {
            case "更新":
                //BloodGas群組判斷
                    var group1 = false;
                    var group2 = false;
                    var group3 = false;
                    var hasRecord = false;
                    var gpArr = new Array();
                    var select = '';
                 
                    $('input[name="gas_record"]').each(function (i) { gpArr[i] = this.value; });
                    for (i = 0 ; i < gpArr.length; i++) {
                        if (i < 8) {
                            if (gpArr[i] != '') {
                                group1 = true;
                            }
                        }
                        else if (i >= 8 && i < 14) {
                            if (gpArr[i] != '') {
                                group2 = true;
                            }
                        }
                        else {
                            if (gpArr[i] != '') {
                                group3 = true;
                            }
                        }
                    }
                    if (group1 == true) {
                        select += 'group1';
                        hasRecord = true;
                    }
                    if (group2 == true) {
                        if (select != '') {
                            select += "|";
                        }
                        select += 'group2';
                        hasRecord = true;
                    }
                    if (group3 == true) {
                        if (select != '') {
                            select += "|";
                        }
                        select += 'group3';
                        hasRecord = true;
                    }
                    if (hasRecord == true)
                    {
                        if ($('input[name="gas_posture"]:checked').val() == undefined)
                        {

                            alert('請填寫血液氣體測量部位')
                            success = false;
                            return;
                        }
                    }
                    $("#gas_select").val(select);
                

                    var gcs_temp = '';                
                if (($("input[name=gc_r1]:checked").val() != undefined || $("input[name=gc_r2]:checked").val() != undefined || $("input[name=gc_r3]:checked").val() != undefined)) {
                    for (var t = 1; t < 4; t++) {  //undefined太多  測試後待刪除
                        gcs_temp += $("input[name=gc_r" + t + "]:checked").val() + "|";
                    }
                    $("input[type=hidden][name=gc_record]").val(gcs_temp.substring(0, gcs_temp.length - 1));
                }
                if ($('#mp_text').val() != '') {
                    $("input[name=mp_part]").val($("input[name=mp_posture]:checked").val() + "|" + $("input[name=mp_position]:checked").val());
                }
                if ($('#bp_text').val() != '') {
                    $("input[name=bp_part]").val($("input[name=bp_posture]:checked").val() + "|" + $("input[name=bp_position]:checked").val());
                }
                $("#mask,#info").show();
                $('#new_date').val($('#new_day').val() + ' ' + $('#new_time').val());
                var success = true;

                //$("#vs_table input[type=text]:visible").each(function () {
                    //if ($(this).val() == "") {//有空输入，将flag置为false 
                    //    alert('請輸入 測量值!');
                    //    success = false;
                    //    $("#mask,#info").hide();
                    //    return;
                    //}
                //});
                if ($('input[name=gi_u_record]:checked').val() === '無解尿' && $('#gi_u_value').val() != '0' && $('#gi_u_value').val() != '') {

                    alert('選擇無解尿時，只能輸入0次')
                    $('#gi_u_value').val('');
                    success = false;
                    $("#mask,#info").hide();
                    return;

                }
                if ($('input[name=gi_u_record]:checked').val() === '結晶尿') {
                    if ($('#gi_u_value').val() === '0') {
                        alert('請輸入0以外的正確的次數')
                        $('#gi_u_value').val('')
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                if ($('input[name=gi_u_record]:checked').val() === '黃') {
                    if ($('#gi_u_value').val() == '0') {
                        alert('請輸入0以外的正確的次數')
                        $('#gi_u_value').val('')
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                else if ($('input[name=gi_u_record]:checked') && $('#gi_u_value').val() === '' || $('#gi_u_value').val() === undefined) {
                    success = true;
                }
                if (!!$('#gi_u_value').val()) {
                    if (!$('input[name=gi_u_record]:checked').prop('checked')) {
                        alert('請選擇尿液性質');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }

                if ($('#bt_drug').val() != '-1') {
                    if ($('#bt_drug').val() == '99' && $.trim($('#bt_drug_other').val()) == '') {
                        alert('請輸入 其他藥名!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    if ($('#bt_drug_amount').val() == '') {
                        alert('請輸入 劑量!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    if ($('#bt_drug_unit').val() == '-1') {
                        alert('請選擇 單位!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    success = true;
                }
                //檢查 排便
                if ($.trim($('#st_value').val()) > 0) {
                    var Error_Value = '';
                    if ($('#stoolType').val() == '請選擇' || $('#stoolColor').val() == '請選擇') {
                        Error_Value += "請選擇 排便 性狀顏色!\n";
                        success = false;
                    }
                    if ($('#stoolType').val() == '其他' && !$('#st_other2 input[name=' + 'st_record' + ']').val()) {
                        Error_Value += "請選擇 排便 其他性狀內容 !\n";
                        success = false;
                    }
                    if ($('#stoolColor').val() == '其他' && !$('#st_other input[name=' + 'st_record' + ']').val()) {
                        Error_Value += "請選擇 排便 其他顏色內容!\n";
                        success = false;
                    }
                    if (!!Error_Value) {
                        alert(Error_Value);
                    }
                }

                if (!CheckComa()) {
                    alert('昏迷指數未輸入完整，請檢查!');
                    success = false;
                    $("#mask,#info").hide();
                    return;
                }
                if (!CheckPain()) {
                    alert('疼痛評估未輸入完整，請檢查!');
                    success = false;
                    $("#mask,#info").hide();
                    return;
                }

                //評估時機 表格檢查
                //if ($('select[name=ps_occasion] option:selected').val() == "請選擇") {
                //    alert('請選擇 評估時機!');
                //    success = false;
                //    $("#mask,#info").hide();
                //    return;
                //}
                var check_value_arr = $(".psData input:radio:checked").map(function () {
                    var val_str = $(this).val().match(/^\(\d*/g).toString();
                    return parseInt(val_str.replace("(", ""));
                }).get();
                //if (check_value_arr.length <= 0) {
                //    alert('請評估 疼痛強度!');
                //    success = false;
                //    $("#mask,#info").hide();
                //    return;
                //}

                //體重單位
                if (!!$('#bw_val').val()) {
                    var Error_Value = '';
                    if ($('#bw_record').val() == '請選擇') {
                        Error_Value += "請選擇 體重 單位!";
                        success = false;
                    }
                    if (!!Error_Value) {
                        alert(Error_Value);
                    }
                }

                if ($('#bw_record').val() != '請選擇') {
                    var Error_Value = '';
                    if (!$('#bw_val').val()) {
                        Error_Value += "請輸入 體重 數值!";
                        success = false;
                    }
                    if (!!Error_Value) {
                        alert(Error_Value);
                    }
                }

                //總膽紅素 表格檢查
                if ($('input[type=radio][name=gi_j_part]:checked').val() != null) {
                    if ($('#giMemo').val() == "") {
                        alert('請輸入總紅膽素 數值!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                //臍帶脫落 表格檢查
                if ($('input[type=radio][name=gi_c_record]:checked').val() === '無脫落') {
                    if ($('#gi_c_type').val() == "") {
                        alert('請輸入臍帶 乾或濕!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                //檢查 痰液
                if ($('#sis').val() != null && $('#sis').val() != "無") {
                    if ($('#sir').val() == null) {
                        alert('請選擇 痰液 性質!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    if ($('#sid').val() == null) {
                        alert('請選擇 痰液 顏色!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }

                // 心跳判斷
                var x = $("input[name='mp_posture']:checked").val()
                if ($("#mp_text").val() != '' && $("input[name='mp_position']:checked").length === 0 && x.indexOf('心尖脈') < 0) {
                    //textBox有填                          沒選左右側                                            不是心尖         
                    alert("請選擇心跳動脈測量部位是'左側'還是'右側'?");
                    success = false;
                    $("#mask,#info").hide();
                    return;
                }

                //血氧判斷
                if (!$('input[type=checkbox][name=sp_reason]').prop('checked')) {
                    if ($('#sp_text').val() == "") {
                        if ($('input[type=radio][name=sp_part]:checked').length === 1) {
                            success = false;
                            alert('請輸入完整血氧 測量值!');
                            success = false;

                            $("#mask,#info").hide();
                            return;
                        }
                    } else {
                        if ($('input[type=radio][name=sp_part]:checked').length === 0) {
                            alert('請選擇血氧 測量方式!');
                            success = false;

                            $("#mask,#info").hide();
                            return;
                        }
                    }
                } else {
                    if ($('input[type=radio][name=sp_part]:checked').length === 0) {
                        alert('請選擇血氧 測量方式!');
                        success = false;

                        $("#mask,#info").hide();
                        return;
                    }
                }


                //檢查並將過濾無須更新的vs_item
                $("form[name=vs_upd_form] tr").each(function () {
                    var not_update = true;
                    //vs_item狀態重置成可更新項目
                    $(this).find("input[type=hidden][name=vs_item]").prop("disabled", false);
                    $(this).find("input, textarea, select, radio, checkbox").each(function () {
                        switch ($(this).getType()) {
                            case 'textarea':
                            case 'text':
                            case "select":
                                if ( ($(this).val() != $(this).attr('default') && ($(this).val() != "" && $(this).val() != "請選擇" && $(this).val() != -1))) {
                                    not_update = false;
                                }
                                break;
                            case 'radio':
                            case 'checkbox':
                                if ($(this).is(':checked') != ($(this).attr('default') == 'true')) {
                                    not_update = false;
                                    //console.log($(this).is(':checked') + "!=" + ($(this).attr('default') +"=="+ 'true'));
                                }
                        }
                    });
                    if (not_update) {
                        //不需更新的項目
                        $(this).find("input[type=hidden][name=vs_item]").prop("disabled", true);
                    }
                });

                var vs_item_arr = $('input:enabled[name=vs_item]').map(function () {
                    return $(this).val();
                }).get();

                if (vs_item_arr.length <= 0) {
                    alert("資料無異動，無法更新!");
                    success = false;
                }

                if (success && ch_all_numcheck()) {
                    var content = $('#bt_drug').val() + '|';
                    content += $('#bt_drug_other').val() + '|';
                    content += $('#bt_drug_amount').val() + '|';
                    content += $('#bt_drug_unit').val();
                    $('#bt_other_memo').val(content);

                    setTimeout(function () { $("form[name=vs_upd_form]").submit(); }, 800);
                }else {
                    $("#mask,#info").hide();
                }

                //BloodGas Part
                if ($('input[name="gas_posture"]:checked').val() != "其他") {
                    gasPart('N');
                }
                else {
                    gasPart('other');
                }
                break;
            case "返回":
                location.href = "VitalSign_Index";
                break;
            case "儲存":
                //BloodGas群組判斷
             
                    var group1 = false;
                    var group2 = false;
                    var group3 = false;
                    var hasRecord = false;
                    var gpArr = new Array();
                    var select = '';

                    $('input[name="gas_record"]').each(function (i) { gpArr[i] = this.value; });
                    for (i = 0 ; i < gpArr.length; i++) {
                        if (i < 8) {
                            if (gpArr[i] != '') {
                                group1 = true;
                            }
                        }
                        else if (i >= 8 && i < 14) {
                            if (gpArr[i] != '') {
                                group2 = true;
                            }
                        }
                        else {
                            if (gpArr[i] != '') {
                                group3 = true;
                            }
                        }
                    }
                    if (group1 == true) {
                        select += 'group1';
                        hasRecord = true;
                    }
                    if (group2 == true) {
                        if (select != '') {
                            select += "|";
                        }
                        select += 'group2';
                        hasRecord = true;
                    }
                    if (group3 == true) {
                        if (select != '') {
                            select += "|";
                        }
                        select += 'group3';
                        hasRecord = true;
                    }
                    if (hasRecord == true) {
                        if ($('input[name="gas_posture"]:checked').val() == undefined) {

                            alert('請填寫血液氣體測量部位')
                            success = false;
                            return;
                        }
                    }
                    $("#gas_select").val(select);
                


                if ($('#mp_text').val() != '')
                    $("input[name=mp_part]").val($("input[name=mp_posture]:checked").val() + "|" + $("input[name=mp_position]:checked").val());
                $("#mask,#info").show();
                var success = true;
                if ($('#bt_drug').val() != '-1') {
                    if ($('#bt_drug').val() == '99' && $.trim($('#bt_drug_other').val()) == '') {
                        alert('請輸入 其他藥名!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    if ($('#bt_drug_amount').val() == '') {
                        alert('請輸入 劑量!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    if ($('#bt_drug_unit').val() == '-1') {
                        alert('請選擇 單位!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    success = true;
                }
                //檢查 排便
                if ($.trim($('#st_value').val()) > 0) {
                    var Error_Value = '';
                    if ($('#stoolType').val() == '請選擇' || $('#stoolColor').val() == '請選擇') {
                        Error_Value += "請選擇 排便 性狀顏色!\n";
                        success = false;
                    }
                    if ($('#stoolType').val() == '其他' && !$('#st_other2 input[name=' + 'st_record' + ']').val()) {
                        Error_Value += "請選擇 排便 其他性狀內容 !\n";
                        success = false;
                    }
                    if ($('#stoolColor').val() == '其他' && !$('#st_other input[name=' + 'st_record' + ']').val()) {
                        Error_Value += "請選擇 排便 其他顏色內容!\n";
                        success = false;
                    }
                    if (!!Error_Value) {
                        alert(Error_Value);
                    }
                }
                
                var sis = $("#sis :selected").text();
                //檢查 痰液
                if (sis != "" && sis != "無") {
                    var sir = $("#sir :selected").text();
                    var sid = $("#sid :selected").text();
                    if (sir == "") {
                        alert('請選擇 痰液 性質!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                    if (sid == "") {
                        alert('請選擇 痰液 顏色!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }

                //評估時機 表格檢查
                //if ($('select[name=ps_occasion] option:selected').val() == "請選擇") {
                //    alert('請選擇 評估時機!');
                //    success = false;
                //    $("#mask,#info").hide();
                //    return;
                //}
                var check_value_arr = $(".psData input:radio:checked").map(function () {
                    var val_str = $(this).val().match(/^\(\d*/g).toString();
                    return parseInt(val_str.replace("(", ""));
                }).get();
                //if (check_value_arr.length <= 0) {
                //    alert('請評估 疼痛強度!');
                //    success = false;
                //    $("#mask,#info").hide();
                //    return;
                //}

                //ABP 檢查兩個表格都要輸入 不可以單獨
                if ($('#abpl_text').val() == "" && $('#abph_text').val() != "") {
                    success = false;
                    alert('請輸入完整ABP 測量值!');
                    
                    success = false;
                    $("#mask,#info").hide();
                    return;
                }
                if ($('#abph_text').val() == "" && $('#abpl_text').val() != "") {
                    success = false;
                    alert('請輸入完整ABP 測量值!');
                    success = false;
                    
                    $("#mask,#info").hide();
                    return;
                }
                //PA 檢查兩個表格都要輸入 不可以單獨
                if ($('#pal_text').val() == "" && $('#pah_text').val() != "") {
                    success = false;
                    alert('請輸入完整PA 測量值!');

                    success = false;
                    $("#mask,#info").hide();
                    return;
                }
                if ($('#pah_text').val() == "" && $('#pal_text').val() != "") {
                    success = false;
                    alert('請輸入完整PA 測量值!');
                    success = false;

                    $("#mask,#info").hide();
                    return;
                }
               
                //尿液 表格檢查 
                if ($('input[name=gi_u_record]:checked').val() === '無解尿' && $('#gi_u_value').val() != '0') {

                    alert('選擇無解尿時，只能輸入0次')
                    $('#gi_u_value').val('');
                    success = false;
                    $("#mask,#info").hide();
                    return;

                }
                if ($('input[name=gi_u_record]:checked').val() === '結晶尿')
                {
                    if ($('#gi_u_value').val() == "")
                    {
                       alert('請輸入完整尿液 測量值!');
                       success = false;
                       $("#mask,#info").hide();
                       return;
                    }
                        
                    if ($('#gi_u_value').val() === '0') {
                        alert('請輸入0以外的正確的次數')
                        $('#gi_u_value').val('')
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                if ($('input[name=gi_u_record]:checked').val() === '黃') {
                    if ($('#gi_u_value').val() == "")
                    {
                        alert('請輸入完整尿液 測量值!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }

                    if ($('#gi_u_value').val() == '0')
                    {
                        alert('請輸入0以外的正確的次數')
                        $('#gi_u_value').val('')
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                if (!!$('#gi_u_value').val()) {
                    if (!$('input[name=gi_u_record]:checked').prop('checked'))
                    {
                        alert('請選擇尿液性質');
                        success = false;
                        $("#mask,#info").hide();    
                        return;
                    }
                }
                //體重單位
                if (!!$('#bw_val').val()) {
                        var Error_Value = '';
                        if ($('#bw_record').val() == '請選擇') {
                            Error_Value += "請選擇 體重 單位!";
                            success = false;
                        }
                        if (!!Error_Value) {
                            alert(Error_Value);
                        }                    
                }
                if ($('#bw_record').val() != '請選擇') {
                    var Error_Value = '';
                    if (!$('#bw_val').val()) {
                        Error_Value += "請輸入 體重 數值!";
                        success = false;
                    }
                    if (!!Error_Value) {
                        alert(Error_Value);
                    }
                }
                //總膽紅素 表格檢查
                if ($('input[type=radio][name=gi_j_part]:checked').val() != null) {
                    if ($('#giMemo').val() == "") {
                        alert('請輸入總紅膽素 數值!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                //臍帶脫落 表格檢查
                if ($('input[type=radio][name=gi_c_record]:checked').val() === '無脫落') {
                    if ($('#gi_c_type').val() == "") {
                        alert('請輸入臍帶 乾或濕!');
                        success = false;
                        $("#mask,#info").hide();
                        return;
                    }
                }
                //血壓 表格檢查
                if ($('#bph_text').val() == "" && $('#bpl_text').val() != "") {
                    success = false;
                    alert('請輸入完整血壓 測量值!');

                    success = false;
                    $("#mask,#info").hide();
                    return;
                }
                if ($('#bpl_text').val() == "" && $('#bph_text').val() != "") {
                    success = false;
                    alert('請輸入完整血壓 測量值!');
                    success = false;

                    $("#mask,#info").hide();
                    return;
                }
                //血氧判斷
                if (!$('input[type=checkbox][name=sp_reason]').prop('checked')) {
                    if ($('#sp_text').val() == "") {
                        if ($('input[type=radio][name=sp_part]:checked').length === 1) {
                        success = false;
                        alert('請輸入完整血氧 測量值!');
                        success = false;

                        $("#mask,#info").hide();
                        return;
                        }
                    } else {
                        if ($('input[type=radio][name=sp_part]:checked').length===0) {
                            alert('請選擇血氧 測量方式!');
                            success = false;

                            $("#mask,#info").hide();
                            return;
                        }
                    }
                } else {
                    if ($('input[type=radio][name=sp_part]:checked').length === 0) {
                        alert('請選擇血氧 測量方式!');
                        success = false;

                        $("#mask,#info").hide();
                        return;
                    }
                }


                // 心跳判斷
                var x = $("input[name='mp_posture']:checked").val()
                if ($("#mp_text").val() != '' && $("input[name='mp_position']:checked").length === 0 && x.indexOf('心尖脈') < 0) {
                    //textBox有填                          沒選左右側                                            不是心尖         

                    alert("請選擇心跳動脈測量部位是'左側'還是'右側'?");              
                    success = false;
                    $(".dataArea").animate({ scrollTop: sctop - 3000 }, 200);
                    $("#mask,#info").hide();
                    return;
                }
                //檢查 昏迷
                if (!CheckComa()) {
                    alert('昏迷指數未輸入完整，請檢查!');
                    success = false;
                }
                if (!CheckPain()) {
                    alert('疼痛評估未輸入完整，請檢查!');
                    success = false;
                    $("#mask,#info").hide();
                    return;
                }
                //if ($('input:radio[name=gc_r4]:checked').val() == " ") {
                //    if ($('#stoolType').val() == '請選擇' || $('#stoolColor').val() == '請選擇') {
                //        alert('請選擇 無法測量原因!');
                //        success = false;
                //    }
                //}
                //else {
                //    //$('#stoolType').val('請選擇');
                //    //$('#stoolColor').val('請選擇');
                //}
                if (success && ch_all_numcheck()) {
                    var content = $('#bt_drug').val() + '|';
                    content += $('#bt_drug_other').val() + '|';
                    content += $('#bt_drug_amount').val() + '|';
                    content += $('#bt_drug_unit').val();
                    $('#bt_other_memo').val(content);
                    $("form[name=vs_form]").submit();
                }
               
                else
                    $("#mask,#info").hide();            
                break;
            case "放棄":
                if (confirm('確認是否要放棄編輯資料?'))
                    location.reload();
                break;
            case "設定":
                var top = $(this).position().top + $(this).height() + 10;
                var left = $(this).position().left;
                $(".func_setting").css({ "top": top, "left": left});
                if (!$(".func_setting").is(":visible")) {
                    $('.func_setting').slideDown(350, function () {
                        $(".func_setting").css({ "overflow": '' });
                    });
                    $(this).css("background-color", "#FFFF99");
                } else {
                    $('.func_setting').slideUp(350);
                    $(this).css("background-color", "");

                    if (location.pathname != "/VitalSign/VitalSignModify") {
                        $(".mask").fadeIn(500);
                    }
                    var dtstr = new Array(), i = 0;
                    $(".func_setting button").each(function () {
                        if ($(this).attr("class") == "func_selected") {
                            dtstr[i] = $(this).val();
                            i++;
                        }
                    });
                    if (location.pathname != "/VitalSign/VitalSignModify") {
                        $.ajax({
                            url: "../VitalSign/SaveVSOption",
                            type: "post",
                            timeout: 5000,
                            data: "func_list=" + dtstr.join("|"),
                            success: function () {
                                setTimeout(function () { $(".mask").fadeOut(350); }, 800);
                            }
                        });
                    }
                }
                break;
            case "批次輸入":
                location.href = 'VitalSign_Multiple';
                break;
            case "TPR":
                window.open('Tpr_Index', 'top_dialog', 'menubar=no,status=no,scrollbars=yes,top=0,resizable=yes,left=200,toolbar=no,width=1024,height=768');
                break;
            case "查詢":
                location.href = 'VitalSign_Index';
                break;
            case "往上移動":
                $(".dataArea").animate({ scrollTop: sctop - 150 }, 200);
                break;
            case "往下移動":
                $(".dataArea").animate({ scrollTop: sctop + 150 }, 200);
                break;
            case "儀器資料":
                var feeno = $("#feeno").val();
                window.open('VitalSign_Interfacing?feeno=' + feeno, '', 'menubar=no,status=no,scrollbars=yes,top=0,left=200,toolbar=no,width=1024,height=768');
                break;
            case "ICU帶入":
                var feeno = $("#feeno").val();
                window.open('ICUData?feeno=' + feeno, 'Tpr_Index', 'menubar=no,status=no,scrollbars=yes,top=0,resizable=yes,left=200,toolbar=no,width=1024,height=768');
                break;
            case "VIP資料修改":
                location.href = 'Machine_Datalist';
                break;
            default:
                break;
        }
      
    });

    $(".dataArea input[type=text][class!='tooltip'][class!='word']").focus(function () {
        currObj = $(this);
        currobj2 = this;
        var keybordHeight = $("#keybord").outerHeight();
        var keybordWidth = $("#keybord").outerWidth();
        var top = $(this).position().top + $(this).height() + 10;
        var left = $(this).position().left;
        if ((top + keybordHeight) > $(window).outerHeight())
            top = $(this).position().top - keybordHeight - 10;
        if ((left + keybordWidth) > $(window).outerWidth())
            left = $(this).position().left - (keybordWidth - $(this).outerWidth());
        //$("#keybord").css({
        //    "top": top,
        //    "left": left
        //}).slideDown(350);
    }).keydown(function (e) {
        if (e.which == 13) {
            $("#keybord").slideUp(350);
            if ($(this).attr("va_check") == "true") {
                carry_in_info($(this));
            } else {
                next_tab();
            }
        }
    });

    $("#keybord button").click(function () {
        switch ($(this).html()) {
            case "↵":
                if (numcheck(currObj)) {
                    var e = $.Event("keydown");
                    e.which = 13;
                    $(currObj).trigger(e);
                }
                break;
            case "身高":
                $("tbody[id='身高']").fadeIn(350);
                $("input[name='bh_record']").focus();
                break;
            case "體重":
                $("tbody[id='體重']").fadeIn(350);
                $("input[name='bw_record']").focus();
                break;
            case "←":
                var cString = $(currObj).val();
                cString = cString.substring(0, cString.length - 1)
                $(currObj).val(cString);
                break;
            case "×":
                if (numcheck(currObj))
                    $("#keybord").slideUp(350);
                break;
            default:
                if (isTextSelected(currObj))
                    $(currObj).val(Trim($(this).html()));
                else
                    $(currObj).val($(currObj).val() + Trim($(this).html()));
                check_num(currobj2);
                break;
        }
    });
    // 意識評估內容帶入、功能設定  
    $("input[name=gc_part]").click(function () {
        var dataStr = "gc_type=" + encodeURIComponent($(this).val());       
        if ($("input[name=gc_default_data]").length > 0) {
            dataStr += "&df_data1=" + $("input[name=gc_default_data]").val();
        }
        $.ajax({
            url: "../VitalSign/VitalSignGCS",
            type: "post",
            cache: "false",
            data: dataStr,
            timeout: 5000,
            success: function (data) {
                $(".gcsData").html(data);
              mt_select_setting();
                //setToolTip();
            }
        });
    });


    //$('#gc .gcsData').click(function () {
    //    if ($('#gc .gcsData input[name=gc_r1]:checked').val() == '(O)其他') {
    //        $('#gcE_record').prop('disabled', false);
    //        $('#gcE_record').show();
    //    } else {
    //        $('#gcE_record').prop('disabled', true);
    //        $('#gcE_record').hide().val('');
    //    }

    //    if ($('#gc .gcsData input[name=gc_r2]:checked').val() == '(O)其他') {
    //        $('#gcV_record').prop('disabled', false);
    //        $('#gcV_record').show();
    //    } else {
    //        $('#gcV_record').prop('disabled', true);
    //        $('#gcV_record').hide().val('');
    //    }

    //    if ($('#gc .gcsData input[name=gc_r3]:checked').val() == '(O)其他') {
    //        $('#gcM_record').prop('disabled', false);
    //        $('#gcM_record').show();
    //    } else {
    //        $('#gcM_record').prop('disabled', true);
    //        $('#gcM_record').hide().val('');
    //    }
    //})

    $("input[name=gc_part]:checked").trigger("click");

    // 疼痛內容帶入、功能設定
    $("input[name=ps_assess]").click(function () {
        var dataStr = "ps_type=" + encodeURIComponent($(this).val());
       
        if ($("input[name=ps_default_data]").length > 0) {
            dataStr += "&df_data=" + $("input[name=ps_default_data]").val();
        }
        $.ajax({
            url: "../VitalSign/VitalSignPS",
            type: "post",
            data: dataStr,
            timeout: 5000,
            success: function (data) {
                $(".psData").html(data);
                mt_select_setting();
                setToolTip();
            }
        });
    });
    $("input[name=ps_assess]:checked").trigger("click");

    // 設定功能列表
    $(".func_setting button").click(function () {
        if ($(this).attr("class") == "func_selected") {
            $(this).removeAttr("class").css("background-color", "");
            $("tbody[id='" + $(this).val() + "']").hide(350).find("input[type=hidden][name=vs_item]").prop("disabled", true);
            //$("tbody[id='" + $(this).val() + "']").fadeOut(350).find("input[type=hidden][name=vs_item]").prop("disabled", true);
        } else {
            $(this).attr("class", "func_selected").css("background-color", "#FFFF99");
            //$("tbody[id='" + $(this).val() + "']").fadeIn(350).find("input[type=hidden][name=vs_item]").prop("disabled", false);
            $("tbody[id='" + $(this).val() + "']").show(350).find("input[type=hidden][name=vs_item]").prop("disabled", false);
        }
        color_setting("#vs_table");
    });

    $(window).resize(function () {
        if ($(this).outerHeight() <= 50) {
            $(".dataArea").outerHeight(350);
        }
        else {
            $(".dataArea").outerHeight($(this).outerHeight() - 120);
        }
    }).trigger("resize");

    mt_select_setting();

    // 設定顏色、訊息置中
    setTimeout(function () {
        color_setting("#vs_table");
        $("#info").center();
    }, 800);

    $('input[type="text"]').click(function () {
        this.select();
    });

});

function set_ck_reason(num) {
    $(".ck_reason").each(function () {
        $(this).val(num);
        if (this.id == 'mp_reason' || this.id == 'bf_reason')
            set_ck_reason_memo('mp', num);
    });
}

function set_ck_reason_memo(id, num) {
    var content = "";
    if (num == '測不到') {
        content = '心跳及呼吸測不到，血壓:___/___mmHg，意識狀態:E___V___M___，瞳孔大小:___/___(+-±/+-±)，急通知:_______醫師並於__:__開始施行心肺復甦術，置入氣';
        content += '管內管__Fr.，固定___cm，予氧氣(呼吸器:VC/SIMV+PS/PS/PC Mode，TV:____，rate:__次/分，FiO2:__%， PEEP:__，PC:__)，抽痰並保持呼吸道';
        content += '通暢，痰量多/少，___(性質)，___(顏色)，___，密切監測心電圖及血壓。';
        content += 'Bosmin 1支IV st. at____，予N.S.500cc+Dopamine 2 Amp IVF___gtt/min，心電圖：VT，電擊___焦耳，續行心肺復甦術。';
    }
    $('#' + id + '_memo').val(content);
}
//額溫
function set_bt_f() {
    $('#bt_text').attr('lo_check', 'btl_f');
    $('#bt_text').attr('hi_check', 'bth_f');
}
//耳溫
function set_bt_e() {
    $('#bt_text').attr('lo_check', 'btl_e');
    $('#bt_text').attr('hi_check', 'bth_e');
}
//腋溫
function set_bt_a() {
    $('#bt_text').attr('lo_check', 'btl_a');
    $('#bt_text').attr('hi_check', 'bth_a');
}
//肛溫
function set_bt_r() {
    $('#bt_text').attr('lo_check', 'btl_r');
    $('#bt_text').attr('hi_check', 'bth_r');
}

function ch_all_numcheck() {
    var success = true;
    var txt_list = $('.check');
    for (var i = 0; i < txt_list.length; i++) {
        success = numcheck(txt_list[i]);
    }
    return success;
}

//驗證欄位_數字
function numcheck(obj) {
    var num = $(obj).val();
    var success = true;
    if (num != "") {
        var max = parseFloat($(obj).attr('max_num'));
        var min = parseFloat($(obj).attr('min_num'));
        if (max != '') {
            if (parseFloat(num) > max)
                success = false;
        }
        if (min != '') {
            if (parseFloat(num) < min) 
                success = false;
        }
    }
    if (!success) {
        alert('請輸入正常範圍');
        $(obj).val('');
        var top = parseFloat($(obj).parent("td").parent("tr").parent("tbody").offset().top) - 15;
        $(".dataArea").animate({ scrollTop: top }, 0, "swing", function () { $(obj).focus(); });
    }
    return success;
}

//驗證欄位_數字
function check_num(obj) {
    if (obj.value.indexOf('.') > -1) {
        var re = /^[0-9]+\.[0-9]*$/;
        if (obj.value.indexOf('-') == 0)
            re = /^[-]?[0-9]+\.[0-9]*$/;
    }
    else {
        var re = /^[0-9]+$/;
        if (obj.value.indexOf('-') == 0)
            re = /^[-]?[0-9]*$/;
    }
    if (!re.test(obj.value) && obj.value != "") {
        alert("只能輸入數字");
        $(obj).val('');
    }
    else if (obj.value.indexOf('.') > -1) {
        var re = /^[0-9]+\.[0-9]?$/;
        if (!re.test(obj.value) && obj.value != "")
            $(obj).val(obj.value.substring(0, obj.value.indexOf('.') + 2));
    }
}

//判斷text 是否為選取狀態
function isTextSelected(input) {
    var startPos = $(input).prop('selectionStart');
    var endPos = $(input).prop('selectionEnd');
    if (startPos != undefined && endPos != undefined && startPos != endPos) {
        if ($(input).val().substring(startPos, endPos - startPos).length == $(input).val().length) {
            return true;
        }
        else
            return false;
    }
    else
        return false;
}

