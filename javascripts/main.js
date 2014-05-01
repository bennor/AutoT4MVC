(function ($) {
    if (!$) { return; }

    $(function () {
        $.get('https://api.github.com/repos/bennor/AutoT4MVC/releases', function (releases) {
            try {
                var html = $('<div id="releases"><h2>Releases</h2></div>');
                html.hide();
                var list = $('<ul />')

                if (!releases || !releases.length) { return; }
                releases.sort(compareTags).reverse();

                $.each(releases, function () {
                    var release = this,
                   title = $('<h3/>').text(release.name),
                   content = markdown.toHTML(release.body, 'Gruber');
                    $('<li />').append(title).append(content).appendTo(list);
                });
                html.append(list);
                $('#main-content').append(html);
                html.show();
            } catch (e) { }
        });

        function compareTags(a, b) {
            var ta = parseTag(a.tag_name);
            var tb = parseTag(b.tag_name);
            if (ta.major < tb.major) { return -1; }
            if (ta.major > tb.major) { return 1; }
            if (ta.minor < tb.minor) { return -1; }
            if (ta.minor > tb.minor) { return 1; }
            if (ta.build < tb.build) { return -1; }
            if (ta.build > tb.build) { return 1; }
            return 0;
        }

        function parseTag(tag) {
            var match = (tag || '').match(/(\d+)(?:\.(\d+)(?:\.(\d+)))\b/);
            if (!match) {
                return {};
            }
            return {
                major: parseInt(match[1], 10),
                minor: parseInt(match[2], 10) || 0,
                build: parseInt(match[3], 10) || 0
            };
        }
    })
})(window.$);