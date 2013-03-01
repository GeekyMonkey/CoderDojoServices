var openNewWindow = function(url)
{
    window.open(url);
}

var onPageShow = function()
{
    // FormSave button just submits the form that's on the same page
    $("div.pagerole").on("click", "a.FormSave", function (e) {
        e.preventDefault();
        var form = $(this).closest("div.pagerole").find("form");
        if (form.length == 0) {
            console.log("Form save clicked, but form not found");
        }
        form.submit();
    });
}

/* jQuery Startup */
$(function () {
    onPageShow();

    $(document).on('pageshow', 'div.pagerole', function (event, ui) {
        onPageShow();
    });

    // Disable caching of some pages
    $(document).on('pagehide', 'div.pagerole', function (event, ui) {
        if (ui.nextPage && ui.nextPage[0] && $(ui.nextPage[0]).is('.ui-dialog')) {
            return;
        }
        var page = jQuery(event.target);
        if (page.attr('data-cache') == 'never') {
            page.remove();
        };
    });
});
