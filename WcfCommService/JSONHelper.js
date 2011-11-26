$.postJSON = function (url, data, callback) {
    $.ajax({
        'url': url,
        'type': 'post',
        'processData': false,
        'data': JSON.stringify(data),
        contentType: 'application/json',
        success: function (data) { if (null != callback) { callback(data); } }
    });
};


function GetTabString(tabLevel) {
    var returnString = '';
    for (i = 0; i < tabLevel; i++) {
        returnString += '\t';
    }
    return returnString;
};


$.printableJSON = function (JSONObject, tabLevel) {
    var printString = "";
    if (null == JSONObject) {
        return printString;
    }
    if (typeof JSONObject == 'object') {
        printString += "{ \n";
        $.each(JSONObject, function (key, value) {
            printString += GetTabString(tabLevel) + key + ":\t" + $.printableJSON(value, tabLevel + 1) + "\n";
        });
        printString += GetTabString(tabLevel) + " } \n";
    }
    else {
        printString += JSONObject;
    }
    return printString;
};




