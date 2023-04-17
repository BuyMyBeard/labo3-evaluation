$(document).ready(function () {
    /*$('.phone').mask('(999) 999-9999');
    $('.zipcode').mask('a9a 9a9');*/
    $(".datepicker").datepicker({
        dateFormat: 'yy-mm-dd',
        changeMonth: true,
        changeYear: true,
        yearRange: "-100:+0",
        dayNamesMin: ["D", "L", "M", "M", "J", "V", "S"],
        monthNamesShort: ["Janv.", "Févr.", "Mars", "Avril", "Mai", "Juin", "Juil.", "Août", "Sept.", "Oct.", "Nov.", "Déc."]
    });

    /*Filter unicode hack */
    $(":input").change(function () {
        try {
            let r = $(this).val().replace(/[^\x00-\xFF]/g, "");
            $(this).val(r);
        } catch (e) { }
    });
    $("textarea").change(function () {
        try {
        let r = $(this).val().replace(/[^\x00-\xFF]/g, "");
            $(this).val(r);
        } catch (e) { }
    });

    $(".countrySelect").change((e) => {
        $(e.target).next().attr("src", "/Images_Data/Loading_icon.gif")
        $.ajax({
            url: "/CountryFlag/get?countryCode=" + $(e.target).val(),
            datatype: "application/json",
            success: json => { $(e.target).next().attr("src", json); }
        });

    })
})

function InstallAutoComplete(targetId, words) {

    function split(val) {
        return val.split(/ \s*/);
    }

    function RemoveExtra(str, extra) {
        var extraLength = extra.length;
        var lastExtraIndex = str.lastIndexOf(extra);
        if ((lastExtraIndex + extraLength) == str.length)
            str = str.substring(0, str.length - extraLength);
        return str;
    }

    function extractLast(term) {
        return split(term).pop();
    }

    $("#" + targetId)
        // don't navigate away from the field on tab when selecting an item
        .bind("keydown", function (event) {
            if (event.keyCode === $.ui.keyCode.TAB && $(this).data("ui-autocomplete").menu.active) {
                event.preventDefault();
            }
        })
        .autocomplete({
            minLength: 1,
            source: function (request, response) {
                // delegate back to autocomplete, but extract the last term
                response($.ui.autocomplete.filter(words, extractLast(request.term)));
            },
            focus: function () {
                // prevent value inserted on focus
                return false;
            },
            select: function (event, ui) {
                var terms = split(this.value);
                // remove the current input
                terms.pop();
                // add the selected item
                terms.push(ui.item.value);
                // add placeholder to get the comma-and-space at the end
                terms.push("");
                this.value = RemoveExtra(terms, ",").join(" ");
                return false;
            }
        });
}

function ajaxActionCall(actionLink) {
    // Ajax Action Call to actionLink
    $.ajax({
        url: actionLink,
        method: 'GET',
        success: (data) => {
            console.log("Result: " + data);
        }
    });
}
