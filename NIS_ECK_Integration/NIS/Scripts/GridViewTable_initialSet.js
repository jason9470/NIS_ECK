//***************這兩個方法實際內容在grideviewscroll.js中***************//
function enhance() {
    gridViewScroll.enhance();
}
function undo() {
    gridViewScroll.undo();
}
//***************GrideViewScroll表格設定***************//
//重新繪製表格時
//1. 先將div內容清空，只放table標籤     div1.innerHTML = '<table cellspacing="0" id="' + table_id + '" style="width:100%;"></table>';
//2. 設定div寬高                       div1.setAttribute("style", style);    //var style = "width:" + screen_width + "px;height:" + screen_height + "px;";
//3. 繪製表格                          buildTable(...);
//4. GrideViewScroll表格設定           GrideViewScrollTable_initial(table_id, true, false, 2, 2, screen_width, screen_height);
function GrideViewScrollTable_initial(table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height) {
    var gridViewScroll = null;
    var options = new GridViewScrollOptions();

    options.elementID = table_id;
    options.width = screen_width;
    options.height = screen_height;
    options.freezeColumn = columnFreeze;
    options.freezeFooter = footerFreeze;
    options.freezeColumnCssClass = "GridViewScrollItemFreeze";
    options.freezeFooterCssClass = "GridViewScrollFooterFreeze";
    options.freezeHeaderRowCount = headerRowCount;
    options.freezeColumnCount = columnCount;

    gridViewScroll = new GridViewScroll(options);
    gridViewScroll.enhance();
}

/*//網頁另一個表格
function GrideViewScrollTable(table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height) {
    var gridViewScroll = null;
    var options = new GridViewScrollOptions();

    options.elementID = table_id;
    options.width = screen_width;
    options.height = screen_height;
    options.freezeColumn = columnFreeze;
    options.freezeFooter = footerFreeze;
    options.freezeColumnCssClass = "GridViewScrollItemFreeze";
    options.freezeFooterCssClass = "GridViewScrollFooterFreeze";
    options.freezeHeaderRowCount = headerRowCount;
    options.freezeColumnCount = columnCount;

    gridViewScroll = new GridViewScroll(options);
    gridViewScroll.enhance();
}

//設定表格為GrideViewScroll樣式
function setGrideViewScrollTable(origin_table_id, new_table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height) {
    //如果不是原本頁面預設的表格id，就要設定新表格的GrideViewScroll
    if (origin_table_id != new_table_id) {
        addLoadEvent_GrideViewScrollTable(new_table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height);
    }
}

//window.onload執行預設表格  或  預設+第二個表格
function addLoadEvent_GrideViewScrollTable(table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height) {
    var oldonload = window.onload;
    console.log(6.1);
    if (typeof window.onload != 'function') {
        console.log(6.2);
        window.onload = GrideViewScrollTable(table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height);
        console.log(6.3);
    } else {
        console.log(6.4);
        window.onload = function () {
            console.log(6.5);
            if (oldonload) {
                console.log(6.6);
                oldonload();
            }
            console.log(6.7);
            GrideViewScrollTable(table_id, columnFreeze, footerFreeze, headerRowCount, columnCount, screen_width, screen_height);
        }
    }
}*/
