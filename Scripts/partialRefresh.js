let EndSessionAction = '/Session/End';

class PartialRefresh {
    constructor(serviceURL, container, refreshRate, postRefreshCallback = null) {
        this.serviceURL = serviceURL;
        this.container = container;
        this.postRefreshCallback = postRefreshCallback;
        this.refreshRate = refreshRate * 1000;
        this.paused = false;
        this.refresh(true);
        setInterval(() => { this.refresh() }, this.refreshRate);
    }
    static setEndSessionAction(action) {
        EndSessionAction = action;
    }
    pause() { this.paused = true }

    restart() { this.paused = false }

    replaceContent(htmlContent) {
        if (htmlContent !== "") {
            $("#" + this.container).html(htmlContent);
            if (this.postRefreshCallback != null) this.postRefreshCallback();
        }
    }

    static redirectToEndSessionAction() {
        console.log(this.EndSessionAction)
        window.location = this.EndSessionAction;
    }

    refresh(forced = false) {
        if (!this.paused) {
            $.ajax({
                url: this.serviceURL + (forced ? (this.serviceURL.indexOf("?") > -1 ? "&" : "?") + "forceRefresh=true" : ""),
                dataType: "html",
                success: (htmlContent) => { this.replaceContent(htmlContent) },
                statusCode: {
                    408: function () {
                        if (EndSessionAction != "")
                            window.location = EndSessionAction + "?message=Session expirée";
                        else
                            alert("Time out occured!");
                    },
                    401: function () {
                        if (EndSessionAction != "")
                            window.location = EndSessionAction + "?message=Access illégal";
                        else
                            alert("Illegal access!");
                    },
                    403: function () {
                        if (EndSessionAction != "")
                            window.location = EndSessionAction + "?message=Compte bloqué";
                        else
                            alert("Illegal access!");
                    }
                }
            })
        }
    }

    command(url) {
        $.ajax({
            url: url,
            method: 'GET',
            success: () => { this.refresh(true) }
        });
    }

    confirmedCommand(message, url) {
        bootbox.confirm(message, (result) => { if (result) this.command(url) });
    }
}
