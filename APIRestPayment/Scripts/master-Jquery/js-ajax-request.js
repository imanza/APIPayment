/// <reference path="../jquery-2.1.1.js" />
/// <reference path="../jquery-ui-1.10.4.min.js" />
/// <reference path="../jquery-2.1.1.js" />
/// <reference path="../jquery-ui-1.10.4.js" />
(function ($) {
    $(document).ready(function ($) {
        initAJAXPages();
        $(".base-content").height(window.innerHeight - 140);
    });
    var initAJAXPages = function () {
        history.replaceState({}, document.title, document.location.href);
        var main = $('#main');
        $(document).on('click', 'a:not([href^="#"]):not([href*=".pdf"]):not([href*=".jpg"])', function (event) {
            var href = $(this).attr('href');
            if (href.indexOf(document.domain) > -1 || href.indexOf(':') === -1) {
                ajaxLoadPage(href);
                event.preventDefault();
            }
        });
        $(window).on('popstate', function (event) {
            if (event.originalEvent.state !== null) {
                ajaxLoadPage(location.href);
            }
        });
        ajaxLoadPage = function (href) {
            $(document).ajaxStart(function () {
                $(".ajax-loader").fadeIn('slow');
            });
            $('.ajaxContainer').empty().load(href + " .content-body",
            function (responseText) {
                ajaxLoadComplete(responseText, href);
            }
            );
            $(document).ajaxComplete(function () {
                $('html, body').stop(true, false).animate({ scrollTop: 0 }, 800, function () {
                    $(".ajax-loader").fadeOut('slow');
                }).delay(6000);

            });
        };
        ajaxLoadComplete = function (responseText, href) {
            String.prototype.decodeHTML = function () {
                return $("<div>", { html: "" + this }).html();
            };
            var title = $(responseText).filter('main').attr('title');
            if (!title) {
                title = $(responseText).filter('title').text();
            }
            document.title = title;
            if (href != location.href) {
                history.pushState({}, title, href);
            }
            // Unwrap new page from container
            $('.ajaxContainer .content-body').unwrap();
            // Remove old page
            $('.content-body:not(:first)').remove();
            // Remove any extra containers                      
            $('.ajaxContainer').remove();
            // Apend new container for next page load
            main.prepend('<div class="ajaxContainer"></div>');
        };
    };
})(jQuery);
