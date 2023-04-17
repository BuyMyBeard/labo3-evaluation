// script pour l'interface de gestion de sélection avec deux <select...>
// Il faut utiliser le fichier de styles css/flashButtons.css les <div> de
// classe MoveLeft et MoveRight
//
// auteur : Nicolas Chourot

$(document).ready(initUI);

function initUI() {
    $('.MoveLeft').hide();
    $('.MoveRight').hide();
    sortAllSelect();
    $('.UnselectedItems').change(function (e) {
        let parent = $(this).parent();
        parent.find('.UnselectedItems option:selected').each(function () {
            parent.find(".SelectedItems option:selected").prop("selected", false);
            parent.find('.MoveLeft').first().show();
            parent.find('.MoveRight').first().hide();
        });
        e.preventDefault();
    });

    $('.SelectedItems').change(function (e) {
        let parent = $(this).parent();
        parent.find('option:selected').each(function () {
            parent.find(".UnselectedItems option:selected").prop("selected", false);
            parent.find('.MoveLeft').first().hide();
            parent.find('.MoveRight').first().show();
        });
        e.preventDefault();
    });

    $(document).on('submit', 'form', function () {
        $('.SelectedItems option').prop('selected', true);
    });

    ///////////////////////////////////////////////////////////////////
    // On the click event on the image id="MoveLeft"
    ///////////////////////////////////////////////////////////////////
    $(".MoveLeft").on('click', function () {
        let parent = $(this).parent().parent();
        console.log(parent)
        parent.find('.UnselectedItems').first().find('option:selected').each(function () {
            console.log($(this))
            $(this).remove();
            $(this).prop("selected", false);
            parent.find('.SelectedItems').first().append($(this));
            sortSelect(parent.find(".SelectedItems").first());
            scrollTo(parent.find(".SelectedItems").first(), $(this).offset().top);
        });
        parent.find('.MoveLeft').hide();
    });

    ///////////////////////////////////////////////////////////////////
    // On the click event on the image id="MoveRight"
    ///////////////////////////////////////////////////////////////////
    $(".MoveRight").on('click', function () {
        let parent = $(this).parent().parent();
        console.log(parent)
        parent.find('.SelectedItems').first().find('option:selected').each(function () {
            $(this).remove();
            $(this).prop("selected", false);
            parent.find('.UnselectedItems').first().append($(this));
            sortSelect(parent.find(".UnselectedItems").first());
            scrollTo(parent.find(".UnselectedItems").first(), $(this).offset().top);
        });
        $('.MoveRight').hide();
    });
}

///////////////////////////////////////////////////////////////////
// Sort text items of a listbox
///////////////////////////////////////////////////////////////////
function normalize(str) {
    return str.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}
function sortSelect(select) {
    select.each(function () {
        let select = $(this);
        select.html(select.find('option').sort(function (option1, option2) {
            return $(option1).text() < $(option2).text() ? -1 : 1;
        }))
    });
}

function sortAllSelect() {
    $('select').each(function () {
        let select = $(this);
        select.html(select.find('option').sort(function (option1, option2) {
            return $(option1).text() < $(option2).text() ? -1 : 1;
        }))
    });       
}

function scrollTo(selectObj, optionTop) {
    var selectTop = selectObj.offset().top;
    selectObj.scrollTop(selectObj.scrollTop() + (optionTop - selectTop));
}